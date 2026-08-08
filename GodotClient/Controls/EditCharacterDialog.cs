using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 EditCharacterDialog：选择变更类型、性别/发型/名称并发送对应服务端包。</summary>
public sealed partial class EditCharacterDialog : DXWindow
{
    private readonly DXLabel _description;
    private readonly DXTextInput _name;
    private readonly DXTextInput _hair;
    private readonly DXButton _male;
    private readonly DXButton _female;
    private readonly DXColourControl _hairColour;
    private readonly DXColourControl _armourColour;
    private readonly DXLabel _genderLabel;
    private EditCharacterChange _change = EditCharacterChange.Gender;
    private MirGender _selectedGender = MirGender.Male;

    public EditCharacterDialog()
    {
        Text = "Change Character";
        HasTitle = false;
        Size = new Vector2I(260, 560);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        AddControl(new DXLabel { Text = "Change", FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(260, 18), IsControl = false });
        // 原版 Change 是由外部功能按钮设置的状态，不在窗口内额外绘制一排选择按钮。
        // 窗口内部第一个可见区就是 Select Gender，客户坐标为 (30,45)。
        AddControl(new DXControl { Location = new Vector2I(30, 45), Size = new Vector2I(200, 85), Border = true, BorderColour = new Color(1f, .75f, .25f), BackColour = new Color(.28f, .14f, .14f), IsControl = false });
        AddControl(new DXLabel { Text = "Select Gender", FontSize = 10, Location = new Vector2I(82, 49), IsControl = false });
        _male = new DXButton { LibraryFile = LibraryFile.Interface1c, Index = 115, Size = new Vector2I(40, 38), Location = new Vector2I(86, 65) };
        _female = new DXButton { LibraryFile = LibraryFile.Interface1c, Index = 111, Size = new Vector2I(40, 38), Location = new Vector2I(134, 65) };
        _male.MouseClick += (s, e) => SelectGender(MirGender.Male);
        _female.MouseClick += (s, e) => SelectGender(MirGender.Female);
        AddControl(_male);
        AddControl(_female);
        _genderLabel = new DXLabel { Text = "Male", FontSize = 9, Align = HorizontalAlignment.Center, Location = new Vector2I(90, 110), Size = new Vector2I(80, 16), IsControl = false };
        AddControl(_genderLabel);

        AddControl(new DXControl { Location = new Vector2I(30, 140), Size = new Vector2I(200, 330), Border = true, BorderColour = new Color(1f, .75f, .25f), BackColour = new Color(.28f, .14f, .14f), IsControl = false });
        AddControl(new DXLabel { Text = "Customization", FontSize = 10, Location = new Vector2I(87, 144), IsControl = false });
        _description = new DXLabel { Text = "", FontSize = 9, TextColour = Colors.White, Location = new Vector2I(42, 164), Size = new Vector2I(176, 18), IsControl = false, Visible = false };
        AddControl(_description);
        AddControl(new DXLabel { Text = "Hair Type:", FontSize = 10, Location = new Vector2I(42, 165), IsControl = false });
        _hair = new DXTextInput { Text = "1", Location = new Vector2I(120, 165), Size = new Vector2I(80, 20) }; AddControl(_hair);
        AddControl(new DXLabel { Text = "Hair Colour:", FontSize = 10, Location = new Vector2I(42, 190), IsControl = false });
        _hairColour = ColourSwatch(new Color(.35f, .25f, .18f), new Vector2I(120, 190));
        AddControl(new DXLabel { Text = "Armour Colour:", FontSize = 10, Location = new Vector2I(42, 215), IsControl = false });
        _armourColour = ColourSwatch(new Color(.22f, .28f, .36f), new Vector2I(120, 215));
        AddControl(new DXControl { Location = new Vector2I(35, 240), Size = new Vector2I(190, 225), Border = true, BorderColour = new Color(1f, .75f, .25f), BackColour = new Color(.19f, .15f, .1f), IsControl = false });
        AddControl(new DXLabel { Text = "Preview", FontSize = 10, Align = HorizontalAlignment.Center, Location = new Vector2I(90, 240), Size = new Vector2I(80, 16), IsControl = false });
        AddControl(new DXLabel { Text = "角色预览", FontSize = 10, Align = HorizontalAlignment.Center, Location = new Vector2I(60, 338), Size = new Vector2I(140, 20), IsControl = false });
        AddControl(new DXLabel { Text = "Name:", FontSize = 10, Location = new Vector2I(28, 482), IsControl = false });
        _name = new DXTextInput { Location = new Vector2I(75, 478), Size = new Vector2I(155, 20) }; AddControl(_name);
        var confirm = new DXButton { Text = "确认", FontSize = 10, Location = new Vector2I(90, 517), Size = new Vector2I(80, 25), Index = -1, LibraryFile = LibraryFile.Interface };
        confirm.MouseClick += (s, e) => Confirm(); AddControl(confirm);
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
    }

