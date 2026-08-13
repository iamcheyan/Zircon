using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 原版 ConsignmentDialog 的 Godot 移植：搜索/我的寄售双页、6 行可滚动结果、
/// 物品格、排序、购买和下架。服务器采用原版 MarketPlace* 包，不在客户端伪造结果。
/// </summary>
public sealed partial class ConsignmentDialog : DXWindow
{
    private const int VisibleRows = 6;
    private readonly List<ClientMarketPlaceInfo> _results = new();
    private readonly List<ClientMarketPlaceInfo> _consignments = new();
    private readonly DXControl _searchPage;
    private readonly DXControl _consignPage;
    private readonly DXImageControl _pageBackground;
    private readonly DXControl _typeFilter;
    private readonly DXControl _typeFilterContent;
    private readonly DXVScrollBar _typeFilterScroll;
    private readonly List<DXButton> _typeFilterButtons = new();
    private readonly DXTextInput _nameInput;
    private readonly DXLabel _searchCount;
    private readonly DXLabel _consignCount;
    private readonly DXButton _buyButton;
    private readonly DXButton _removeButton;
    private readonly DXButton _searchButton;
    private readonly DXButton _consignButton;
    private readonly DXCheckButton _buyGuildFunds;
    private readonly DXCheckButton _consignGuildFunds;
    private readonly DXButton _searchTab;
    private readonly DXButton _consignTab;
    private readonly DXVScrollBar _searchScroll;
    private readonly DXVScrollBar _consignScroll;
    private readonly DXLabel[] _searchRows = new DXLabel[VisibleRows];
    private readonly DXLabel[] _consignRows = new DXLabel[VisibleRows];
    private readonly DXItemCell[] _searchCells = new DXItemCell[VisibleRows];
    private readonly DXItemCell[] _consignCells = new DXItemCell[VisibleRows];
    private readonly HashSet<int> _requestedSearchIndexes = new();
    private readonly DXItemCell _consignTarget;
    private readonly DXTextInput _consignPrice;
    private ConsignItemDialog _consignPopup;
    private MarketPlaceSort _sort = MarketPlaceSort.Newest;
    private ItemType? _itemTypeFilter;
    private int _selectedSearch = -1;
    private int _selectedConsign = -1;
    private bool _searchActive = true;
    private CellLinkInfo _pendingConsignLink;

