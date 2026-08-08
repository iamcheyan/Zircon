using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 NPCRepairDialog：11×5 维修格、费用、背包导入和维修提交。</summary>
public partial class NPCRepairPanel : DXControl
{
    private readonly ClientUserItem[] _items = new ClientUserItem[55];
    private readonly List<CellLinkInfo> _links = new();
    private readonly DXItemGrid _grid;
    private readonly DXLabel _cost;
    private readonly DXButton _repair;
    private readonly DXCheckButton _special;
    private readonly DXCheckButton _guildFunds;
    private readonly LegacyWindowFrame _frame;
    private readonly List<CellLinkInfo> _pendingLinks = new();
    private HashSet<ItemType> _allowedTypes;

    public IEnumerable<ItemType> AllowedTypes
    {
        set => _allowedTypes = value == null ? null : new HashSet<ItemType>(value);
    }

    public NPCRepairPanel()
    {
        // 原版 DXItemGrid 默认 GridPadding=0：11x5 为 386x176。
        // SetClientSize(Grid.Width, Grid.Height + 70) 后总窗口为 404x292，且没有 Footer。
        Size = new Vector2I(404, 292);
        _frame = new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false };
        AddControl(_frame);
        AddControl(new DXLabel { Text = "Repair Items", FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(404, 18), IsControl = false });
        _grid = new DXItemGrid { GridSize = new Vector2I(11, 5), Location = new Vector2I(9, 37), GridType = GridType.Repair, ItemGrid = _items, GridPadding = 0, Border = false }; AddControl(_grid);
        foreach (var cell in _grid.Cells)
            cell.LinkChanged += (o) => RebuildLinks();

        int bottom = 224;
        AddControl(new DXLabel { Text = "Repair Cost:", FontSize = 9, TextColour = Colors.White, Location = new Vector2I(9, bottom), Size = new Vector2I(79, 20), IsControl = false, Border = true, BorderColour = new Color(1f, .75f, .25f) });
        _cost = new DXLabel { Text = "0", FontSize = 10, TextColour = Colors.White, Location = new Vector2I(88, bottom), Size = new Vector2I(248, 20), IsControl = false, Border = true, BorderColour = new Color(1f, .75f, .25f) }; AddControl(_cost);
        _special = new DXCheckButton("Special Repair") { Location = new Vector2I(210, bottom + 25), Size = new Vector2I(100, 19), FontSize = 9 };
        _special.Changed += (o, e) => { if (_special.Checked) ClearUnavailableSpecialItems(); RebuildLinks(); }; AddControl(_special);
        _guildFunds = new DXCheckButton("Use Guild Funds") { Location = new Vector2I(200, bottom + 47), Size = new Vector2I(110, 19), FontSize = 9, Enabled = false }; AddControl(_guildFunds);

