using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 MonsterDialog：鼠标悬停怪物时显示等级、名称、血量和展开的基础属性。</summary>
public sealed partial class MonsterDialog : DXWindow
{
    private readonly DXLabel _level;
    private readonly DXLabel _name;
    private readonly DXLabel _health;
    private readonly DXControl _healthFill;
    private readonly DXButton _expand;
    private readonly DXLabel _details;
    private ObjectRenderer _monster;

    public MonsterDialog()
    {
        HasTitle = false; HasFooter = false; HasTopBorder = false; Size = new Vector2I(186, 54); Opacity = .75f; MouseFilter = MouseFilterEnum.Ignore;
        var levelBox = new DXControl { Location = new Vector2I(5, 5), Size = new Vector2I(31, 20), BackColour = new Color(0, 0, 0, .75f), Border = true, BorderColour = new Color(1f, .75f, .25f), MouseFilter = MouseFilterEnum.Ignore }; AddControl(levelBox);
        _level = new DXLabel { FontSize = 9, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(31, 20), IsControl = false }; levelBox.AddControl(_level);
        var nameBox = new DXControl { Location = new Vector2I(41, 5), Size = new Vector2I(140, 20), BackColour = new Color(0, 0, 0, .75f), Border = true, BorderColour = new Color(1f, .75f, .25f), MouseFilter = MouseFilterEnum.Ignore }; AddControl(nameBox);
        _name = new DXLabel { FontSize = 9, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(140, 20), IsControl = false }; nameBox.AddControl(_name);
        var healthBox = new DXControl { Location = new Vector2I(41, 32), Size = new Vector2I(121, 16), BackColour = new Color(0, 0, 0, .75f), Border = true, BorderColour = new Color(1f, .75f, .25f), MouseFilter = MouseFilterEnum.Ignore }; AddControl(healthBox);
        _healthFill = new DXControl { Location = new Vector2I(1, 2), Size = new Vector2I(1, 12), BackColour = new Color(.2f, .85f, .25f, .85f), MouseFilter = MouseFilterEnum.Ignore }; healthBox.AddControl(_healthFill);
        _health = new DXLabel { FontSize = 8, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(120, 16), IsControl = false }; healthBox.AddControl(_health);
        _expand = new DXButton { LibraryFile = LibraryFile.Interface, Index = 46, Location = new Vector2I(167, 34) }; _expand.MouseClick += (s, e) => ToggleDetails(); AddControl(_expand);
        _details = new DXLabel { FontSize = 8, TextColour = Colors.White, DrawOutline = true, OutlineColour = Colors.Black, Location = new Vector2I(5, 60), Size = new Vector2I(176, 108), IsControl = false, Visible = false }; AddControl(_details);
    }

    public void SetMonster(ObjectRenderer monster)
    {
        _monster = monster;
        Visible = monster != null && monster.Type == ObjectRenderer.Kind.Monster;
        if (!Visible) return;
        _level.Text = monster.Level.ToString();
        _name.Text = monster.DisplayName;
        Refresh();
    }

    public void Refresh()
    {
        if (_monster == null) return;
        int max = Mathf.Max(1, _monster.MaxHealth);
        int hp = Mathf.Clamp(_monster.Health, 0, max);
        _health.Text = _monster.MaxHealth > 0 ? $"{hp}/{max}" : "未知";
        _healthFill.Size = new Vector2I(Mathf.Max(1, 118 * hp / max), 12);
        _details.Text = "属性\nAC: --    MR: --    DC: --\n抗性: --";
    }

    private void ToggleDetails()
    {
        _details.Visible = !_details.Visible;
        Size = new Vector2I(186, _details.Visible ? 175 : 54);
        _expand.Index = _details.Visible ? 44 : 46;
    }
}