    public ConsignmentDialog()
    {
        Text = Lang.ConsignmentConsignmentLabel;
        HasTitle = false;
        Movable = true;
        HasFooter = false;
        Size = new Vector2I(720, 440);

        AddControl(new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 300,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddControl(new DXLabel { Text = Lang.ConsignmentConsignmentLabel, FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(720, 18), IsControl = false });

        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => WindowManager.Close(this);
        AddControl(close);
        _pageBackground = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 301, FixedSize = true, Size = new Vector2I(720, 332), Location = new Vector2I(0, 60), MouseFilter = MouseFilterEnum.Ignore };
        AddControl(_pageBackground);

        _searchTab = TabButton(Lang.ConsignmentDialogSearchButtonLabel, 8);
        _consignTab = TabButton(Lang.ConsignmentConsignmentLabel3, 92);
        _searchTab.MouseClick += (s, e) => SetPage(true);
        _consignTab.MouseClick += (s, e) => SetPage(false);

        _searchPage = new DXControl { Location = new Vector2I(0, 60), Size = new Vector2I(720, 380), Clip = true };
        AddControl(_searchPage);
        _consignPage = new DXControl { Location = new Vector2I(0, 60), Size = new Vector2I(720, 380), Clip = true, Visible = false };
        AddControl(_consignPage);

        _searchPage.AddControl(new DXLabel { Text = Lang.StorageDialogSortButtonLabel, Location = new Vector2I(10, 6), FontSize = 10, TextColour = new Color(1f, .85f, .3f), IsControl = false });
        var sort = new DXButton { Text = Lang.ConsignmentNewestLabel, FontSize = 10, Location = new Vector2I(62, 10), Size = new Vector2I(105, 22), Index = -1, LibraryFile = LibraryFile.Interface };
        sort.MouseClick += (s, e) => { _sort = _sort == MarketPlaceSort.Newest ? MarketPlaceSort.LowestPrice : MarketPlaceSort.Newest; sort.Text = _sort == MarketPlaceSort.Newest ? Lang.ConsignmentNewestLabel : Lang.ConsignmentLowestPriceLabel; Search(); };
        _searchPage.AddControl(sort);
        _nameInput = new DXTextInput { Location = new Vector2I(495, 10), Size = new Vector2I(125, 20) };
        _searchPage.AddControl(_nameInput);
        _searchButton = new DXButton { Text = Lang.ConsignmentDialogSearchButtonLabel, FontSize = 10, Location = new Vector2I(637, 8), Size = new Vector2I(70, 24), Index = -1, LibraryFile = LibraryFile.Interface };
        _searchButton.MouseClick += (s, e) => Search();
        _searchPage.AddControl(_searchButton);
        _typeFilter = new DXControl { Location = new Vector2I(13, 50), Size = new Vector2I(160, 268), Clip = true };
        _typeFilterContent = new DXControl { Size = new Vector2I(140, 268) };
        _typeFilter.AddControl(_typeFilterContent);
        _typeFilterScroll = new DXVScrollBar { Location = new Vector2I(149, 50), Size = new Vector2I(18, 268), VisibleSize = 12, Change = 1, HideWhenNoScroll = true };
        _typeFilterScroll.ValueChanged += (s, e) => _typeFilterContent.Location = new Vector2I(0, -_typeFilterScroll.Value * 21);
        _searchPage.AddControl(_typeFilter);
        _searchPage.AddControl(_typeFilterScroll);
        BuildTypeFilter();
        AddHeader(_searchPage, Lang.ConsignmentDialogItemTypesLabel, new Vector2I(4, 32), new Vector2I(160, 20));
        AddHeader(_searchPage, Lang.ConsignmentDialogNameLabel, new Vector2I(180, 32), new Vector2I(172, 20));
        AddHeader(_searchPage, Lang.ConsignmentDialogLevelLabel, new Vector2I(356, 32), new Vector2I(55, 20));
        AddHeader(_searchPage, Lang.ConsignmentDialogPriceLabel, new Vector2I(415, 32), new Vector2I(110, 20));
        AddHeader(_searchPage, Lang.ConsignmentDialogSellerLabel, new Vector2I(525, 32), new Vector2I(160, 20));
        _searchCount = new DXLabel { Text = Lang.ConsignmentUi48Label, Location = new Vector2I(38, 338), Size = new Vector2I(106, 17), FontSize = 9, TextColour = Colors.Yellow, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, IsControl = false };
        _searchPage.AddControl(_searchCount);

        _searchScroll = new DXVScrollBar { Location = new Vector2I(691, 52), Size = new Vector2I(18, 268), VisibleSize = VisibleRows };
        _searchScroll.ValueChanged += (s, e) =>
        {
            _selectedSearch = -1;
            _buyButton.Enabled = false;
            RefreshRows();
        };
        _searchPage.AddControl(_searchScroll);
        for (int i = 0; i < VisibleRows; i++)
            CreateRow(_searchPage, _searchRows, _searchCells, i, true);

        _buyButton = ActionButton(Lang.ConsignmentDialogBuyButtonLabel, 615, 338);
        _buyButton.Enabled = false;
        _buyButton.MouseClick += (s, e) => BuySelected();
        _searchPage.AddControl(_buyButton);
        var history = ActionButton(Lang.ConsignmentHistoryLabel, 175, 338);
        history.MouseClick += (s, e) => ShowHistory();
        _searchPage.AddControl(history);
        _buyGuildFunds = new DXCheckButton(Lang.ConsignmentGuildLabel) { Location = new Vector2I(285, 342), Size = new Vector2I(18, 18), FontSize = 9, Enabled = GameScene.Game?.HasGuild == true };
        _searchPage.AddControl(_buyGuildFunds);
        _searchPage.AddControl(new DXLabel { Text = Lang.ConsignmentGuildLabel, FontSize = 9, Location = new Vector2I(305, 342), Size = new Vector2I(70, 18), IsControl = false });

        AddHeader(_consignPage, Lang.ConsignmentDialogNameLabel, new Vector2I(14, 32), new Vector2I(250, 20));
        AddHeader(_consignPage, Lang.ConsignmentDialogLevelLabel, new Vector2I(260, 32), new Vector2I(60, 20));
        AddHeader(_consignPage, Lang.ConsignmentDialogPriceLabel, new Vector2I(325, 32), new Vector2I(140, 20));
        AddHeader(_consignPage, Lang.ConsignmentDialogConsignDateLabel, new Vector2I(479, 32), new Vector2I(200, 20));
        _consignCount = new DXLabel { Text = Lang.ConsignmentUi48Label, Location = new Vector2I(38, 338), Size = new Vector2I(106, 17), FontSize = 9, TextColour = Colors.Yellow, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, IsControl = false };
        _consignPage.AddControl(_consignCount);
        _consignScroll = new DXVScrollBar { Location = new Vector2I(691, 52), Size = new Vector2I(18, 268), VisibleSize = VisibleRows };
        _consignScroll.ValueChanged += (s, e) =>
        {
            _selectedConsign = -1;
            _removeButton.Enabled = false;
            RefreshRows();
        };
        _consignPage.AddControl(_consignScroll);
        for (int i = 0; i < VisibleRows; i++)
            CreateRow(_consignPage, _consignRows, _consignCells, i, false);

        _consignTarget = new DXItemCell { GridType = GridType.Consign, Location = new Vector2I(16, 278), ItemGrid = new ClientUserItem[1], Visible = false };
        _consignPage.AddControl(_consignTarget);
        _consignPrice = new DXTextInput { Location = new Vector2I(145, 281), Size = new Vector2I(120, 24), Visible = false };
        _consignPage.AddControl(_consignPrice);
        _consignButton = ActionButton(Lang.ConsignmentDialogConsignConfirmCaption, 615, 338);
        _consignButton.MouseClick += (s, e) => OpenConsignPopup();
        _consignPage.AddControl(_consignButton);
        _removeButton = ActionButton(Lang.ConsignmentRemoveListingLabel, 510, 338);
        _removeButton.Enabled = false;
        _removeButton.MouseClick += (s, e) => RemoveSelected();
        _consignPage.AddControl(_removeButton);
        _consignGuildFunds = new DXCheckButton(Lang.ConsignmentGuildLabel) { Location = new Vector2I(400, 342), Size = new Vector2I(18, 18), FontSize = 9, Enabled = GameScene.Game?.HasGuild == true };
        _consignPage.AddControl(_consignGuildFunds);
        _consignPage.AddControl(new DXLabel { Text = Lang.ConsignmentGuildLabel, FontSize = 9, Location = new Vector2I(420, 342), Size = new Vector2I(70, 18), IsControl = false });
        SetPage(true);
    }

