using System;
using Godot;
using Library;
using Library.Network;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 GroupLFGInputWindow：组名、PvE/PvP、人数和 Enable/Disable/Cancel。</summary>
public sealed partial class GroupLfgInputDialog : DXWindow
{
    private readonly DXTextInput _name;
    private readonly DXTextInput _count;
    private readonly DXButton _type;
    private readonly Action<bool, string, string, int> _submit;

    public GroupLfgInputDialog(ClientLookingForGroup current, Action<bool, string, string, int> submit)
    {
        Text = "寻找队伍";
        HasFooter = true;
        Size = new Vector2I(318, 196); // 原版 SetClientSize(300, 60 + wrapped label)
        _submit = submit;
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(290, 3) };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);

        AddControl(new DXLabel
        {
            Text = "输入想要的队伍名称、规模和类型。\n队伍通知将保留 1 小时，或直到你取消它。",
            FontSize = 9,
            Align = HorizontalAlignment.Center,
            Location = new Vector2I(9, 38),
            Size = new Vector2I(300, 34),
            IsControl = false,
        });
        _name = new DXTextInput { Text = current?.GroupName ?? string.Empty, Location = new Vector2I(59, 78), Size = new Vector2I(200, 20) };
        AddControl(_name);
        AddControl(new DXLabel { Text = "类型", FontSize = 9, Location = new Vector2I(20, 108), IsControl = false });
        _type = new DXButton { Text = string.Equals(current?.GroupType, "PvP", StringComparison.OrdinalIgnoreCase) ? "PvP" : "PvE", FontSize = 9, Location = new Vector2I(59, 104), Size = new Vector2I(100, 20), LibraryFile = LibraryFile.Interface, Index = -1 };
        _type.MouseClick += (o, e) => _type.Text = _type.Text == "PvE" ? "PvP" : "PvE";
        AddControl(_type);
        AddControl(new DXLabel { Text = "最大人数", FontSize = 9, Location = new Vector2I(177, 108), IsControl = false });
        _count = new DXTextInput { Text = Math.Clamp(current?.MaxCount ?? 4, 2, Globals.GroupLimit).ToString(), Location = new Vector2I(239, 104), Size = new Vector2I(55, 20) };
        AddControl(_count);

        var enable = MakeButton("Enable", new Vector2I(16, 153));
        enable.MouseClick += (o, e) => Submit(true);
        var disable = MakeButton("Disable", new Vector2I(116, 153));
        disable.MouseClick += (o, e) => Submit(false);
        var cancel = MakeButton("Cancel", new Vector2I(222, 153));
        cancel.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(enable);
        AddControl(disable);
        AddControl(cancel);
    }

    private DXButton MakeButton(string text, Vector2I location) => new()
    {
        Text = text,
        FontSize = 9,
        Location = location,
        Size = new Vector2I(80, 25),
        LibraryFile = LibraryFile.Interface,
        Index = -1,
    };

    private void Submit(bool enabled)
    {
        if (!int.TryParse(_count.Text, out int count)) count = 4;
        count = Math.Clamp(count, 2, Globals.GroupLimit);
        _submit?.Invoke(enabled, _name.Text.Trim(), _type.Text, count);
        WindowManager.Close(this);
    }

    public bool AuditLayout(out string details)
    {
        details = $"size={Size} name={_name.Location}/{_name.Size} type={_type.Location}/{_type.Size} count={_count.Location}/{_count.Size} buttons=(16,153),(116,153),(222,153)";
        return Size == new Vector2I(318, 196)
            && _name.Location == new Vector2I(59, 78)
            && _type.Location == new Vector2I(59, 104)
            && _count.Location == new Vector2I(239, 104);
    }
}
