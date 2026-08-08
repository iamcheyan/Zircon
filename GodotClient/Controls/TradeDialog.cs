using System;
using Godot;
using System.Linq;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 TradeDialog(Interface 125)：双方 5×2 物品区、金币、确认状态。</summary>
public partial class TradeDialog : DXWindow
{
    private readonly DXItemGrid _playerGrid;
    private readonly DXItemGrid _userGrid;
    private readonly ClientUserItem[] _playerItems = new ClientUserItem[10];
    private readonly DXLabel _userGold;
    private readonly DXLabel _playerGold;
    private DXButton _confirm;

    public TradeDialog()
    {
        HasTitle = false; HasFooter = false; Movable = true; Size = new Vector2I(428, 244);
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 125, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(398, 3) };
        close.MouseClick += (o, e) => CloseTrade(); AddControl(close);
        AddControl(new DXLabel { Text = "交易", FontSize = 12, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I(428, 27), IsControl = false });
        AddControl(new DXLabel { Text = "用户", FontSize = 11, TextColour = new Color(1f, .85f, .3f), Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I(186, 20), Location = new Vector2I(15, 38), IsControl = false });
        AddControl(new DXLabel { Text = "玩家", FontSize = 11, TextColour = new Color(1f, .85f, .3f), Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I(186, 20), Location = new Vector2I(226, 38), IsControl = false });
        _userGrid = new DXItemGrid { GridSize = new Vector2I(5, 2), Location = new Vector2I(15, 73), GridType = GridType.TradeUser, Linked = true, GridPadding = 1, Border = false }; AddControl(_userGrid);
        foreach (var cell in _userGrid.Cells)
            cell.LinkChanged += linked =>
            {
                if (linked?.LinkedSourceSlot >= 0)
                    GameScene.Game?.SendTradeItem(new CellLinkInfo { GridType = linked.LinkedSourceGrid, Slot = linked.LinkedSourceSlot, Count = linked.Item?.Count ?? 1 });
            };
        _playerGrid = new DXItemGrid { GridSize = new Vector2I(5, 2), Location = new Vector2I(226, 73), GridType = GridType.TradePlayer, ItemGrid = _playerItems, ReadOnly = true, GridPadding = 1, Border = false }; AddControl(_playerGrid);
        AddControl(new DXLabel { Text = "金币", FontSize = 8, TextColour = new Color(1f, .85f, .3f), Location = new Vector2I(11, 168), Size = new Vector2I(58, 16), IsControl = false });
        AddControl(new DXLabel { Text = "金币", FontSize = 8, TextColour = new Color(1f, .85f, .3f), Location = new Vector2I(222, 168), Size = new Vector2I(58, 16), IsControl = false });
        _userGold = AddValue("0", 75, 168); _playerGold = AddValue("0", 286, 168);
        _userGold.IsControl = true;
        _userGold.MouseClick += (o, e) =>
        {
            if (GameScene.Game?.IsObserver == true) return;
            var dialog = new ItemAmountDialog("交易金币", 999999999, 1, amount => GameScene.Game?.SendTradeGold(amount));
            WindowManager.Open(dialog, GameScene.Game?.UILayer ?? GetParent());
        };
        _confirm = new DXButton { Text = "确认交易", FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(126, 203), Size = new Vector2I(80, 25) };
        _confirm.MouseClick += (o, e) =>
        {
            if (GameScene.Game?.IsObserver == true) return;
            _confirm.Enabled = false;
            GameScene.Game?.SendTradeConfirm();
        }; AddControl(_confirm);
    }

    private DXLabel AddValue(string text, int x, int y)
    {
        var label = new DXLabel { Text = text, FontSize = 10, TextColour = Colors.White, DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Right, AutoSize = false, Size = new Vector2I(130, 16), Location = new Vector2I(x, y), IsControl = false }; AddControl(label); return label;
    }
    public void OpenTrade(string name) { Text = name; _confirm.Enabled = true; WindowManager.Open(this, GameScene.Game?.UILayer ?? GetParent()); }
    public void ShowRequest(string name)
    {
        var panel = new DXControl { Location = new Vector2I(12, 36), Size = new Vector2I(300, 72), BackColour = new Color(.05f, .03f, .02f, .98f), Border = true, BorderColour = new Color(1f, .75f, .25f) };
        panel.AddControl(new DXLabel { Text = $"{name ?? "未知玩家"} 请求交易", Location = new Vector2I(8, 7), Size = new Vector2I(280, 20), IsControl = false });
        var yes = new DXButton { Text = "接受", Location = new Vector2I(60, 38), Size = new Vector2I(70, 24), Index = -1 };
        yes.MouseClick += (o, e) => { GameScene.Game?.SendTradeRequestResponse(true); panel.QueueFree(); };
        var no = new DXButton { Text = "拒绝", Location = new Vector2I(170, 38), Size = new Vector2I(70, 24), Index = -1 };
        no.MouseClick += (o, e) => { GameScene.Game?.SendTradeRequestResponse(false); panel.QueueFree(); };
        panel.AddControl(yes); panel.AddControl(no); AddControl(panel);
    }
    public void SetOtherItem(ClientUserItem item)
    {
        if (item == null) return;
        int slot = Array.FindIndex(_playerItems, x => x == null);
        if (slot < 0) return;
        _playerItems[slot] = item;
        _playerGrid.RefreshGrid();
    }
    /// <summary>原版 S.TradeAddGold：刷新本地玩家提交的金币。</summary>
    public void SetPlayerGold(long gold) => _userGold.Text = $"金币: {gold:#,##0}";

    /// <summary>原版 S.TradeGoldAdded：刷新交易对方的金币。</summary>
    public void SetOtherGold(long gold) => _playerGold.Text = $"金币: {gold:#,##0}";

    public bool AuditGoldRouting(out string details)
    {
        SetPlayerGold(1234);
        SetOtherGold(5678);
        details = $"player={_userGold.Text} other={_playerGold.Text}";
        bool valid = _userGold.Text == "金币: 1,234" && _playerGold.Text == "金币: 5,678";
        _userGold.Text = "金币: 0";
        _playerGold.Text = "金币: 0";
        return valid;
    }
    public void Unlock() => _confirm.Enabled = true;
    public void ClearTrade()
    {
        for (int i = 0; i < _playerItems.Length; i++) _playerItems[i] = null;
        foreach (var cell in _userGrid.Cells ?? System.Array.Empty<DXItemCell>())
        {
            if (cell.LinkedSourceSlot >= 0)
                GetSourceCell(new CellLinkInfo { GridType = cell.LinkedSourceGrid, Slot = cell.LinkedSourceSlot })?.UnlockForTrade();
            cell.Item = null;
            cell.LinkedSourceGrid = GridType.None;
            cell.LinkedSourceSlot = -1;
        }
        _playerGrid.RefreshGrid();
        _userGold.Text = "0";
        _playerGold.Text = "0";
        _confirm.Enabled = true;
        WindowManager.Close(this);
    }

    public void ApplyTradeAddItem(Library.Network.ServerPackets.TradeAddItem packet)
    {
        var link = packet?.Cell;
        if (link == null) return;
        var source = GetSourceCell(link);
        var target = _userGrid.Cells?.FirstOrDefault(c => c.LinkedSourceGrid == link.GridType && c.LinkedSourceSlot == link.Slot);
        if (!packet.Success)
        {
            if (target != null)
            {
                target.Item = null;
                target.LinkedSourceGrid = GridType.None;
                target.LinkedSourceSlot = -1;
                target.LinkChanged?.Invoke(target);
            }
            source?.UnlockForTrade();
            return;
        }

        if (source == null || target == null)
        {
            // 成功回包可能晚于窗口清理/重复回包；来源不能因找不到展示格而永久保持交易锁。
            source?.UnlockForTrade();
            return;
        }
        DXItemCell.SetCellItem(target, new ClientUserItem(source.Item, Math.Clamp(link.Count, 1, source.Item.Count)));
        target.RefreshItem();
        source.Locked = true;
        source.UpdateBorder();
    }

    private static DXItemCell GetSourceCell(CellLinkInfo link)
    {
        var game = GameScene.Game;
        if (game == null || link == null) return null;
        var cells = link.GridType switch
        {
            GridType.Inventory => game.InventoryCells,
            GridType.Equipment => game.EquipmentCells,
            GridType.Storage => game.StorageCells,
            GridType.PartsStorage => game.PartsStorageCells,
            GridType.CompanionInventory => game.CompanionInventoryCells,
            GridType.CompanionEquipment => game.CompanionEquipmentCells,
            _ => System.Array.Empty<DXItemCell>(),
        };
        return link.Slot >= 0 && link.Slot < cells.Length ? cells[link.Slot] : null;
    }

    public bool TryRouteItem(DXItemCell source)
    {
        if (GameScene.Game?.IsObserver == true) return false;
        var target = _userGrid.Cells?.FirstOrDefault(c => c.Item == null && c.LinkedSourceSlot < 0);
        if (target == null || source?.Item == null || source.GridType is not (GridType.Inventory or GridType.Storage or GridType.PartsStorage or GridType.Equipment) ||
            source.Item.Flags.HasFlag(UserItemFlags.Marriage)) return false;
        source.MoveItem(target);
        return true;
    }
    private void CloseTrade() { GameScene.Game?.SendTradeClose(); WindowManager.Close(this); }
}