    private DXButton TabButton(string text, int x)
    {
        var b = new DXButton { Text = text, FontSize = 10, Location = new Vector2I(x, 37), Size = new Vector2I(78, 24), Index = -1, LibraryFile = LibraryFile.Interface };
        AddControl(b);
        return b;
    }

    private DXButton ActionButton(string text, int x, int y) => new() { Text = text, FontSize = 10, Location = new Vector2I(x, y), Size = new Vector2I(90, 27), Index = -1, LibraryFile = LibraryFile.Interface };

    private static void AddHeader(DXControl parent, string text, Vector2I location, Vector2I size)
        => parent.AddControl(new DXLabel { Text = text, Location = location, Size = size, FontSize = 9, TextColour = new Color(1f, .85f, .3f), Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, IsControl = false });

    private void CreateRow(DXControl parent, DXLabel[] labels, DXItemCell[] cells, int row, bool search)
    {
        int y = 58 + row * 42;
        int x = search ? 180 : 14;
        var cell = new DXItemCell { GridType = GridType.Inspect, Location = new Vector2I(x, y), ReadOnly = true, Hidden = false };
        cell.ItemGrid = new ClientUserItem[1];
        parent.AddControl(cell);
        cells[row] = cell;
        var label = new DXLabel { Location = new Vector2I(x + 46, y + 1), Size = new Vector2I(search ? 460 : 600, 35), FontSize = 9, TextColour = Colors.White, DrawOutline = true, OutlineColour = Colors.Black, IsControl = false };
        parent.AddControl(label);
        labels[row] = label;
        int index = row;
        label.MouseClick += (s, e) => Select(index, search);
        cell.MouseClick += (s, e) => Select(index, search);
    }

