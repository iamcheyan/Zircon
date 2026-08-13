using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 Fortune Checker：按名称检索有掉落记录的物品，并逐项发起查询。</summary>
public sealed partial class FortuneCheckerDialog : DXWindow
{
    private readonly DXTextInput _name;
    private readonly DXButton _itemType;
    private readonly DXVScrollBar _scroll;
    private readonly DXButton _search;
    private readonly FortuneItemTypeMenu _itemTypeMenu;
    private readonly FortuneRow[] _rows = new FortuneRow[9];
    private List<ItemInfo> _results = new();
    private ItemType? _selectedItemType;

    public DXButton SearchButton => _search;
    public DXButton ItemTypeButton => _itemType;
    public DXVScrollBar SearchScrollBar => _scroll;
    public IReadOnlyList<FortuneRow> Rows => _rows;
    public FortuneItemTypeMenu ItemTypeMenu => _itemTypeMenu;

    public FortuneCheckerDialog()
    {
        Text = "算命师";
        Size = new Vector2I(503, 597);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false });
        AddControl(new DXControl { Location = new Vector2I(9, 37), Size = new Vector2I(485, 26), Border = true, BorderColour = new Color(1f, .8f, .3f), IsControl = false });
        AddControl(new DXLabel { Text = "名称：", FontSize = 10, Location = new Vector2I(14, 42), IsControl = false });
        _name = new DXTextInput { Location = new Vector2I(48, 41), Size = new Vector2I(180, 20) };
        AddControl(_name);
        AddControl(new DXLabel { Text = "物品：", FontSize = 10, Location = new Vector2I(238, 42), IsControl = false });
        _itemType = new DXButton { Text = "全部", Type = DXButton.ButtonType.SmallButton, Size = new Vector2I(95, 25), Location = new Vector2I(270, 40), Index = -1, LibraryFile = LibraryFile.Interface };
        _itemType.MouseClick += (s, e) => _itemTypeMenu.Visible = !_itemTypeMenu.Visible;
        AddControl(_itemType);
        _search = new DXButton { Text = "搜索", Type = DXButton.ButtonType.SmallButton, Size = new Vector2I(80, 25), Location = new Vector2I(389, 40), Index = -1, LibraryFile = LibraryFile.Interface };
        _search.MouseClick += (s, e) => Search();
        AddControl(_search);
        _itemTypeMenu = new FortuneItemTypeMenu(SelectItemType) { Location = new Vector2I(270, 65), Visible = false };
        AddControl(_itemTypeMenu);
        _name.TextSubmitted += _ => Search();
        _scroll = new DXVScrollBar { Location = new Vector2I(480, 68), Size = new Vector2I(14, 524), VisibleSize = 9, Change = 3 };
        AddControl(_scroll);
        _scroll.ValueChanged += (s, e) => RefreshRows();
        for (int i = 0; i < _rows.Length; i++)
        {
            _rows[i] = new FortuneRow(this) { Location = new Vector2I(9, 68 + i * 58) };
            AddControl(_rows[i]);
            _rows[i].MouseWheel += _scroll.DoMouseWheel;
        }
    }

    private void SelectItemType(ItemType? itemType)
    {
        _selectedItemType = itemType;
        _itemType.Text = _itemTypeMenu.GetDisplayName(itemType);
        _itemTypeMenu.Visible = false;
        Search();
    }

    public bool AuditLayout(out string details)
    {
        bool rows = _rows.Length == 9 && _rows[0].Location == new Vector2I(9, 68)
            && _rows[8].Location == new Vector2I(9, 532)
            && _rows.All(x => x.Size == new Vector2I(465, 55));
        bool controls = Size == new Vector2I(503, 597)
            && _itemType.Location == new Vector2I(270, 40)
            && _search.Location == new Vector2I(389, 40)
            && _scroll.Location == new Vector2I(480, 68)
            && _scroll.VisibleSize == 9;
        bool menu = _itemTypeMenu.ItemCount == Enum.GetValues<ItemType>().Length
            && _itemTypeMenu.VisibleRows == 10;
        details = $"size={Size} rows={_rows.Length} rowSize={_rows[0].Size} scroll={_scroll.Location}/{_scroll.VisibleSize} menu={_itemTypeMenu.ItemCount}/{_itemTypeMenu.VisibleRows}";
        return rows && controls && menu;
    }

    public void Search()
    {
        string filter = _name.Text.Trim();
        _results = (Globals.ItemInfoList?.Binding ?? Enumerable.Empty<ItemInfo>())
            .Where(x => x != null && x.Drops != null && x.Drops.Count > 0)
            .Where(x => !_selectedItemType.HasValue || x.ItemType == _selectedItemType.Value)
            .Where(x => string.IsNullOrEmpty(filter) || (x.ItemName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(x => x.ItemName, StringComparer.Ordinal).ToList();
        _scroll.MaxValue = _results.Count;
        RefreshRows();
    }

    private void RefreshRows()
    {
        for (int i = 0; i < _rows.Length; i++)
            _rows[i].SetItem(i + _scroll.Value < _results.Count ? _results[i + _scroll.Value] : null);
    }
}

public sealed partial class FortuneRow : DXControl
{
    private readonly DXItemCell _cell;
    private readonly DXLabel _name;
    private readonly DXLabel _countLabelLabel, _countLabel;
    private readonly DXLabel _progressLabelLabel, _progressLabel;
    private readonly DXLabel _dateLabelLabel, _dateLabel;
    private readonly DXButton _check;
    private ItemInfo _info;

    public FortuneRow(FortuneCheckerDialog owner)
    {
        Size = new Vector2I(465, 55);
        Border = true;
        _cell = new DXItemCell { GridType = GridType.Inspect, ItemGrid = new ClientUserItem[1], Slot = 0, ReadOnly = true, Location = new Vector2I(5, 5) };
        AddControl(_cell);
        _name = new DXLabel { FontSize = 10, Location = new Vector2I(49, 22), Size = new Vector2I(260, 18), IsControl = false };
        AddControl(_name);

        _countLabelLabel = CreateRightLabel("Drop Count:", 5);
        _countLabel = CreateValueLabel(5);
        _progressLabelLabel = CreateRightLabel("Fortune Drop in:", 20);
        _progressLabel = CreateValueLabel(20);
        _dateLabelLabel = CreateRightLabel("Last Check:", 35);
        _dateLabel = CreateValueLabel(35);

        _check = new DXButton { Text = "查询", Type = DXButton.ButtonType.SmallButton, Size = new Vector2I(50, 25), Location = new Vector2I(410, 34), Index = -1, LibraryFile = LibraryFile.Interface };
        _check.MouseClick += (s, e) => Check();
        AddControl(_check);
        Visible = false;
    }

    private DXLabel CreateRightLabel(string text, int y)
    {
        var label = new DXLabel { Text = text, FontSize = 9, TextColour = Colors.White, Location = new Vector2I(210, y), Size = new Vector2I(105, 15), Align = HorizontalAlignment.Right, IsControl = false, AutoSize = false };
        AddControl(label);
        return label;
    }

    private DXLabel CreateValueLabel(int y)
    {
        var label = new DXLabel { FontSize = 9, Location = new Vector2I(320, y), Size = new Vector2I(82, 15), IsControl = false, AutoSize = false };
        AddControl(label);
        return label;
    }

    public void SetItem(ItemInfo info)
    {
        _info = info;
        Visible = info != null;
        if (info == null) return;
        _cell.ItemGrid[0] = new ClientUserItem(info, 1);
        _cell.RefreshItem();
        _name.Text = info.Local();
        UpdateInfo(GameScene.Game?.GetFortune(info.Index));
    }

    private void UpdateInfo(ClientFortuneInfo fortune)
    {
        if (fortune == null)
        {
            _countLabel.Text = "未查询";
            _progressLabel.Text = "未查询";
            _dateLabel.Text = "未查询";
            return;
        }

        _countLabel.Text = fortune.DropCount.ToString("#,##0");
        string format = fortune.Progress < 10000 ? "#,##0.#####%" : "#,##0.##%";
        _progressLabel.Text = (1 + fortune.DropCount - fortune.Progress).ToString(format);
        _dateLabel.Text = FormatAge(DateTime.Now - fortune.CheckDate);
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalSeconds < 0) age = TimeSpan.Zero;
        if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d {age.Hours}h";
        if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h {age.Minutes}m";
        return $"{Math.Max(0, (int)age.TotalMinutes)}m";
    }

    private void Check()
    {
        if (_info == null || GameScene.Game?.IsObserver == true) return;
        var confirm = new ConfirmDialog("Are you sure you want to check your fortune progress?", "Fortune Check", () => GameScene.Game?.SendFortuneCheck(_info.Index));
        WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent() ?? this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_info == null) return;
        UpdateInfo(GameScene.Game?.GetFortune(_info.Index));
    }
}