    public void ResetForCurrent()
    {
        var info = GameScene.Game?.StartInfo;
        _name.Text = info?.Name ?? string.Empty;
        _hair.Text = NormalizeHairType(info?.HairType ?? 1, info?.Class ?? MirClass.Warrior, info?.Gender ?? MirGender.Male).ToString();
        _hairColour.BackColour = ToGodotColour(info?.HairColour ?? System.Drawing.Color.Black);
        _armourColour.BackColour = ToGodotColour(info?.ArmourColour ?? System.Drawing.Color.White);
        SelectGender(info?.Gender ?? MirGender.Male);
        _change = EditCharacterChange.Gender;
        _description.Text = $"当前: {info?.Gender} / {info?.Class}";
    }

    public void SelectChange(EditCharacterChange change)
    {
        _change = change;
        _description.Text = $"已选择: {change}";
    }

    public bool AuditLayout(out string details)
    {
        details = $"size={Size} gender=(30,45)/(200,85) custom=(30,140)/(200,330) name={_name.Location}/{_name.Size} confirm=(90,517)/{new Vector2I(80,25)}";
        return Size == new Vector2I(260, 560)
            && _male.Location == new Vector2I(86, 65)
            && _female.Location == new Vector2I(134, 65)
            && _hair.Location == new Vector2I(120, 165)
            && _name.Location == new Vector2I(75, 478);
    }

    public static bool CanConfirmGender(MirGender current, MirGender selected)
        => current != selected;

    public static int NormalizeHairType(int hair, MirClass characterClass, MirGender gender)
    {
        int max = characterClass == MirClass.Assassin ? 5 : gender == MirGender.Female ? 11 : 10;
        return Math.Clamp(hair, 0, max);
    }

    private void SelectGender(MirGender gender)
    {
        _selectedGender = gender;
        _genderLabel.Text = gender.ToString();
        _male.Index = gender == MirGender.Male ? 115 : 116;
        _female.Index = gender == MirGender.Female ? 110 : 111;
        _male.QueueRedraw();
        _female.QueueRedraw();
    }

    private void Confirm()
    {
        if (GameScene.Game == null) return;
        var info = GameScene.Game.StartInfo;
        if (!int.TryParse(_hair.Text, out int hair)) hair = 1;
        hair = NormalizeHairType(hair, info?.Class ?? MirClass.Warrior, _selectedGender);
        switch (_change)
        {
            case EditCharacterChange.Gender:
                if (info != null && !CanConfirmGender(info.Gender, _selectedGender)) return;
                GameScene.Game.SendGenderChange(_selectedGender, hair, ToDrawingColour(_hairColour.BackColour));
                break;
            case EditCharacterChange.Hair:
                GameScene.Game.SendHairChange(hair, ToDrawingColour(_hairColour.BackColour));
                break;
            case EditCharacterChange.Armour:
                GameScene.Game.SendArmourDye(ToDrawingColour(_armourColour.BackColour));
                break;
            case EditCharacterChange.Name:
                if (!string.IsNullOrWhiteSpace(_name.Text)) GameScene.Game.SendNameChange(_name.Text.Trim());
                break;
        }
        WindowManager.Close(this);
    }

    private DXColourControl ColourSwatch(Color colour, Vector2I location)
    {
        var swatch = new DXColourControl
        {
            BackColour = colour,
            Location = location,
            Size = new Vector2I(80, 22),
        };
        AddControl(swatch);
        return swatch;
    }

    private static System.Drawing.Color ToDrawingColour(Color colour)
        => System.Drawing.Color.FromArgb(
            Mathf.RoundToInt(colour.R * 255f), Mathf.RoundToInt(colour.G * 255f), Mathf.RoundToInt(colour.B * 255f));

    private static Color ToGodotColour(System.Drawing.Color colour)
        => new(colour.R / 255f, colour.G / 255f, colour.B / 255f, colour.A / 255f);
}

public enum EditCharacterChange : byte
{
    Gender,
    Hair,
    Armour,
    Name,
}