    private void SetPage(bool search)
    {
        _searchActive = search;
        _pageBackground.Index = search ? 301 : 302;
        _searchPage.Visible = search;
        _consignPage.Visible = !search;
        _selectedSearch = _selectedConsign = -1;
        _buyButton.Enabled = false;
        _removeButton.Enabled = false;
        _buyGuildFunds.Visible = search;
        _consignGuildFunds.Visible = !search;
        if (search) Search(); else RefreshRows();
    }

    public bool TryRouteItem(DXItemCell source)
    {
        if (GameScene.Game?.IsObserver == true || !_consignPage.Visible || source?.GridType is not (GridType.Inventory or GridType.Storage or GridType.PartsStorage)) return false;
        var target = _consignPopup?.Visible == true ? _consignPopup.ItemCell : _consignTarget;
        if (target == null || source.Item == null || source.Item.Flags.HasFlag(UserItemFlags.Marriage) ||
            source.Item.Flags.HasFlag(UserItemFlags.NonRefinable) || source.Item.Flags.HasFlag(UserItemFlags.Bound) ||
            source.Item.Info?.CanTrade != true || (source.GridType == GridType.Inventory && !GameScene.Game.InSafeZone)) return false;
        source.MoveItem(target);
        return true;
    }

    public void CancelPendingLinks()
    {
        if (_pendingConsignLink != null)
        {
            GameScene.Game?.UnlockItemLink(_pendingConsignLink);
            _pendingConsignLink = null;
        }
        foreach (var cell in new[] { _consignTarget, _consignPopup?.ItemCell })
        {
            if (cell == null) continue;
            if (cell.LinkedSourceSlot >= 0)
                GameScene.Game?.UnlockItemLink(new CellLinkInfo { GridType = cell.LinkedSourceGrid, Slot = cell.LinkedSourceSlot });
            if (cell.ItemGrid != null && cell.Slot >= 0 && cell.Slot < cell.ItemGrid.Length) cell.ItemGrid[cell.Slot] = null;
            cell.LinkedSourceGrid = GridType.None;
            cell.LinkedSourceSlot = -1;
            cell.RefreshItem();
        }
    }

    /// <summary>
    /// MarketPlaceConsign 先回 S.ItemChanged，再回 S.MarketPlaceConsign。
    /// 原版在前一个包到达时释放来源 Link；不能等寄售列表包，也不能把失败
    /// 回包留下的 pending 状态带到下一次寄售。
    /// </summary>
    public void ItemChanged(Library.Network.ServerPackets.ItemChanged packet)
    {
        var link = packet?.Link;
        if (link == null || _pendingConsignLink == null ||
            link.GridType != _pendingConsignLink.GridType || link.Slot != _pendingConsignLink.Slot)
            return;

        GameScene.Game?.UnlockItemLink(_pendingConsignLink);
        _pendingConsignLink = null;
    }

    private void OpenConsignPopup()
    {
        if (_consignPopup?.Visible == true) { _consignPopup.GrabFocus(); return; }
        _consignPopup = new ConsignItemDialog((cell, price) =>
        {
            if (GameScene.Game?.IsObserver == true || cell?.Item == null || price <= 0) return;
            var source = FindSourceCell(cell.LinkedSourceGrid, cell.LinkedSourceSlot);
            if (source?.Item == null || cell.Item.Count <= 0 || cell.Item.Count > source.Item.Count) return;
            GameScene.Game?.SendMarketConsign(cell.LinkedSourceGrid, cell.LinkedSourceSlot, cell.Item.Count, price, _consignGuildFunds.Checked);
            // 原版确认后锁定来源格，并销毁寄售弹窗中的临时 Link，等待服务端库存回包。
            source.Locked = true;
            source.UpdateBorder();
            _pendingConsignLink = new CellLinkInfo { GridType = cell.LinkedSourceGrid, Slot = cell.LinkedSourceSlot };
            cell.ItemGrid[0] = null;
            cell.LinkedSourceGrid = GridType.None;
            cell.LinkedSourceSlot = -1;
            cell.RefreshItem();
            _consignGuildFunds.Checked = false;
            WindowManager.Close(_consignPopup);
        });
        WindowManager.Open(_consignPopup, GameScene.Game?.UILayer ?? GetParent());
    }