        int buttonY = bottom + 25;
        AddSourceButton("Inventory", 9, buttonY, () => ImportCells(GameScene.Game?.InventoryCells));
        AddSourceButton("Equipment", 93, buttonY, () => ImportCells(GameScene.Game?.EquipmentCells));
        _repair = new DXButton { Text = "Repair", Type = DXButton.ButtonType.SmallButton, FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(315, buttonY), Size = new Vector2I(79, 25), Enabled = false };
        _repair.MouseClick += (o, e) => Submit(); AddControl(_repair);
        AddSourceButton("Storage", 9, buttonY + 30, () => ImportCells(GameScene.Game?.StorageCells));
        AddSourceButton("Guild Storage", 93, buttonY + 30, () => ImportCells(GameScene.Game?.GuildStorageCells));
    }

    public void Clear()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i] = null;
            if (_grid.Cells != null && i < _grid.Cells.Length)
            {
                _grid.Cells[i].LinkedSourceGrid = GridType.None;
                _grid.Cells[i].LinkedSourceSlot = -1;
            }
        }
        _links.Clear(); _grid.RefreshGrid(); _cost.Text = "维修费用: 0"; _repair.Enabled = false;
    }

    public List<CellLinkInfo> CancelDisplayedLinks()
    {
        var links = new List<CellLinkInfo>(_links);
        foreach (var cell in _grid.Cells ?? Array.Empty<DXItemCell>())
        {
            if (cell.LinkedSourceSlot >= 0)
                links.Add(new CellLinkInfo { GridType = cell.LinkedSourceGrid, Slot = cell.LinkedSourceSlot });
        }
        Clear();
        return links;
    }

    public List<CellLinkInfo> CancelLinks()
    {
        var links = CancelDisplayedLinks();
        links.AddRange(_pendingLinks);
        _pendingLinks.Clear();
        return links;
    }

    public void RepairResult(Library.Network.ServerPackets.NPCRepair packet)
    {
        if (packet == null) return;

        var resultLinks = packet.Links != null && packet.Links.Count > 0
            ? packet.Links
            : _pendingLinks;
        foreach (var link in resultLinks)
        {
            var source = FindSourceCell(link);
            if (source == null) continue;

            source.Locked = false;
            source.UpdateBorder();
            if (packet.Success && source.Item != null)
            {
                if (packet.Special)
                {
                    source.Item.CurrentDurability = source.Item.MaxDurability;
                    if (source.Item.Info.ItemType != ItemType.Weapon && packet.SpecialRepairDelay > TimeSpan.Zero)
                        source.Item.NextSpecialRepair = Library.Time.Now.Add(packet.SpecialRepairDelay);
                }
                else
                {
                    source.Item.MaxDurability = Math.Max(0,
                        source.Item.MaxDurability - (source.Item.MaxDurability - source.Item.CurrentDurability) / Globals.DuraLossRate);
                    source.Item.CurrentDurability = source.Item.MaxDurability;
                }
                source.RefreshItem();
            }
        }

        _pendingLinks.Clear();
        _repair.Enabled = false;
    }

    public bool TryRouteItem(DXItemCell source)
    {
        if (!CanAcceptSource(source))
            return false;
        var target = _grid.Cells?.FirstOrDefault(c => c.Item == null && c.LinkedSourceSlot < 0);
        if (target == null) return false;
        source.MoveItem(target);
        RebuildLinks();
        return true;
    }

    /// <summary>
    /// 原版 DXItemCell.CheckLink(Repair) 的页面级过滤，供直接拖到维修格
    /// 的路径复用；右键导入和底部批量导入也必须走同一套规则。
    /// </summary>
    public bool CanAcceptSource(DXItemCell source)
    {
        return source?.Item?.Info != null
            && (_allowedTypes == null || _allowedTypes.Contains(source.Item.Info.ItemType))
            && !source.Item.Flags.HasFlag(UserItemFlags.Marriage)
            && source.GridType is not (GridType.PartsStorage or GridType.CompanionEquipment)
            && source.Item.Info.CanRepair
            && source.Item.CurrentDurability < source.Item.MaxDurability
            && (!_special.Checked || source.Item.NextSpecialRepair <= Library.Time.Now);
    }

    private void AddSourceButton(string text, int x, int y, Action action)
    {
        var button = new DXButton { Text = text, Type = DXButton.ButtonType.SmallButton, FontSize = 9, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(x, y), Size = new Vector2I(79, 25) };
        button.MouseClick += (o, e) => action();
        AddControl(button);
    }

    private void ImportCells(IEnumerable<DXItemCell> cells)
    {
        Clear();
        int target = 0;
        foreach (var source in cells ?? Enumerable.Empty<DXItemCell>())
        {
            var item = source?.Item;
            if (item == null || item.Info == null || !item.Info.CanRepair ||
                source.GridType is GridType.PartsStorage or GridType.CompanionEquipment ||
                (_allowedTypes != null && !_allowedTypes.Contains(item.Info.ItemType)) ||
                item.CurrentDurability >= item.MaxDurability || item.Flags.HasFlag(UserItemFlags.Marriage) ||
                (_special.Checked && item.NextSpecialRepair > Library.Time.Now) || target >= _items.Length) continue;
            _items[target] = new ClientUserItem(item, item.Count);
            _grid.Cells[target].LinkedSourceGrid = source.GridType;
            _grid.Cells[target].LinkedSourceSlot = source.Slot;
            target++;
        }
        _grid.RefreshGrid();
        RebuildLinks();
    }

    private void Submit()
    {
        if (_pendingLinks.Count > 0) return;
        RebuildLinks();
        if (_links.Count == 0) return;

        _pendingLinks.Clear();
        _pendingLinks.AddRange(_links);
        foreach (var link in _pendingLinks)
        {
            var source = FindSourceCell(link);
            if (source != null)
            {
                source.Locked = true;
                source.UpdateBorder();
            }
        }

        // 原版在发送前解除维修槽的 Link；来源格保持 Locked，直到 NPCRepair 回包。
        for (int i = 0; i < _grid.Cells.Length; i++)
        {
            _items[i] = null;
            _grid.Cells[i].LinkedSourceGrid = GridType.None;
            _grid.Cells[i].LinkedSourceSlot = -1;
        }
        _links.Clear();
        _grid.RefreshGrid();
        _cost.Text = "维修费用: 0";
        _repair.Enabled = false;
        // 原版提交后复位 GuildCheckBox（Special 不复位）。
        _guildFunds.Checked = false;
        _guildFunds.Enabled = GameScene.Game?.HasGuild == true;
        GameScene.Game?.SendNPCRepair(new List<CellLinkInfo>(_pendingLinks), _special.Checked, _guildFunds.Checked);
    }

    private DXItemCell FindSourceCell(CellLinkInfo link)
    {
        if (link == null || GameScene.Game == null) return null;
        var cells = link.GridType switch
        {
            GridType.Inventory => GameScene.Game.InventoryCells,
            GridType.Equipment => GameScene.Game.EquipmentCells,
            GridType.Storage => GameScene.Game.StorageCells,
            GridType.PartsStorage => GameScene.Game.PartsStorageCells,
            GridType.GuildStorage => GameScene.Game.GuildStorageItemCells,
            GridType.CompanionInventory => GameScene.Game.CompanionInventoryCells,
            GridType.CompanionEquipment => GameScene.Game.CompanionEquipmentCells,
            _ => Array.Empty<DXItemCell>(),
        };
        return link.Slot >= 0 && link.Slot < cells.Length ? cells[link.Slot] : null;
    }

    private void RebuildLinks()
    {
        _links.Clear();
        long cost = 0;
        if (_grid.Cells != null)
            foreach (var cell in _grid.Cells)
            {
                if (cell?.Item == null || cell.LinkedSourceSlot < 0) continue;
                _links.Add(new CellLinkInfo { GridType = cell.LinkedSourceGrid, Slot = cell.LinkedSourceSlot, Count = cell.Item.Count });
                cost += cell.Item.RepairCost(_special.Checked);
            }
        _cost.Text = $"维修费用: {cost:#,##0}";
        _guildFunds.Enabled = GameScene.Game?.HasGuild == true;
        if (_guildFunds.Checked && cost > (GameScene.Game?.GuildFunds ?? 0))
            _cost.TextColour = Colors.Red;
        else
            _cost.TextColour = Colors.White;
        _repair.Enabled = _links.Count > 0;
    }

    private void ClearUnavailableSpecialItems()
    {
        if (_grid.Cells == null) return;
        foreach (var cell in _grid.Cells)
        {
            if (cell?.Item == null || cell.Item.NextSpecialRepair <= Library.Time.Now) continue;
            cell.ItemGrid[cell.Slot] = null;
            cell.LinkedSourceGrid = GridType.None;
            cell.LinkedSourceSlot = -1;
        }
        _grid.RefreshGrid();
    }
}
