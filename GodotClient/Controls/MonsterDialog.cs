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
    private readonly DXImageControl _healthTexture;
    private readonly DXImageControl _attackIcon;
    private readonly DXButton _expand;
    private readonly DXControl _detailsPanel;
    private readonly DXLabel _ac;
    private readonly DXLabel _mr;
    private readonly DXLabel _dc;
    private readonly DXLabel[] _resist = new DXLabel[8];
    private readonly DXImageControl[] _resistIcons = new DXImageControl[8];
    private readonly DXImageControl _attackSpeedIcon;
    private readonly DXImageControl _movementSpeedIcon;
    private readonly DXImageControl _tamableIcon;
    private readonly DXImageControl _undeadIcon;
    private readonly DXImageControl _growthIcon;
    private ObjectRenderer _monster;

    public MonsterDialog()
    {
        HasTitle = false; HasFooter = false; HasTopBorder = false; ShowCloseButton = false; Size = new Vector2I(186, 54); Opacity = .3f; MouseFilter = MouseFilterEnum.Ignore;
        var levelBox = new DXControl { Location = new Vector2I(5, 5), Size = new Vector2I(31, 20), BackColour = Colors.Black, Border = true, BorderColour = new Color(1f, .75f, .25f), Opacity = .6f, MouseFilter = MouseFilterEnum.Ignore }; AddControl(levelBox);
        _level = new DXLabel { FontSize = 9, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(31, 20), IsControl = false }; levelBox.AddControl(_level);
        var nameBox = new DXControl { Location = new Vector2I(41, 5), Size = new Vector2I(140, 20), BackColour = Colors.Black, Border = true, BorderColour = new Color(1f, .75f, .25f), Opacity = .6f, MouseFilter = MouseFilterEnum.Ignore }; AddControl(nameBox);
        _name = new DXLabel { FontSize = 9, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(140, 20), IsControl = false }; nameBox.AddControl(_name);
        var healthBox = new DXControl { Location = new Vector2I(41, 32), Size = new Vector2I(121, 16), BackColour = Colors.Black, Border = true, BorderColour = new Color(1f, .75f, .25f), Opacity = .6f, MouseFilter = MouseFilterEnum.Ignore, Clip = true }; AddControl(healthBox);
        _healthFill = new DXControl { Location = new Vector2I(1, 2), Size = new Vector2I(1, 12), MouseFilter = MouseFilterEnum.Ignore, Clip = true }; healthBox.AddControl(_healthFill);
        _healthTexture = new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 5430, IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        _healthFill.AddControl(_healthTexture);
        _health = new DXLabel { FontSize = 8, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Size = new Vector2I(120, 16), IsControl = false }; healthBox.AddControl(_health);
        var attackBox = new DXControl { Location = new Vector2I(5, 30), Size = new Vector2I(31, 20), BackColour = Colors.Black, Border = true, BorderColour = new Color(1f, .75f, .25f), Opacity = .6f, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(attackBox);
        _attackIcon = new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 1517, Location = new Vector2I(5, 0), IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        attackBox.AddControl(_attackIcon);
        _expand = new DXButton { LibraryFile = LibraryFile.Interface, Index = 46, Location = new Vector2I(167, 34) }; _expand.MouseClick += (s, e) => ToggleDetails(); AddControl(_expand);

        _detailsPanel = new DXControl
        {
            Location = new Vector2I(5, 60),
            Size = new Vector2I(176, 110),
            Border = true,
            BorderColour = new Color(1f, .75f, .25f),
            BackColour = Colors.Black,
            Opacity = .6f,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        AddControl(_detailsPanel);

        AddStatRow("AC:", 0, out _ac);
        AddStatRow("MR:", 89, out _mr);
        AddStatRow("DC:", 0, out _dc, 17);
        _resist[0] = AddResistance(1510, 5, 39);
        _resist[1] = AddResistance(1511, 48, 39);
        _resist[2] = AddResistance(1512, 91, 39);
        _resist[3] = AddResistance(1513, 134, 39);
        _resist[4] = AddResistance(1514, 5, 63);
        _resist[5] = AddResistance(1515, 48, 63);
        _resist[6] = AddResistance(1516, 91, 63);
        _resist[7] = AddResistance(1517, 134, 63);

        _attackSpeedIcon = AddProgIcon(590, 5);
        _movementSpeedIcon = AddProgIcon(620, 23);
        _tamableIcon = AddProgIcon(631, 41);
        _undeadIcon = AddProgIcon(634, 59);
        _growthIcon = AddProgIcon(630, 77);
        _growthIcon.Visible = false;
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
        var info = _monster.MonsterInfo;
        if (info == null)
        {
            _ac.Text = _mr.Text = _dc.Text = "--";
            foreach (var label in _resist) label.Text = "--";
            return;
        }
        var stats = info.Stats;
        _attackIcon.Index = stats.GetAffinityElement() switch
        {
            Element.Fire => 1510,
            Element.Ice => 1511,
            Element.Lightning => 1512,
            Element.Wind => 1513,
            Element.Holy => 1514,
            Element.Dark => 1515,
            Element.Phantom => 1516,
            _ => 1517,
        };
        _ac.Text = $"{stats[Stat.MinAC]} - {stats[Stat.MaxAC]}";
        _mr.Text = $"{stats[Stat.MinMR]} - {stats[Stat.MaxMR]}";
        _dc.Text = $"{stats[Stat.MinDC]} - {stats[Stat.MaxDC]}";
        _resist[0].Text = Resistance(stats[Stat.FireResistance]);
        _resist[1].Text = Resistance(stats[Stat.IceResistance]);
        _resist[2].Text = Resistance(stats[Stat.LightningResistance]);
        _resist[3].Text = Resistance(stats[Stat.WindResistance]);
        _resist[4].Text = Resistance(stats[Stat.HolyResistance]);
        _resist[5].Text = Resistance(stats[Stat.DarkResistance]);
        _resist[6].Text = Resistance(stats[Stat.PhantomResistance]);
        _resist[7].Text = Resistance(stats[Stat.PhysicalResistance]);
        ColourResist(_resist[0], stats[Stat.FireResistance]);
        ColourResist(_resist[1], stats[Stat.IceResistance]);
        ColourResist(_resist[2], stats[Stat.LightningResistance]);
        ColourResist(_resist[3], stats[Stat.WindResistance]);
        ColourResist(_resist[4], stats[Stat.HolyResistance]);
        ColourResist(_resist[5], stats[Stat.DarkResistance]);
        ColourResist(_resist[6], stats[Stat.PhantomResistance]);
        ColourResist(_resist[7], stats[Stat.PhysicalResistance]);
        _attackSpeedIcon.Index = AttackSpeedIcon(info.AttackDelay);
        _movementSpeedIcon.Index = MovementSpeedIcon(info.MoveDelay);
        _tamableIcon.Index = info.CanTame ? 631 : 632;
        _undeadIcon.Index = info.Undead ? 635 : 634;

        // 旧版 RefreshStats：GrowthLevel > 0 时显示成长图标（ProgUse 630）。
        int growthLevel = _monster.Stats?[Stat.GrowthLevel] ?? 0;
        _growthIcon.Visible = growthLevel > 0;
        _growthIcon.TooltipText = growthLevel > 0 ? $"成长等级 {growthLevel}" : "成长";

        // 旧版各图标的 Hint（悬停文字）：元素/抗性/速度/可驯服/亡灵。
        _attackIcon.TooltipText = stats.GetAffinityElement() switch
        {
            Element.Fire => "火",
            Element.Ice => "冰",
            Element.Lightning => "闪电",
            Element.Wind => "风",
            Element.Holy => "神圣",
            Element.Dark => "黑暗",
            Element.Phantom => "幻影",
            _ => "物理",
        };
        string[] resistNames = { "火", "冰", "闪电", "风", "神圣", "黑暗", "幻影", "物理" };
        for (int i = 0; i < _resistIcons.Length && i < resistNames.Length; i++)
        {
            if (_resistIcons[i] != null) _resistIcons[i].TooltipText = $"{resistNames[i]}抗性";
        }
        _attackSpeedIcon.TooltipText = "攻击速度";
        _movementSpeedIcon.TooltipText = "移动速度";
        _tamableIcon.TooltipText = info.CanTame ? "可驯服" : "不可驯服";
        _undeadIcon.TooltipText = info.Undead ? "亡灵" : "生者";
    }

    public bool AuditLayout(out string details)
    {
        bool valid = Size == new Vector2I(186, 54)
            && Mathf.IsEqualApprox(Opacity, .3f)
            && _detailsPanel.Size == new Vector2I(176, 110)
            && Mathf.IsEqualApprox(_detailsPanel.Opacity, .6f)
            && _resist.Length == 8
            && _attackSpeedIcon.Index == 590
            && _movementSpeedIcon.Index == 620
            && _tamableIcon.Index == 631
            && _undeadIcon.Index == 634
            && _detailsPanel.Controls.Count == 27;
        details = $"size={Size} details={_detailsPanel.Size} controls={_detailsPanel.Controls.Count} icons={_resist.Length}";
        return valid;
    }

    private void ToggleDetails()
    {
        _detailsPanel.Visible = !_detailsPanel.Visible;
        Size = new Vector2I(186, _detailsPanel.Visible ? 175 : 54);
        _expand.Index = _detailsPanel.Visible ? 44 : 46;
    }

    private void AddStatRow(string caption, int x, out DXLabel value, int y = 0)
    {
        var label = new DXLabel
        {
            Text = caption,
            FontSize = 8,
            Location = new Vector2I(x, y + 5),
            Size = new Vector2I(36, 16),
            IsControl = false,
        };
        value = new DXLabel
        {
            FontSize = 8,
            Location = new Vector2I(x + 36, y + 5),
            Size = new Vector2I(54, 16),
            IsControl = false,
        };
        _detailsPanel.AddControl(label);
        _detailsPanel.AddControl(value);
    }

    private DXLabel AddResistance(int iconIndex, int x, int y)
    {
        var icon = new DXImageControl
        {
            LibraryFile = LibraryFile.GameInter,
            Index = iconIndex,
            Location = new Vector2I(x, y),
            IsControl = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailsPanel.AddControl(icon);
        // 记录图标引用，供 Refresh 设置悬停 Hint（旧版 DXImageControl.Hint）。
        for (int i = 0; i < _resistIcons.Length; i++)
            if (_resistIcons[i] == null) { _resistIcons[i] = icon; break; }
        var value = new DXLabel
        {
            FontSize = 8,
            Location = new Vector2I(x + 16, y + 2),
            Size = new Vector2I(27, 16),
            IsControl = false,
        };
        _detailsPanel.AddControl(value);
        return value;
    }

    private DXImageControl AddProgIcon(int index, int x)
    {
        var icon = new DXImageControl
        {
            LibraryFile = LibraryFile.ProgUse,
            Index = index,
            Location = new Vector2I(x, 87),
            IsControl = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _detailsPanel.AddControl(icon);
        return icon;
    }

    private static string Resistance(int value) => $"x{Mathf.Abs(value):0}";

    /// <summary>
    /// 旧版 PopulateLabel 的抗性颜色：0 白、正数 Lime、负数 IndianRed。
    /// </summary>
    private static void ColourResist(DXLabel label, int value)
    {
        label.TextColour = value switch
        {
            > 0 => Colors.Lime,
            < 0 => new Color(0.69f, 0.28f, 0.28f),
            _ => Colors.White,
        };
        label.QueueRedraw();
    }

    private static int AttackSpeedIcon(int delay) => delay switch
    {
        0 => 630,
        >= 2500 => 590,
        >= 2000 => 591,
        >= 1750 => 592,
        >= 1500 => 593,
        >= 1250 => 594,
        >= 1000 => 595,
        > 0 => 596,
        _ => 630,
    };

    private static int MovementSpeedIcon(int delay) => delay switch
    {
        0 => 620,
        >= 2500 => 621,
        >= 1500 => 622,
        >= 1000 => 623,
        >= 900 => 624,
        >= 800 => 625,
        >= 700 => 626,
        > 0 => 627,
        _ => 620,
    };
}