    private DXItemCell FindSourceCell(GridType grid, int slot)
    {
        var cells = grid switch
        {
            GridType.Inventory => GameScene.Game?.InventoryCells,
            GridType.Storage => GameScene.Game?.StorageCells,
            GridType.PartsStorage => GameScene.Game?.PartsStorageCells,
            GridType.CompanionInventory => GameScene.Game?.CompanionInventoryCells,
            _ => null,
        };
        return cells != null && slot >= 0 && slot < cells.Length ? cells[slot] : null;
    }

    public void Search()
    {
        _results.Clear();
        _requestedSearchIndexes.Clear();
        _selectedSearch = -1;
        _buyButton.Enabled = false;
        _searchScroll.Value = 0;
        _searchScroll.MaxValue = 0;
        RefreshRows();
        GameScene.Game?.SendMarketSearch(_nameInput.Text.Trim(), _sort, _itemTypeFilter.HasValue, _itemTypeFilter ?? ItemType.Nothing);
    }

    private void BuildTypeFilter()
    {
        foreach (var button in _typeFilterButtons)
        {
            _typeFilterContent.RemoveControl(button);
            button.QueueFree();
        }
        _typeFilterButtons.Clear();
        int y = 0;
        AddTypeButton(Lang.ConsignmentDialogAllLabel, null, ref y);
        foreach (var type in Enum.GetValues<ItemType>())
        {
            if (type == ItemType.Nothing) continue;
            AddTypeButton(type.ToString(), type, ref y);
        }
        _typeFilterContent.Size = new Vector2I(140, Math.Max(268, y));
        _typeFilterScroll.MaxValue = Math.Max(12, (int)Math.Ceiling(y / 21f));
    }

    private void AddTypeButton(string text, ItemType? type, ref int y)
    {
        var button = new DXButton { Text = text, FontSize = 9, TextColour = type == _itemTypeFilter ? new Color(1f, .85f, .3f) : Colors.White, LibraryFile = LibraryFile.GameInter, Index = 831, Location = new Vector2I(0, y), Size = new Vector2I(140, 19) };
        button.MouseClick += (s, e) => { _itemTypeFilter = type; BuildTypeFilter(); Search(); };
        _typeFilterContent.AddControl(button);
        _typeFilterButtons.Add(button);
        y += 21;
    }

    public void ApplySearch(int count, IList<ClientMarketPlaceInfo> results)
    {
        _results.Clear();
        // 原版按服务器返回的索引保留空位；不能把未加载项压缩，否则后续
        // MarketPlaceSearchIndex 的 index 会对应到错误的商品。
        if (results != null) _results.AddRange(results);
        while (_results.Count < count) _results.Add(null);
        if (_results.Count > count) _results.RemoveRange(count, _results.Count - count);
        _selectedSearch = -1;
        _buyButton.Enabled = false;
        _searchScroll.MaxValue = Math.Max(0, count - VisibleRows);
        RefreshRows();
    }

    public void ApplySearchCount(int count)
    {
        while (_results.Count < count) _results.Add(null);
        if (_results.Count > count) _results.RemoveRange(count, _results.Count - count);
        _selectedSearch = -1;
        _buyButton.Enabled = false;
        _searchScroll.MaxValue = Math.Max(0, count - VisibleRows);
        RefreshRows();
    }

    public void ApplySearchIndex(int index, ClientMarketPlaceInfo result)
    {
        if (index < 0) return;
        while (_results.Count <= index) _results.Add(null);
        _results[index] = result;
        _selectedSearch = -1;
        _buyButton.Enabled = false;
        RefreshRows();
    }

