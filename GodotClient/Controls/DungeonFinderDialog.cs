using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 DungeonFinderDialog：副本/团队页、名称过滤、排序、9 行滚动和加入副本。</summary>
public sealed partial class DungeonFinderDialog : DXWindow
{
    private readonly List<InstanceInfo> _items = new();
    private readonly DungeonFinderRow[] _rows = new DungeonFinderRow[9];
    private readonly DXTextInput _filter;
    private readonly DXVScrollBar _scroll;
    private readonly DXButton _join;
    private readonly DXLabel _status;
    private readonly DXButton _dungeonTab;
    private readonly DXButton _raidTab;
    private readonly DXButton _sort;
    private int _selected = -1;
    private DungeonFinderSort _sortMode;

    public DungeonFinderDialog()
    {
        Text = "地下城查找";
        Size = new Vector2I(578, 507);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false });
        _dungeonTab = MakeTab("Dungeons", new Vector2I(9, 37), true);
        _raidTab = MakeTab("Raids", new Vector2I(109, 37), false);

        // 原版筛选栏是 DungeonTab 客户区内的 10,10 / 540x26 边框面板。
        // 这里使用根坐标表达同一位置：客户区从 (9,37) 开始。
        var filterPanel = new DXControl
        {
            Location = new Vector2I(19, 47),
            Size = new Vector2I(540, 26),
            Border = true,
            BorderColour = new Color(.55f, .40f, .18f),
            IsControl = false,
        };
        AddControl(filterPanel);
        filterPanel.AddControl(new DXLabel { Text = "名称：", FontSize = 9, Location = new Vector2I(5, 5), IsControl = false });
        _filter = new DXTextInput { Location = new Vector2I(47, 3), Size = new Vector2I(180, 20) };
        filterPanel.AddControl(_filter);
        filterPanel.AddControl(new DXLabel { Text = "排序：", FontSize = 9, Location = new Vector2I(237, 5), IsControl = false });
        _sort = new DXButton { Text = "名称", Type = DXButton.ButtonType.SmallButton, FontSize = 9, Location = new Vector2I(272, 1), Size = new Vector2I(100, 24), Index = -1, LibraryFile = LibraryFile.Interface };
        _sort.MouseClick += (s, e) => { _sortMode = (DungeonFinderSort)(((int)_sortMode + 1) % 3); _sort.Text = $"排序: {_sortMode}"; Search(); };
        filterPanel.AddControl(_sort);
        var search = new DXButton { Text = "搜索", Type = DXButton.ButtonType.SmallButton, FontSize = 10, Location = new Vector2I(447, 1), Size = new Vector2I(80, 25), Index = -1, LibraryFile = LibraryFile.Interface };
        search.MouseClick += (s, e) => Search(); filterPanel.AddControl(search);
        _filter.TextChanged += s => { if (s.EndsWith("\n")) Search(); };
        _scroll = new DXVScrollBar { Location = new Vector2I(542, 83), Size = new Vector2I(14, 402), VisibleSize = _rows.Length, Change = 3 };
        _scroll.ValueChanged += (s, e) => RefreshRows(); AddControl(_scroll);
        for (int i = 0; i < _rows.Length; i++)
        {
            int row = i;
            _rows[i] = new DungeonFinderRow { Location = new Vector2I(19, 83 + i * 43), Size = new Vector2I(515, 40) };
            _rows[i].MouseClick += (s, e) => { _selected = row + _scroll.Value; RefreshRows(); };
            AddControl(_rows[i]);
        }
        // 原版 JoinButton 是根窗口按钮，位于页签标题右侧，而不是列表底部。
        _join = new DXButton { Text = "加入副本", Type = DXButton.ButtonType.SmallButton, FontSize = 9, Location = new Vector2I(490, 35), Size = new Vector2I(80, 25), Index = -1, LibraryFile = LibraryFile.Interface, Enabled = false };
        _join.MouseClick += (s, e) => JoinSelected(); AddControl(_join);
        _status = new DXLabel { Text = "", FontSize = 9, TextColour = Colors.Yellow, Location = new Vector2I(20, 482), IsControl = false };
        AddControl(_status);
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(548, 3) };
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
        Search();
    }

    private DXButton MakeTab(string text, Vector2I location, bool selected)
    {
        var button = new DXButton
        {
            Text = text,
            Type = selected ? DXButton.ButtonType.SelectedTab : DXButton.ButtonType.DeselectedTab,
            FontSize = 9,
            Location = location,
            Size = new Vector2I(96, 24),
            LibraryFile = LibraryFile.Interface,
            Index = -1,
        };
        button.MouseClick += (s, e) =>
        {
            _dungeonTab.Type = button == _dungeonTab ? DXButton.ButtonType.SelectedTab : DXButton.ButtonType.DeselectedTab;
            _raidTab.Type = button == _raidTab ? DXButton.ButtonType.SelectedTab : DXButton.ButtonType.DeselectedTab;
            _dungeonTab.QueueRedraw();
            _raidTab.QueueRedraw();
            Search();
        };
        AddControl(button);
        return button;
    }

    public void Search()
    {
        _items.Clear();
        string filter = _filter.Text.Trim();
        foreach (var info in Globals.InstanceInfoList?.Binding ?? Enumerable.Empty<InstanceInfo>())
        {
            if (info == null || !info.ShowOnDungeonFinder) continue;
            if (!string.IsNullOrEmpty(filter) && (info.Name == null || !info.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))) continue;
            _items.Add(info);
        }
        switch (_sortMode)
        {
            case DungeonFinderSort.Level:
                _items.Sort((a, b) => b.MinPlayerLevel.CompareTo(a.MinPlayerLevel));
                break;
            case DungeonFinderSort.PlayerCount:
                _items.Sort((a, b) => a.MaxPlayerCount.CompareTo(b.MaxPlayerCount));
                break;
            default:
                _items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                break;
        }
        _selected = -1;
        _scroll.Value = 0;
        _scroll.MaxValue = _items.Count;
        _join.Enabled = false;
        RefreshRows();
    }

    private void RefreshRows()
    {
        for (int i = 0; i < _rows.Length; i++)
        {
            int index = _scroll.Value + i;
            var info = index < _items.Count ? _items[index] : null;
            _rows[i].Visible = info != null;
            if (info == null) continue;
            _rows[i].SetInstance(info, index == _selected);
        }
        _join.Enabled = _selected >= 0 && _selected < _items.Count;
    }

    private void JoinSelected()
    {
        if (_selected < 0 || _selected >= _items.Count) return;
        GameScene.Game?.SendJoinInstance(_items[_selected].Index);
        _status.Text = string.Format(Lang.DungeonFinderUi8Label, _items[_selected].Name);
    }

    public bool AuditLayout(out string details)
    {
        bool rows = _rows.All(x => x.Size == new Vector2I(515, 40))
            && _rows[0].Location == new Vector2I(19, 83)
            && _rows[8].Location == new Vector2I(19, 427);
        bool controls = _scroll.Location == new Vector2I(542, 83)
            && _scroll.Size == new Vector2I(14, 402)
            && _join.Location == new Vector2I(490, 35)
            && _join.Size == new Vector2I(80, 25);
        details = $"size={Size} filter=(19,47)/(540,26) rows={_rows.Length} row0={_rows[0].Location}/{_rows[0].Size} scroll={_scroll.Location}/{_scroll.Size} join={_join.Location}/{_join.Size}";
        return Size == new Vector2I(578, 507) && rows && controls;
    }
}

