using System.Collections.Generic;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 原版独立 ChatTextBox：聊天记录、输入栏、频道按钮和选项按钮是四个不同的
/// 控件层级，不能把输入框塞进 ChatTab 的滚动区域。
/// </summary>
public sealed partial class ChatTextBox : DXWindow
{
    public enum ChatMode
    {
        Local, Whisper, Group, Guild, Shout, Global, Observer,
    }

    private static readonly string[] ModeNames = { "普通", "悄悄话", "队伍", "行会", "喊话", "世界", "观察" };
    private readonly DXTextInput _input;
    private readonly DXButton _modeButton;
    private readonly DXButton _optionsButton;
    private readonly List<int> _linkedItemIndexes = new();

    public ChatMode Mode { get; private set; }
    public string LastPM { get; private set; } = string.Empty;

    public ChatTextBox()
    {
        HasTitle = false;
        HasTopBorder = false;
        HasFooter = false;
        ShowCloseButton = false;
        Movable = false;
        AllowResize = true;
        CanResizeHeight = false; // 原版 ChatTextBox: 只允许横向拉宽
        Size = new Vector2I(400, 25);
        Opacity = 0.6f;
        BackColour = new Color(0f, 0f, 0f, 0.35f);

        _modeButton = new DXButton
        {
            Text = ModeNames[0], FontSize = 9, Type = DXButton.ButtonType.SmallButton, Location = new Vector2I(0, 0),
            Size = new Vector2I(60, 24), LibraryFile = LibraryFile.Interface, Index = -1,
        };
        _modeButton.MouseClick += (s, e) => CycleMode();
        AddControl(_modeButton);

        _optionsButton = new DXButton
        {
            Text = "选项", FontSize = 9, Type = DXButton.ButtonType.SmallButton, Location = new Vector2I(345, 0),
            Size = new Vector2I(50, 24), LibraryFile = LibraryFile.Interface, Index = -1,
        };
        _optionsButton.MouseClick += (s, e) => GameScene.Game?.OpenChatOptionsDialog();
        AddControl(_optionsButton);

        _input = new DXTextInput
        {
            Position = new Vector2(65, 1), Size = new Vector2(275, 23),
        };
        _input.MaxLength = Globals.MaxChatLength;
        _input.TextSubmitted += SubmitChat;
        AddControl(_input);
    }

    public void CycleMode()
    {
        Mode = (ChatMode)(((int)Mode + 1) % ModeNames.Length);
        _modeButton.Text = ModeNames[(int)Mode];
    }

    public void LinkItem(ClientUserItem item)
    {
        if (item?.Info == null || _linkedItemIndexes.Count >= Globals.MaxChatItemLinks) return;
        _input.Text += $"[{item.Info.ItemName}]";
        _linkedItemIndexes.Add(item.Index);
        _input.CaretColumn = _input.Text.Length;
        _input.GrabFocus();
    }

    public void StartPM(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        LastPM = $"/{name}";
        _input.Text = LastPM + " ";
        _input.GrabFocus();
        _input.CaretColumn = _input.Text.Length;
    }

    public void OpenChat()
    {
        if (string.IsNullOrEmpty(_input.Text))
        {
            _input.Text = Mode switch
            {
                ChatMode.Shout => "!",
                ChatMode.Whisper when !string.IsNullOrWhiteSpace(LastPM) => LastPM + " ",
                ChatMode.Group => "!!",
                ChatMode.Guild => "!~",
                ChatMode.Global => "!@",
                ChatMode.Observer => "#",
                _ => string.Empty,
            };
        }
        _input.GrabFocus();
        _input.CaretColumn = _input.Text.Length;
    }

    /// <summary>处理原版由 GameScene 转发的空格、回车和频道快捷键。</summary>
    public bool HandleGlobalKey(InputEventKey key)
    {
        if (!key.Pressed) return false;
        var focused = GetViewport()?.GuiGetFocusOwner();
        if (focused != null && (focused == _input || IsAncestorOf(focused))) return false;

        if (key.Keycode == Key.Space || key.Keycode == Key.Enter)
        {
            OpenChat();
            return true;
        }
        if (key.Keycode == Key.Slash)
        {
            OpenChat();
            if (string.IsNullOrWhiteSpace(_input.Text)) _input.Text = string.IsNullOrWhiteSpace(LastPM) ? "/" : LastPM + " ";
            _input.CaretColumn = _input.Text.Length;
            return true;
        }
        if (key.Unicode == '@' || (key.Unicode == '!' && key.ShiftPressed))
        {
            OpenChat();
            _input.Text = key.Unicode == '!' ? "!" : "@";
            _input.CaretColumn = _input.Text.Length;
            return true;
        }
        return false;
    }

    private void SubmitChat(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            GameScene.Game?.SendChat(text, new List<int>(_linkedItemIndexes));
            if (text.StartsWith('/')) LastPM = text.Split(' ')[0];
        }
        _linkedItemIndexes.Clear();
        _input.Text = string.Empty;
        _input.ReleaseFocus();
    }
}
