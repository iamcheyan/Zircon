using System;
using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Scripts;

public partial class SelectScene : Control
{
    private Network.NetworkManager _net;
    private List<SelectInfo> _characters = new();
    private ItemList _charList;
    private LineEdit _nameEdit;
    private OptionButton _classBtn;
    private OptionButton _genderBtn;
    private Button _createBtn;
    private Button _startBtn;
    private Label _statusLabel;

    public override void _Ready()
    {
        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _charList = GetNode<ItemList>("VBox/CharList");
        _nameEdit = GetNode<LineEdit>("VBox/CreateRow/NameEdit");
        _classBtn = GetNode<OptionButton>("VBox/CreateRow/ClassBtn");
        _genderBtn = GetNode<OptionButton>("VBox/CreateRow/GenderBtn");
        _createBtn = GetNode<Button>("VBox/CreateBtn");
        _startBtn = GetNode<Button>("VBox/StartBtn");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");

        // 填充职业/性别选项
        _classBtn.AddItem("Warrior", (int)MirClass.Warrior);
        _classBtn.AddItem("Wizard", (int)MirClass.Wizard);
        _classBtn.AddItem("Taoist", (int)MirClass.Taoist);
        _classBtn.AddItem("Assassin", (int)MirClass.Assassin);
        _genderBtn.AddItem("Male", (int)MirGender.Male);
        _genderBtn.AddItem("Female", (int)MirGender.Female);

        _createBtn.Pressed += OnCreatePressed;
        _startBtn.Pressed += OnStartPressed;
        _charList.ItemSelected += idx => _startBtn.Disabled = false;

        // 订阅网络事件
        _net.Connection.NewCharacterResultEvent += OnNewCharacterResult;
        _net.Connection.StartGameResultEvent += OnStartGameResult;

        RefreshList();

        // headless 自动测试: 没角色就自动建, 有角色就自动进游戏
        var userArgs = OS.GetCmdlineUserArgs();
        bool autoTest = System.Array.Exists(userArgs, a => a == "--auto-login");
        if (autoTest)
        {
            if (_characters.Count == 0)
            {
                GD.Print("[Select] 自动建角色 TestHero...");
                CallDeferred(nameof(AutoCreateCharacter));
            }
            else
            {
                GD.Print($"[Select] 自动进入游戏, 角色: {_characters[0].CharacterName}");
                CallDeferred(nameof(AutoStartGame));
            }
        }
    }

    private void AutoCreateCharacter()
    {
        _net.Connection?.SendNewCharacter("TestHero", MirClass.Warrior, MirGender.Male);
    }
    private void AutoStartGame()
    {
        GD.Print($"[Select] AutoStartGame: 发送 StartGame, charIndex={_characters[0].CharacterIndex}");
        _net.Connection?.SendStartGame(_characters[0].CharacterIndex);
    }

    public void SetCharacters(List<SelectInfo> chars)
    {
        _characters = chars ?? new List<SelectInfo>();
    }

    private void RefreshList()
    {
        _charList.Clear();
        foreach (var c in _characters)
            _charList.AddItem($"#{c.CharacterIndex} {c.CharacterName} Lv{c.Level} {c.Class}");
        if (_characters.Count == 0)
            _statusLabel.Text = "没有角色, 请创建";
        else
            _statusLabel.Text = $"选择角色后点进入游戏";
    }

    private void OnCreatePressed()
    {
        if (string.IsNullOrEmpty(_nameEdit.Text))
        {
            _statusLabel.Text = "请输入角色名";
            return;
        }
        _createBtn.Disabled = true;
        _statusLabel.Text = "创建中...";
        _net.Connection?.SendNewCharacter(
            _nameEdit.Text,
            (MirClass)_classBtn.Selected,
            (MirGender)_genderBtn.Selected
        );
    }

    private NewCharacterResult _pendingNewCharResult;
    private SelectInfo _pendingNewCharInfo;
    private void OnNewCharacterResult(NewCharacterResult result, SelectInfo info)
    {
        _pendingNewCharResult = result;
        _pendingNewCharInfo = info;
        CallDeferred(nameof(ShowNewCharacterResult));
    }
    private void ShowNewCharacterResult()
    {
        _createBtn.Disabled = false;
        if (_pendingNewCharResult == NewCharacterResult.Success)
        {
            GD.Print($"[Select] 建角色成功: {_pendingNewCharInfo?.CharacterName}");
            if (_pendingNewCharInfo != null)
                _characters.Add(_pendingNewCharInfo);
            RefreshList();
            _statusLabel.Text = "创建成功! 选择角色后进入游戏";
            // headless 自动测试: 建完直接进游戏
            var userArgs = OS.GetCmdlineUserArgs();
            if (System.Array.Exists(userArgs, a => a == "--auto-login") && _characters.Count > 0)
            {
                GD.Print("[Select] 自动进入游戏...");
                CallDeferred(nameof(AutoStartGame));
            }
        }
        else
        {
            GD.Print($"[Select] 建角色失败: {_pendingNewCharResult}");
            _statusLabel.Text = $"创建失败: {_pendingNewCharResult}";
        }
    }

    private void OnStartPressed()
    {
        if (_charList.GetSelectedItems().Length == 0) return;
        int idx = _charList.GetSelectedItems()[0];
        if (idx >= _characters.Count) return;
        _startBtn.Disabled = true;
        _statusLabel.Text = "进入游戏...";
        _net.Connection?.SendStartGame(_characters[idx].CharacterIndex);
    }

    private StartGameResult _pendingStartResult;
    private StartInformation _pendingStartInfo;
    private void OnStartGameResult(StartGameResult result, StartInformation info)
    {
        _pendingStartResult = result;
        _pendingStartInfo = info;
        CallDeferred(nameof(ShowStartGameResult));
    }
    private void ShowStartGameResult()
    {
        if (_pendingStartResult == StartGameResult.Success)
        {
            GD.Print($"[Select] *** StartGame 成功! 进入游戏 ***");
            var gameScene = ResourceLoader.Load<PackedScene>("res://Scenes/GameScene.tscn");
            var game = gameScene.Instantiate<GameScene>();
            GetTree().Root.AddChild(game);
            QueueFree();
        }
        else if (_pendingStartResult == StartGameResult.Delayed)
        {
            GD.Print("[Select] StartGame 冷却中, 3秒后重试...");
            _statusLabel.Text = "冷却中, 3秒后重试...";
            var timer = new Timer();
            timer.WaitTime = 3.0;
            timer.OneShot = true;
            AddChild(timer);
            timer.Timeout += () =>
            {
                GD.Print("[Select] 重试 StartGame");
                GD.Print($"[Select] AutoStartGame: 发送 StartGame, charIndex={_characters[0].CharacterIndex}");
        _net.Connection?.SendStartGame(_characters[0].CharacterIndex);
            };
            timer.Start();
        }
        else
        {
            _statusLabel.Text = $"进入游戏失败: {_pendingStartResult}";
            GD.Print($"[Select] StartGame 失败: {_pendingStartResult}");
            _startBtn.Disabled = false;
        }
    }
}