    public void AddConsignments(IEnumerable<ClientMarketPlaceInfo> items)
    {
        if (items == null) return;
        // 登录时是全量列表，成功寄售时是单条增量包；原版按 Index 合并，
        // 不能在收到单条增量包时清空其它寄售。
        foreach (var info in items.Where(x => x != null))
        {
            int index = _consignments.FindIndex(x => x?.Index == info.Index);
            if (index >= 0) _consignments[index] = info;
            else _consignments.Add(info);
        }
        _selectedConsign = -1;
        _removeButton.Enabled = false;
        _consignScroll.MaxValue = Math.Max(0, _consignments.Count - VisibleRows);
        RefreshRows();
    }

    public void ApplyConsignChanged(int index, long count)
    {
        var item = _consignments.FirstOrDefault(x => x?.Index == index);
        if (item == null) return;
        if (count <= 0) _consignments.Remove(item); else item.Item.Count = count;
        _selectedConsign = -1;
        _removeButton.Enabled = false;
        _consignScroll.MaxValue = Math.Max(0, _consignments.Count - VisibleRows);
        RefreshRows();
    }

    public void ApplyBuy(int index, long count, bool success)
    {
        if (!success)
        {
            _buyButton.Enabled = _selectedSearch >= 0 && _selectedSearch < _results.Count && _results[_selectedSearch]?.Item != null;
            return;
        }
        var item = _results.FirstOrDefault(x => x?.Index == index);
        if (item == null) return;
        // 搜索结果的列表位置就是服务端后续 SearchIndex 的索引；售罄时
        // 保留空槽，不能 Remove 后让所有未加载项发生位移。
        if (count <= 0) item.Item = null; else item.Item.Count = count;
        _selectedSearch = -1;
        _buyButton.Enabled = false;
        RefreshRows();
    }

    private void Select(int row, bool search)
    {
        if (search)
        {
            _selectedSearch = row + _searchScroll.Value;
            _buyButton.Enabled = _selectedSearch >= 0 && _selectedSearch < _results.Count && _results[_selectedSearch]?.Item != null;
        }
        else
        {
            _selectedConsign = row + _consignScroll.Value;
            _removeButton.Enabled = _selectedConsign >= 0 && _selectedConsign < _consignments.Count;
        }
        RefreshRows();
    }

    private void RefreshRows()
    {
        RefreshList(_results, _searchRows, _searchCells, _searchScroll.Value, _selectedSearch, true);
        RefreshList(_consignments, _consignRows, _consignCells, _consignScroll.Value, _selectedConsign, false);
        _searchCount.Text = string.Format(Lang.ConsignmentUi56Label, (_results.Count == 0 ? 0 : _searchScroll.Value + 1), _results.Count);
        _consignCount.Text = string.Format(Lang.ConsignmentUi57Label, (_consignments.Count == 0 ? 0 : _consignScroll.Value + 1), _consignments.Count);
    }

