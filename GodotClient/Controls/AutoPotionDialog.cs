using System;
using System.Linq;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 自动喝药窗口。布局和旧客户端 AutoPotionDialog 保持一致：8 个顺序槽，
/// 每行物品、HP/MP 阈值、启用状态和上下移动按钮；行数超过客户区时使用原版滚动条。
/// </summary>
public sealed partial class AutoPotionDialog : DXWindow
{
    public ClientAutoPotionLink[] Links { get; } = new ClientAutoPotionLink[Globals.MaxAutoPotionCount];
    public AutoPotionRow[] Rows { get; } = new AutoPotionRow[Globals.MaxAutoPotionCount];
    public DXVScrollBar ScrollBar { get; }
    public bool Updating { get; set; }

    public AutoPotionDialog()
    {
        Text = "自动药水";
        HasFooter = true;
        Size = new Vector2I(298, 498); // SetClientSize(280x398) + 原版边框
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(270, 3) };
        close.MouseClick += (s, e) => WindowManager.Close(this);
        AddControl(close);

        for (int i = 0; i < Links.Length; i++)
            Links[i] = new ClientAutoPotionLink { Slot = i, LinkInfoIndex = -1 };

        var panel = new DXControl
        {
            Location = new Vector2I(9, 37),
            Size = new Vector2I(264, 398),
            Clip = true,
            PassThrough = true,
        };
        AddControl(panel);

        ScrollBar = new DXVScrollBar
        {
            Location = new Vector2I(275, 38),
            Size = new Vector2I(14, 396),
            VisibleSize = 398,
            MaxValue = Globals.MaxAutoPotionCount * 50 - 2,
            Change = 50,
        };
        AddControl(ScrollBar);
        ScrollBar.ValueChanged += (s, e) => UpdateLocations(panel);
        panel.MouseWheel += ScrollBar.DoMouseWheel;

        for (int i = 0; i < Rows.Length; i++)
        {
            int slot = i;
            Rows[i] = new AutoPotionRow(slot, this)
            {
                Location = new Vector2I(1, 1 + slot * 50),
            };
            panel.AddControl(Rows[i]);
            Rows[i].MouseWheel += ScrollBar.DoMouseWheel;
        }
    }

    public bool AuditLayout(out string details)
    {
        bool rows = Rows.Length == Globals.MaxAutoPotionCount && Rows.All(x => x.Size == new Vector2I(260, 46));
        details = $"size={Size} panel=(9,37)/(264,398) scroll={ScrollBar.Location}/{ScrollBar.Size} rows={Rows.Length} row0={Rows[0].Size}";
        return Size == new Vector2I(298, 498)
            && ScrollBar.Location == new Vector2I(275, 38)
            && ScrollBar.Size == new Vector2I(14, 396)
            && rows;
    }

    private void UpdateLocations(DXControl panel)
    {
        for (int i = 0; i < Rows.Length; i++)
            Rows[i].Location = new Vector2I(1, 1 + i * 50 - ScrollBar.Value);
    }

    public void ApplyLinks(System.Collections.Generic.IEnumerable<ClientAutoPotionLink> links)
    {
        Updating = true;
        for (int i = 0; i < Links.Length; i++)
        {
            Links[i].Slot = i;
            Links[i].LinkInfoIndex = -1;
            Links[i].Health = 0;
            Links[i].Mana = 0;
            Links[i].Enabled = false;
            Rows[i].ItemCell.QuickInfo = null;
            Rows[i].Health.Value = 0;
            Rows[i].Mana.Value = 0;
            Rows[i].EnabledCheck.Checked = false;
        }

        foreach (var link in links ?? Enumerable.Empty<ClientAutoPotionLink>())
        {
            if (link == null || link.Slot < 0 || link.Slot >= Rows.Length) continue;
            Links[link.Slot] = new ClientAutoPotionLink
            {
                Slot = link.Slot,
                LinkInfoIndex = link.LinkInfoIndex,
                Health = link.Health,
                Mana = link.Mana,
                Enabled = link.Enabled,
            };
            var info = Globals.ItemInfoList?.Binding.FirstOrDefault(x => x.Index == link.LinkInfoIndex);
            Rows[link.Slot].ItemCell.QuickInfo = info;
            Rows[link.Slot].Health.Value = link.Health;
            Rows[link.Slot].Mana.Value = link.Mana;
            Rows[link.Slot].EnabledCheck.Checked = link.Enabled;
        }
        Updating = false;
    }

    public void SendRowUpdate(int slot)
    {
        if (Updating || GameScene.Game == null || slot < 0 || slot >= Rows.Length) return;
        var row = Rows[slot];
        Links[slot].LinkInfoIndex = row.ItemCell.QuickInfo?.Index ?? -1;
        Links[slot].Health = row.Health.Value;
        Links[slot].Mana = row.Mana.Value;
        Links[slot].Enabled = row.EnabledCheck.Checked;
        GameScene.Game.SendAutoPotionLinkChanged(slot, Links[slot]);
    }

    public void SwapRows(int first, int second)
    {
        if (first < 0 || second < 0 || first >= Rows.Length || second >= Rows.Length) return;
        Updating = true;
        Rows[first].SwapValues(Rows[second]);
        var link = Links[first];
        Links[first] = Links[second];
        Links[second] = link;
        Links[first].Slot = first;
        Links[second].Slot = second;
        Rows[first].RefreshIndex(first);
        Rows[second].RefreshIndex(second);
        Updating = false;
        SendRowUpdate(first);
        SendRowUpdate(second);
    }
}

