using System;
using System.Collections.Generic;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 ChatOptionsDialog 的窗口布局：聊天窗口列表、添加、重置、保存。</summary>
public partial class ChatOptionsDialog : DXWindow
{
    private readonly DXControl _list;
    private readonly DXControl _filterPanel;
    private readonly Dictionary<MessageType, DXButton> _filterButtons = new();
    private readonly Dictionary<string, DXButton> _optionButtons = new();
    private readonly List<DXButton> _tabButtons = new();
    private DXTextInput _nameInput;
    private DXButton _removeTab;
    private int _selectedTab;
    private int _count = 1;

    private static readonly (MessageType Type, string Name)[] FilterTypes =
    {
        (MessageType.Normal, "本地"),
        (MessageType.Shout, "喊话"),
        (MessageType.WhisperIn, "私聊"),
        (MessageType.Group, "队伍"),
        (MessageType.Global, "世界"),
        (MessageType.Hint, "提示"),
        (MessageType.System, "系统"),
        (MessageType.Combat, "战斗"),
        (MessageType.ObserverChat, "观察"),
        (MessageType.Guild, "行会"),
    };

    public ChatOptionsDialog()
    {
        HasTitle = true;
        HasFooter = true;
        Movable = true;
        AllowResize = true;
        // 原版 SetClientSize(350, 250) 的总窗口尺寸。
        Size = new Vector2I(368, 350);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(338, 3) };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);
        AddControl(new DXLabel { Text = "聊天选项", FontSize = 12, TextColour = new Color(1f, 0.85f, 0.3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I(368, 28), IsControl = false });

        _list = new DXControl { Location = new Vector2I(9, 37), Size = new Vector2I(120, 220), Clip = true };
        AddControl(_list);
        AddTab("主聊天");

        _filterPanel = new DXControl { Location = new Vector2I(134, 37), Size = new Vector2I(200, 250), Clip = true };
        AddControl(_filterPanel);
        _filterPanel.AddControl(new DXLabel
        {
            Text = "窗口名:",
            FontSize = 10,
            TextColour = new Color(1f, 0.85f, 0.3f),
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Location = new Vector2I(0, 1),
            Size = new Vector2I(48, 20),
            IsControl = false,
        });
        _nameInput = new DXTextInput { Location = new Vector2I(48, 0), Size = new Vector2I(82, 21) };
        _nameInput.TextChanged += value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                GameScene.Game?.RenameChatTab(_selectedTab, value);
                if (_selectedTab >= 0 && _selectedTab < _tabButtons.Count)
                    _tabButtons[_selectedTab].Text = value.Trim();
            }
        };
        _filterPanel.AddControl(_nameInput);
        _removeTab = new DXButton { Text = "移除", FontSize = 9, Size = new Vector2I(55, 21), Location = new Vector2I(136, 0), LibraryFile = LibraryFile.Interface, Index = -1 };
        _removeTab.MouseClick += (o, e) =>
        {
            if (_tabButtons.Count <= 1) return;
            GameScene.Game?.RemoveChatTab(_selectedTab);
            RefreshTabsFromGame();
        };
        _filterPanel.AddControl(_removeTab);

        AddOption("transparent", "透明", new Vector2I(0, 20));
        AddOption("alert", "提醒", new Vector2I(65, 20));
        AddOption("hideTab", "隐藏标签", new Vector2I(130, 20));
        AddOption("reverse", "倒序", new Vector2I(0, 43));
        AddOption("cleanup", "移除旧的", new Vector2I(65, 43));
        AddOption("fade", "淡出", new Vector2I(130, 43));

        for (int i = 0; i < FilterTypes.Length; i++)
        {
            var entry = FilterTypes[i];
            var button = new DXButton
            {
                FontSize = 9,
                Size = new Vector2I(62, 25),
                Location = new Vector2I((i % 3) * 64, 68 + (i / 3) * 22),
                LibraryFile = LibraryFile.Interface,
                Index = -1,
            };
            button.MouseClick += (o, e) => ToggleFilter(entry.Type, entry.Name);
            _filterPanel.AddControl(button);
            _filterButtons[entry.Type] = button;
            UpdateFilterButton(entry.Type, entry.Name);
        }

        var add = Button("添加", new Vector2I(79, 262), new Vector2I(50, 25), DXButton.ButtonType.SmallButton);
        add.MouseClick += (o, e) =>
        {
            var title = $"聊天 {_count + 1}";
            AddTab(title);
            GameScene.Game?.AddChatTab(title);
            SelectTab(_count - 1);
        };
        var reset = Button("重置所有", new Vector2I(278, 307), new Vector2I(80, 25), DXButton.ButtonType.Default);
        reset.MouseClick += (o, e) =>
        {
            foreach (var button in _tabButtons)
            {
                _list.RemoveControl(button);
                button.QueueFree();
            }
            _tabButtons.Clear();
            _count = 0;
            AddTab("主聊天");
            GameScene.Game?.ResetChatTabs();
            SelectTab(0);
        };
        var save = Button("保存所有", new Vector2I(9, 307), new Vector2I(80, 25), DXButton.ButtonType.Default);
        save.MouseClick += (o, e) =>
        {
            GameScene.Game?.SaveChatTabs();
            GameScene.Game?.ReceiveChat("聊天布局已保存", MessageType.Announcement);
        };
        var reload = Button("重新加载", new Vector2I(94, 307), new Vector2I(80, 25), DXButton.ButtonType.Default);
        reload.MouseClick += (o, e) =>
        {
            GameScene.Game?.LoadChatTabs();
            RefreshTabsFromGame();
        };
        SelectTab(0);
    }

    private void AddTab(string title)
    {
        int index = _count;
        _count++;
        var button = new DXButton { Text = title, FontSize = 10, TextColour = Colors.White, Size = new Vector2I(108, 25), Location = new Vector2I(0, index * 28), LibraryFile = LibraryFile.Interface, Index = -1 };
        button.MouseClick += (o, e) => SelectTab(index);
        _list.AddControl(button);
        _tabButtons.Add(button);
        if (_count == 1) SelectTab(0);
    }

    private void RefreshTabsFromGame()
    {
        foreach (var button in _tabButtons)
        {
            _list.RemoveControl(button);
            button.QueueFree();
        }
        _tabButtons.Clear();
        _count = 0;
        int count = Math.Max(1, GameScene.Game?.ChatTabCount ?? 1);
        for (int i = 0; i < count; i++)
            AddTab(GameScene.Game?.GetChatTabTitle(i) ?? $"聊天 {i + 1}");
        SelectTab(Math.Clamp(GameScene.Game?.SelectedChatTab ?? 0, 0, _tabButtons.Count - 1));
    }

    private void AddOption(string option, string name, Vector2I location)
    {
        var button = new DXButton
        {
            FontSize = 9,
            Size = new Vector2I(62, 21),
            Location = location,
            LibraryFile = LibraryFile.Interface,
            Index = -1,
        };
        button.MouseClick += (o, e) =>
        {
            GameScene.Game?.SetChatOption(option, !GetChatOption(option));
            UpdateOptionButton(option, name);
        };
        _filterPanel.AddControl(button);
        _optionButtons[option] = button;
        UpdateOptionButton(option, name);
    }

    private bool GetChatOption(string option) => GameScene.Game?.GetChatOption(option) ?? false;

    private void UpdateOptionButton(string option, string name)
    {
        if (!_optionButtons.TryGetValue(option, out var button)) return;
        bool enabled = GetChatOption(option);
        button.Text = $"{(enabled ? "[开]" : "[关]")}{name}";
        button.TextColour = enabled ? Colors.White : new Color(0.45f, 0.45f, 0.45f);
    }

    private void SelectTab(int index)
    {
        if (index < 0 || index >= _tabButtons.Count) return;
        _selectedTab = index;
        foreach (var button in _tabButtons)
            button.TextColour = button == _tabButtons[index] ? new Color(1f, .85f, .3f) : Colors.White;
        GameScene.Game?.SelectChatTab(index);
        if (_nameInput != null)
            _nameInput.Text = GameScene.Game?.GetChatTabTitle(index) ?? _tabButtons[index].Text;
        foreach (var entry in FilterTypes) UpdateFilterButton(entry.Type, entry.Name);
        foreach (var entry in new[] { ("transparent", "透明"), ("alert", "提醒"), ("hideTab", "隐藏标签"), ("reverse", "倒序"), ("cleanup", "移除旧的"), ("fade", "淡出") })
            UpdateOptionButton(entry.Item1, entry.Item2);
    }

    private void ToggleFilter(MessageType type, string name)
    {
        bool enabled = !(GameScene.Game?.IsChatTypeEnabled(type) ?? true);
        GameScene.Game?.SetChatFilter(type, enabled);
        UpdateFilterButton(type, name);
    }

    private void UpdateFilterButton(MessageType type, string name)
    {
        if (!_filterButtons.TryGetValue(type, out var button)) return;
        button.Text = $"{(GameScene.Game?.IsChatTypeEnabled(type) ?? true ? "[开]" : "[关]")}{name}";
        button.TextColour = GameScene.Game?.IsChatTypeEnabled(type) ?? true
            ? Colors.White
            : new Color(0.45f, 0.45f, 0.45f);
    }

    private DXButton Button(string text, Vector2I location, Vector2I size, DXButton.ButtonType type)
    {
        var button = new DXButton { Text = text, FontSize = 10, TextColour = new Color(1f, 0.85f, 0.3f), Location = location, Size = size, LibraryFile = LibraryFile.Interface, Index = -1, Type = type };
        AddControl(button);
        return button;
    }
}