/// <summary>原版 DungeonRow：515x40，四列独立字段，选中时只改变行底色。</summary>
internal sealed partial class DungeonFinderRow : DXControl
{
    private readonly DXLabel _name = MakeLabel(20);
    private readonly DXLabel _type = MakeLabel(150);
    private readonly DXLabel _level = MakeLabel(250);
    private readonly DXLabel _count = MakeLabel(350);

    public DungeonFinderRow()
    {
        BackColour = new Color(.08f, .06f, .045f, .82f);
        Border = true;
        BorderColour = new Color(.35f, .27f, .15f);
        AddControl(_name);
        AddControl(_type);
        AddControl(_level);
        AddControl(_count);
    }

    public void SetInstance(InstanceInfo info, bool selected)
    {
        BackColour = selected ? new Color(.28f, .22f, .10f, .92f) : new Color(.08f, .06f, .045f, .82f);
        _name.Text = info.Name ?? string.Empty;
        _type.Text = info.Type.ToString();
        _level.Text = $"等级: {GetLevel(info)}";
        _count.Text = $"玩家人数: {GetPlayerCount(info)}";
        QueueRedraw();
    }

    private static string GetLevel(InstanceInfo info)
        => info.MinPlayerLevel == 0 && info.MaxPlayerLevel == 0 ? "任意" : info.MaxPlayerLevel == 0 ? $"{info.MinPlayerLevel}+" : $"{info.MinPlayerLevel} - {info.MaxPlayerLevel}";

    private static string GetPlayerCount(InstanceInfo info)
        => info.MaxPlayerCount == 0 ? "任意" : $"{info.MinPlayerCount} - {info.MaxPlayerCount}";

    private static DXLabel MakeLabel(int x) => new()
    {
        Location = new Vector2I(x, 12),
        FontSize = 9,
        TextColour = Colors.White,
        DrawOutline = true,
        OutlineColour = Colors.Black,
        IsControl = false,
    };
}