public sealed partial class AutoPotionRow : DXControl
{
    public readonly AutoPotionDialog PotionOwner;
    public readonly DXItemCell ItemCell;
    public readonly DXNumberField Health;
    public readonly DXNumberField Mana;
    public readonly DXCheckButton EnabledCheck;
    private readonly DXLabel _index;
    private readonly DXButton _up;
    private readonly DXButton _down;
    public int Slot { get; private set; }

    public AutoPotionRow(int slot, AutoPotionDialog owner)
    {
        PotionOwner = owner;
        Slot = slot;
        Size = new Vector2I(260, 46);
        Border = true;
        BorderColour = new Color(0.45f, 0.34f, 0.16f);

        _index = new DXLabel { Text = (slot + 1).ToString(), FontSize = 9, TextColour = new Color(1f, .9f, .55f), IsControl = false, Location = new Vector2I(2, 1), Size = new Vector2I(12, 14) };
        AddControl(_index);

        ItemCell = new DXItemCell { GridType = GridType.AutoPotion, Slot = slot, Location = new Vector2I(20, 5) };
        AddControl(ItemCell);

        Health = new DXNumberField("HP", 0, 50000) { Location = new Vector2I(104, 4) };
        Mana = new DXNumberField("MP", 0, 50000) { Location = new Vector2I(104, 25) };
        AddControl(Health);
        AddControl(Mana);

        EnabledCheck = new DXCheckButton("Enabled") { Location = new Vector2I(188, 5) };
        AddControl(EnabledCheck);

        _up = new DXButton { Index = 44, LibraryFile = LibraryFile.Interface, Size = new Vector2I(18, 18), Location = new Vector2I(5, 5) };
        _down = new DXButton { Index = 46, LibraryFile = LibraryFile.Interface, Size = new Vector2I(18, 18), Location = new Vector2I(5, 29) };
        AddControl(_up);
        AddControl(_down);
        _up.MouseClick += (s, e) => PotionOwner.SwapRows(Slot, Slot - 1);
        _down.MouseClick += (s, e) => PotionOwner.SwapRows(Slot, Slot + 1);
        ItemCell.ItemChanged += (s, e) => PotionOwner.SendRowUpdate(Slot);
        Health.ValueChanged += (s, e) => PotionOwner.SendRowUpdate(Slot);
        Mana.ValueChanged += (s, e) => PotionOwner.SendRowUpdate(Slot);
        EnabledCheck.Changed += (s, e) => PotionOwner.SendRowUpdate(Slot);
        RefreshIndex(slot);
    }

    public void RefreshIndex(int slot)
    {
        Slot = slot;
        _index.Text = (slot + 1).ToString();
        ItemCell.Slot = slot;
        _up.Enabled = slot > 0;
        _down.Enabled = slot < PotionOwner.Rows.Length - 1;
    }

    public void SwapValues(AutoPotionRow other)
    {
        var info = ItemCell.QuickInfo;
        ItemCell.QuickInfo = other.ItemCell.QuickInfo;
        other.ItemCell.QuickInfo = info;
        var hp = Health.Value; Health.Value = other.Health.Value; other.Health.Value = hp;
        var mp = Mana.Value; Mana.Value = other.Mana.Value; other.Mana.Value = mp;
        var enabled = EnabledCheck.Checked; EnabledCheck.Checked = other.EnabledCheck.Checked; other.EnabledCheck.Checked = enabled;
    }
}

public sealed partial class DXNumberField : DXControl
{
    public event EventHandler<EventArgs> ValueChanged;
    public readonly string Prefix;
    public readonly int MinValue;
    public readonly int MaxValue;
    private int _value;
    private readonly DXLabel _label;

    public int Value
    {
        get => _value;
        set
        {
            int next = Math.Clamp(value, MinValue, MaxValue);
            if (_value == next) return;
            _value = next;
            _label.Text = $"{Prefix}:{_value}";
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public DXNumberField(string prefix, int min, int max)
    {
        Prefix = prefix; MinValue = min; MaxValue = max;
        Size = new Vector2I(80, 19);
        Border = true;
        _label = new DXLabel { Text = $"{Prefix}:0", FontSize = 9, IsControl = false, Location = new Vector2I(2, 1), Size = new Vector2I(58, 17) };
        AddControl(_label);
        var plus = new DXButton { Text = "+", Size = new Vector2I(16, 9), Location = new Vector2I(61, 0) };
        var minus = new DXButton { Text = "-", Size = new Vector2I(16, 9), Location = new Vector2I(61, 10) };
        AddControl(minus);
        AddControl(plus);
        minus.MouseClick += (s, e) => Value -= 100;
        plus.MouseClick += (s, e) => Value += 100;
    }
}

public sealed partial class DXCheckButton : DXButton
{
    public event EventHandler<EventArgs> Changed;
    private bool _checked;
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Text = value ? "✓" : "□";
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public DXCheckButton(string label)
    {
        Text = "□";
        Size = new Vector2I(68, 18);
        MouseClick += (s, e) => Checked = !Checked;
    }
}