    private void RefreshList(List<ClientMarketPlaceInfo> list, DXLabel[] labels, DXItemCell[] cells, int offset, int selected, bool search)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            int index = offset + i;
            var info = index < list.Count ? list[index] : null;
            bool slotExists = index >= 0 && index < list.Count;
            labels[i].Visible = slotExists;
            cells[i].Visible = info?.Item != null;
            labels[i].Text = info?.Item == null ? Lang.ConsignmentDialogLoadingLabel : search
                ? $"{info.Item.Info.Local()} x{info.Item.Count:#,##0}    {info.Price:#,##0} 金币    {info.Seller ?? "未知"}\n{info.Message ?? ""}"
                : string.Format(Lang.ConsignmentGoldLabel2, info.Item.Info.Local(), info.Item.Count, info.Price, info.ConsignDate);
            labels[i].TextColour = index == selected ? Colors.Yellow : Colors.White;
            cells[i].ItemGrid[0] = info?.Item;
            cells[i].RefreshItem();
            if (search && slotExists && info == null && _requestedSearchIndexes.Add(index))
                GameScene.Game?.SendMarketSearchIndex(index);
        }
    }

    private void BuySelected()
    {
        if (!CanAttemptBuy(GameScene.Game?.IsObserver == true, _selectedSearch, _results.Count)) return;
        if (_results[_selectedSearch] == null) return;
        var info = _results[_selectedSearch];
        if (info.Item == null) return;
        var amount = new ItemAmountDialog($"购买 {info.Item.Info?.Local() ?? "物品"}", Math.Max(1, info.Item.Count), 1, count =>
        {
            long total = count * info.Price;
            var confirm = new ConfirmDialog($"{info.Item.Info?.Local() ?? Lang.CommunicationDialogSendTabItemsLabel} x{count}\n单价: {info.Price:#,##0}\n总价: {total:#,##0}", Lang.ConsignmentDialogBuyConfirmCaption, () =>
            {
                if (!CanConfirmBuy(GameScene.Game?.IsObserver == true, count, info.Item?.Count ?? 0)) return;
                _buyButton.Enabled = false;
                bool guildFunds = _buyGuildFunds.Checked;
                _buyGuildFunds.Checked = false;
                GameScene.Game?.SendMarketBuy(info.Index, count, guildFunds);
            });
            WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
        });
        WindowManager.Open(amount, GameScene.Game?.UILayer ?? GetParent());
    }

    public static bool CanAttemptBuy(bool observer, int selectedIndex, int resultCount)
        => !observer && selectedIndex >= 0 && selectedIndex < resultCount;

    public static bool CanConfirmBuy(bool observer, long count, long available)
        => !observer && count > 0 && count <= available;

    public bool AuditBuyGuard(out string details)
    {
        bool valid = CanAttemptBuy(false, 0, 1)
            && !CanAttemptBuy(true, 0, 1)
            && !CanAttemptBuy(false, -1, 1)
            && !CanAttemptBuy(false, 1, 1)
            && CanConfirmBuy(false, 2, 2)
            && !CanConfirmBuy(true, 1, 2)
            && !CanConfirmBuy(false, 3, 2);
        details = $"normal={CanAttemptBuy(false, 0, 1)} observer={CanAttemptBuy(true, 0, 1)} invalid={CanAttemptBuy(false, 1, 1)} confirm={CanConfirmBuy(false, 2, 2)}";
        return valid;
    }

    private void ShowHistory()
    {
        if (_selectedSearch < 0 || _selectedSearch >= _results.Count || _results[_selectedSearch]?.Item == null) return;
        var item = _results[_selectedSearch].Item;
        GameScene.Game?.OpenMarketHistory(item);
    }

    private void RemoveSelected()
    {
        if (GameScene.Game?.IsObserver == true) return;
        if (_selectedConsign < 0 || _selectedConsign >= _consignments.Count || _consignments[_selectedConsign] == null) return;
        var info = _consignments[_selectedConsign];
        if (info.Item == null) return;
        var amount = new ItemAmountDialog($"下架 {info.Item.Info?.Local() ?? "物品"}", Math.Max(1, info.Item.Count), 1, count =>
        {
            var confirm = new ConfirmDialog($"确定下架 {info.Item.Info?.Local() ?? "物品"} x{count}？", Lang.ConsignmentDialogRemoveListingButtonLabel, () =>
            {
                if (GameScene.Game?.IsObserver == true || info.Item == null || count <= 0 || count > info.Item.Count) return;
                GameScene.Game.SendMarketCancel(info.Index, count);
            });
            WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
        });
        WindowManager.Open(amount, GameScene.Game?.UILayer ?? GetParent());
    }

    private void ConsignSelected()
    {
        if (_consignTarget.Item == null || _consignTarget.LinkedSourceGrid == GridType.None) return;
        if (!int.TryParse(_consignPrice.Text.Trim(), out int price) || price <= 0) return;
        GameScene.Game?.SendMarketConsign(_consignTarget.LinkedSourceGrid, _consignTarget.LinkedSourceSlot, _consignTarget.Item.Count, price, _consignGuildFunds.Checked);
    }
}

/// <summary>原版 Interface 303-306 的独立寄售确认窗口。</summary>
public sealed partial class ConsignItemDialog : DXWindow
{
    private readonly Action<DXItemCell, int> _confirm;
    private readonly DXTextInput _price;
    private readonly DXLabel _itemName;
    public DXItemGrid Grid { get; }
    public DXItemCell ItemCell => Grid.Cells[0];