/// <summary>原版 DXComboBox 的下拉列表。列表项使用原版 ItemType 描述文字并可滚动。</summary>
public sealed partial class FortuneItemTypeMenu : DXControl
{
    private readonly List<DXButton> _items = new();
    private readonly DXVScrollBar _scroll;
    private readonly Action<ItemType?> _select;

    public int ItemCount => _items.Count;
    public int VisibleRows { get; } = 10;

    public FortuneItemTypeMenu(Action<ItemType?> select)
    {
        _select = select;
        Size = new Vector2I(180, 198);
        Border = true;
        BorderColour = new Color(.8f, .6f, .2f);
        BackColour = new Color(0.02f, 0.015f, 0.02f, .98f);
        Clip = true;
        IsControl = true;

        _scroll = new DXVScrollBar { Location = new Vector2I(164, 0), Size = new Vector2I(14, 198), VisibleSize = VisibleRows, Change = 1, HideWhenNoScroll = true };
        _scroll.ValueChanged += (s, e) => UpdateLocations();
        AddControl(_scroll);

        AddItem("全部", null);
        foreach (var itemType in Enum.GetValues<ItemType>())
        {
            if (itemType == ItemType.Nothing) continue;
            var member = typeof(ItemType).GetMember(itemType.ToString()).FirstOrDefault();
            var description = member?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            AddItem(description ?? itemType.ToString(), itemType);
        }

        _scroll.MaxValue = _items.Count;
        UpdateLocations();
    }

    public string GetDisplayName(ItemType? type)
    {
        if (!type.HasValue) return "全部";
        return _items.FirstOrDefault(x => Equals(x.Tag, type))?.Text ?? type.Value.ToString();
    }

    private void AddItem(string text, ItemType? type)
    {
        var button = new DXButton { Text = text, Type = DXButton.ButtonType.DeselectedTab, FontSize = 9, Size = new Vector2I(162, 18), Index = -1, LibraryFile = LibraryFile.Interface, Tag = type };
        button.MouseClick += (s, e) => _select((ItemType?)((DXButton)s).Tag);
        button.MouseWheel += _scroll.DoMouseWheel;
        AddControl(button);
        _items.Add(button);
    }

    private void UpdateLocations()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].Location = new Vector2I(1, 1 + (i - _scroll.Value) * 18);
    }
}
