using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 BundleDialog：GameInter 3350 背景、16 格预览和三种领取模式。</summary>
public sealed partial class BundleDialog : DXWindow
{
    public DXItemGrid Grid { get; }
    private readonly ClientUserItem[] _items = new ClientUserItem[16];
    private readonly DXButton _confirm;
    private DXItemCell _selectedBundle;
    private ClientUserItem _selectedBundleItem;
    private BundleInfo _info;
    private int _slot = -1;
    private int _selectedIndex = -1;

    public BundleDialog()
    {
        HasTitle = false; HasFooter = false; Movable = true; Size = new Vector2I(180, 268); DropShadow = true;
        AddControl(new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 3350, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => Close(); AddControl(close);
        AddControl(new DXLabel { Text = "物品包", FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(180, 18), IsControl = false });
        Grid = new DXItemGrid { GridSize = new Vector2I(4, 4), Location = new Vector2I(15, 48), GridType = GridType.Bundle, GridPadding = 1, ItemGrid = _items, ReadOnly = true };
        AddControl(Grid);
        foreach (var cell in Grid.Cells) cell.MouseClick += (s, e) => SelectCell((DXItemCell)s);
        _confirm = new DXButton { Text = "", FontSize = 9, Location = new Vector2I(40, 225), Size = new Vector2I(100, 27), LibraryFile = LibraryFile.Interface, Index = -1, Enabled = false };
        _confirm.MouseClick += (s, e) => Confirm(); AddControl(_confirm);
    }

    public void Open(int slot, List<ClientBundleItemInfo> contents)
    {
        var item = GameScene.Game?.Inventory?.ElementAtOrDefault(slot);
        if (item?.Info == null || item.Info.ItemType != ItemType.Bundle) return;
        _info = Globals.BundleInfoList?.Binding?.FirstOrDefault(x => x.Index == item.Info.Shape);
        if (_info == null || _info.Contents == null || _info.Contents.Count == 0) return;
        _slot = slot; _selectedIndex = -1; _selectedBundle = GameScene.Game?.InventoryCells?.ElementAtOrDefault(slot); _selectedBundleItem = item;
        _selectedBundle?.Locked = true; _selectedBundle?.UpdateBorder();
        ResetCells();
        _confirm.Text = _info.Type switch { BundleType.AnyOf => "随机领取", BundleType.AllOf => "全部领取", BundleType.OneOf => "选择并领取", _ => "确认" };
        _confirm.Enabled = _info.Type != BundleType.OneOf;
        foreach (var entry in contents ?? new List<ClientBundleItemInfo>())
        {
            if (entry == null || entry.Slot < 0 || entry.Slot >= _items.Length) continue;
            var itemInfo = entry.ItemInfo ?? Globals.ItemInfoList?.Binding?.FirstOrDefault(x => x.Index == entry.ItemIndex);
            if (itemInfo != null) _items[entry.Slot] = new ClientUserItem(itemInfo, entry.Amount);
        }
        Grid.RefreshGrid();
        if (_info.AutoOpen)
        {
            GameScene.Game?.SendBundleConfirm(_slot, -1);
            return;
        }
        WindowManager.Open(this, GameScene.Game?.UILayer);
    }

    private void ResetCells()
    {
        for (int i = 0; i < Grid.Cells.Length; i++)
        {
            Grid.Cells[i].Selected = false; Grid.Cells[i].Locked = false; _items[i] = null;
        }
        _selectedIndex = -1;
    }

    private void SelectCell(DXItemCell cell)
    {
        if (_info?.Type != BundleType.OneOf || cell?.Item == null) return;
        _selectedIndex = cell.Slot;
        foreach (var other in Grid.Cells) other.Selected = other == cell;
        _confirm.Enabled = true;
    }

    private void Confirm()
    {
        if (_slot < 0 || _info == null) return;
        _confirm.Enabled = false;
        GameScene.Game?.SendBundleConfirm(_slot, _info.Type == BundleType.OneOf ? _selectedIndex : -1);
        // 服务端会先回 ItemChanged，再回 BundleClose；保持来源锁和窗口
        // 到 BundleClose，避免确认包尚未处理时重复使用同一个礼包。
    }

    public override void Close()
    {
        if (_selectedBundle != null && ReferenceEquals(_selectedBundle.Item, _selectedBundleItem))
        {
            _selectedBundle.Locked = false;
            _selectedBundle.UpdateBorder();
        }
        _selectedBundle = null; _selectedBundleItem = null; _info = null; ResetCells(); base.Close();
    }

    public static bool ShouldUnlockSource(ClientUserItem current, ClientUserItem expected)
        => ReferenceEquals(current, expected);
}