    public ConsignItemDialog(Action<DXItemCell, int> confirm)
    {
        _confirm = confirm;
        HasTitle = false; HasFooter = false; Movable = true; Size = new Vector2I(296, 228);
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 303, FixedSize = true, Size = new Vector2I(296, 60), MouseFilter = MouseFilterEnum.Ignore });
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 304, FixedSize = true, Size = new Vector2I(296, 84), Location = new Vector2I(0, 60), MouseFilter = MouseFilterEnum.Ignore });
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 305, FixedSize = true, Size = new Vector2I(296, 84), Location = new Vector2I(0, 144), MouseFilter = MouseFilterEnum.Ignore });
        AddControl(new DXLabel { Text = Lang.ConsignmentDialogConsignItemTitle, FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(296, 18), IsControl = false });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 306, Location = new Vector2I(24, 88), MouseFilter = MouseFilterEnum.Ignore });
        Grid = new DXItemGrid { GridSize = new Vector2I(1, 1), GridType = GridType.Consign, Location = new Vector2I(36, 98), Linked = true };
        AddControl(Grid);
        _itemName = new DXLabel { Text = string.Empty, FontSize = 9, Location = new Vector2I(89, 96), Size = new Vector2I(155, 20), IsControl = false };
        AddControl(_itemName);
        AddControl(new DXLabel { Text = Lang.ConsignmentUnitPriceLabel, FontSize = 9, TextColour = new Color(1f, .85f, .3f), Location = new Vector2I(55, 122), Size = new Vector2I(30, 20), IsControl = false });
        _price = new DXTextInput { Location = new Vector2I(89, 120), Size = new Vector2I(155, 22) };
        AddControl(_price);
        var plus = new DXButton { Text = "+5000", FontSize = 8, Location = new Vector2I(246, 96), Size = new Vector2I(45, 22), Index = -1 };
        plus.MouseClick += (s, e) => _price.Text = (ParsePrice() + 5000).ToString(); AddControl(plus);
        var minus = new DXButton { Text = "-5000", FontSize = 8, Location = new Vector2I(246, 122), Size = new Vector2I(45, 22), Index = -1 };
        minus.MouseClick += (s, e) => _price.Text = Math.Max(0, ParsePrice() - 5000).ToString(); AddControl(minus);
        var confirmButton = new DXButton { Text = Lang.ConsignmentDialogConsignConfirmCaption, FontSize = 9, Location = new Vector2I(43, 178), Size = new Vector2I(80, 27), Index = -1 };
        confirmButton.MouseClick += (s, e) => Confirm(); AddControl(confirmButton);
        var cancel = new DXButton { Text = Lang.CommonControlCancel, FontSize = 9, Location = new Vector2I(173, 178), Size = new Vector2I(80, 27), Index = -1 };
        cancel.MouseClick += (s, e) => WindowManager.Close(this); AddControl(cancel);
        ItemCell.ItemChanged += (s, e) => _itemName.Text = ItemCell.Item?.Info?.Local() ?? string.Empty;
    }

    private int ParsePrice() => int.TryParse(_price.Text.Trim(), out int value) ? Math.Max(0, value) : 0;

    private void Confirm()
    {
        int price = ParsePrice();
        if (ItemCell.Item == null)
        {
            // 原版：未放入物品时提示。
            GameScene.Game?.ReceiveChat("Error: No Item selected.", MessageType.System);
            return;
        }
        if (price <= 0)
        {
            // 原版：价格无效时提示。
            GameScene.Game?.ReceiveChat("Error: Invalid Price.", MessageType.System);
            return;
        }
        long fee = Globals.MarketPlaceFee;
        var confirm = new ConfirmDialog(string.Format(Lang.ConsignmentConsignmentLabel4, _itemName.Text, ItemCell.Item?.Count ?? 1, price, fee), Lang.ConsignmentDialogConsignConfirmCaption, () => { _confirm?.Invoke(ItemCell, price); WindowManager.Close(this); });
        WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
    }
}
