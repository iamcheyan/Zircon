using System;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 物品格子 (移植自 Client/Controls/DXItemCell.cs 的核心行为):
/// Item getter/setter 直读直写底层数组 (GameScene.Inventory/Equipment/Storage),
/// setter 触发 RefreshItem 刷新图标/数量/边框。左键拿起/放下/移动,
/// 右键/双击使用, 中键锁定。腰带格用 QuickInfo/QuickItem 存"链接"而非真实物品。
/// </summary>
public partial class DXItemCell : DXControl
{
    public const int CellWidth = 36;
    public const int CellHeight = 36;

    /// <summary>拿起状态: 记录源格子, 点另一格完成移动 (原版静态 SelectedCell)</summary>
    public static DXItemCell SelectedCell;

    public ClientUserItem[] ItemGrid;
    public int Slot;

    public DXItemGrid HostGrid;
    public bool Locked;
    public bool ReadOnly;

    /// <summary>装备槽空时隐藏 (不画边框/底)</summary>
    public bool Hidden;

    /// <summary>物品图标在格内水平/垂直居中显示</summary>
    public bool CenterImage = true;

    private GridType _gridType;
    public GridType GridType
    {
        get => _gridType;
        set { _gridType = value; QueueRedraw(); }
    }

    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            if (_selected && SelectedCell != this) SelectedCell = this;
            if (!_selected && SelectedCell == this) SelectedCell = null;
            UpdateBorder();
        }
    }

    // ---- 腰带: 链接物品 (非真实持有) ----
    private ItemInfo _quickInfo;
    public ItemInfo QuickInfo
    {
        get => _quickInfo;
        set
        {
            if (_quickInfo == value) return;
            _quickInfo = value;

            if (value == null)
            {
                QuickInfoItem = null;
                if (GameScene.Game != null && Slot < GameScene.Game.BeltLinks.Length)
                    GameScene.Game.BeltLinks[Slot].LinkInfoIndex = -1;
            }
            else
            {
                QuickInfoItem = new ClientUserItem(value, 1);
                QuickItem = null;
                if (GameScene.Game != null && Slot < GameScene.Game.BeltLinks.Length)
                    GameScene.Game.BeltLinks[Slot].LinkInfoIndex = value.Index;
            }
            RefreshItem();
        }
    }

    public ClientUserItem QuickInfoItem { get; private set; }

    private ClientUserItem _quickItem;
    public ClientUserItem QuickItem
    {
        get => _quickItem;
        set
        {
            if (_quickItem == value) return;
            _quickItem = value;
            if (GameScene.Game != null && Slot < GameScene.Game.BeltLinks.Length)
                GameScene.Game.BeltLinks[Slot].LinkItemIndex = value?.Index ?? -1;
            RefreshItem();
        }
    }

    // ---- 物品: 直读直写底层数组 ----
    public ClientUserItem Item
    {
        get
        {
            if (GridType == GridType.Belt)
            {
                if (QuickInfo != null) return QuickInfoItem;
                return QuickItem;
            }
            if (ItemGrid == null || Slot >= ItemGrid.Length) return null;
            return ItemGrid[Slot];
        }
        set
        {
            if (ItemGrid == null || Slot >= ItemGrid.Length || ItemGrid[Slot] == value) return;
            ItemGrid[Slot] = value;
            if (value != null) value.Slot = Slot;
            RefreshItem();
        }
    }

    public event EventHandler<EventArgs> ItemChanged;

    private DXLabel _countLabel;

    public DXItemCell()
    {
        Size = new Vector2(CellWidth, CellHeight);
    }

    public override void _Ready()
    {
        base._Ready();

        _countLabel = new DXLabel
        {
            FontSize = 10,
            TextColour = Colors.White,
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Align = HorizontalAlignment.Right,
            VAlign = VerticalAlignment.Bottom,
            IsControl = false,
        };
        _countLabel.Size = new Vector2(CellWidth - 2, CellHeight - 2);
        _countLabel.Location = new Vector2I(1, 1);
        AddControl(_countLabel);
        MouseEnter += OnHoverEnter;
        MouseLeave += OnHoverLeave;
        UpdateBorder();
    }

    private void OnHoverEnter(object sender, EventArgs e)
    {
        GameScene.Game?.SetHoverItem(Item);
        UpdateBorder();
    }

    private void OnHoverLeave(object sender, EventArgs e)
    {
        GameScene.Game?.SetHoverItem(null);
        UpdateBorder();
    }

    // ---- 绘制 ----

    protected override void DrawControl()
    {
        // 装备槽: 空槽图由面板画 (BeforeDraw), 有物品时这里画图标
        var item = Item;
        if (item != null)
            DrawItemIcon(item);
    }

    private void DrawItemIcon(ClientUserItem item)
    {
        ItemInfo info = item.Info;
        int drawIndex;

        if (IsCurrencyItem(info))
            drawIndex = CurrencyImage(info, item.Count);
        else
        {
            if (info.ItemEffect == ItemEffect.ItemPart && item.AddedStats != null && item.AddedStats[Stat.ItemIndex] > 0)
            {
                var partInfo = Globals.ItemInfoList?.Binding.FirstOrDefault(x => x.Index == item.AddedStats[Stat.ItemIndex]);
                if (partInfo != null) info = partInfo;
            }
            drawIndex = info.Image;
        }

        var tex = MirSkin.GetTexture(LibraryFile.StoreItem, drawIndex);
        if (tex == null) return;

        var imgSize = tex.GetSize();
        float x = CenterImage ? (Size.X - imgSize.X) / 2f : 0;
        float y = CenterImage ? (Size.Y - imgSize.Y) / 2f : 0;
        var colour = item.Count > 0 ? Colors.White : new Color(0.5f, 0.5f, 0.5f, 1f);
        DrawTextureRect(tex, new Rect2(x, y, imgSize.X, imgSize.Y), false, colour);

        // 角标: New=47, Lock=48, 不可用=49, ItemPart=103 (GameInter2)
        if (item.New)
            DrawBadge(47);
        if (item.Flags.HasFlag(UserItemFlags.Locked) && !Hidden && GridType != GridType.Inspect)
            DrawBadge(48);
        if (GameScene.Game != null && !GameScene.Game.CanUseItem(item) && !Hidden && GridType != GridType.Inspect)
            DrawBadge(49);
        if (item.Info.ItemEffect == ItemEffect.ItemPart)
            DrawBadge(103);
    }

    private void DrawBadge(int index)
    {
        var tex = MirSkin.GetTexture(LibraryFile.GameInter2, index);
        if (tex == null) return;
        var imgSize = tex.GetSize();
        float x = index == 49 || index == 103 ? Size.X - imgSize.X - 1 : 1;
        DrawTextureRect(tex, new Rect2(x, 1, imgSize.X, imgSize.Y), false);
    }

    public void UpdateBorder()
    {
        if (Hidden)
        {
            Border = false;
            return;
        }

        bool active = IsHovered || Selected || Locked;
        Border = active;
        BorderColour = active ? Colors.Lime : new Color(0.45f, 0.45f, 0.45f);
        QueueRedraw();
    }

    /// <summary>数组/数量/链接变化后刷新显示 (原版 RefreshItem)</summary>
    public void RefreshItem()
    {
        var item = Item;

        // 腰带格: 数量 = 背包同类物品合计
        if (GridType == GridType.Belt && QuickInfo != null && QuickInfoItem != null && GameScene.Game != null)
        {
            long sum = GameScene.Game.Inventory.Where(x => x?.Info == QuickInfo).Sum(x => x.Count);
            QuickInfoItem.Count = sum;
        }

        bool showCount = !Hidden && item != null
            && !IsCurrencyItem(item.Info) && item.Info.ItemEffect != ItemEffect.Experience
            && (item.Info.StackSize > 1 || item.Count > 1);

        _countLabel.Visible = showCount;
        _countLabel.Text = item?.Count.ToString() ?? "";

        UpdateBorder();
        QueueRedraw();
    }

    private static bool IsCurrencyItem(ItemInfo info)
    {
        return Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.DropItem == info) != null;
    }

    private static int CurrencyImage(ItemInfo info, long count)
    {
        var currency = Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.DropItem == info);
        if (currency == null) return info.Image;

        var image = currency.Images.OrderByDescending(x => x.Amount).FirstOrDefault(x => x.Amount <= count);
        return image?.Image ?? currency.DropItem.Image;
    }

    // ---- 交互 ----

    public override void _GuiInput(InputEvent e)
    {
        if (!IsEnabled) return;

        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    FocusControl = this;
                    if (!Locked) MoveItem();
                }
                else if (mb.DoubleClick)
                {
                    UseItem();
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                if (!Locked) UseItem();
            }
            else if (mb.ButtonIndex == MouseButton.Middle && mb.Pressed)
            {
                ToggleLock();
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown)
            {
                // 滚轮: 转发给基类触发 MouseWheel (仓库滚动)
                base._GuiInput(e);
            }
            AcceptEvent();
            return;
        }

        if (e is InputEventMouseMotion)
        {
            // 悬停刷新物品提示 (原版 MouseControl == this -> MouseItem)
            GameScene.Game?.SetHoverItem(Item);
            base._GuiInput(e);
        }
    }

    /// <summary>拿起/放下/移动 (原版无参 MoveItem)</summary>
    public void MoveItem()
    {
        if (Locked || ReadOnly || GameScene.Game == null) return;

        if (SelectedCell == null)
        {
            if (Item == null) return;
            SelectedCell = this;
            return;
        }

        // 放回原位
        if (SelectedCell == this || SelectedCell.Item == null)
        {
            SelectedCell = null;
            return;
        }

        var from = SelectedCell;

        switch (from.GridType)
        {
            case GridType.Equipment:
                // 装备身上的物品不互移
                if (GridType == GridType.Equipment) return;
                if (Item == null || (from.Item.Info == Item.Info && from.Item.Count < Item.Info.StackSize))
                    from.MoveItem(this);
                else if (HostGrid != null)
                    from.MoveItem(HostGrid);
                SelectedCell = null;
                return;
        }

        switch (GridType)
        {
            case GridType.Storage:
                if (from.Item.Info.ItemEffect == ItemEffect.ItemPart) return;
                break;
            case GridType.PartsStorage:
                if (from.Item.Info.ItemEffect != ItemEffect.ItemPart) return;
                break;
            case GridType.Equipment:
                if (!Functions.CorrectSlot(from.Item.Info.ItemType, (EquipmentSlot)Slot) || from.GridType == GridType.Belt) return;
                ToEquipment(from);
                return;
        }

        from.MoveItem(this);
    }

    /// <summary>穿戴到本装备槽 (原版 ToEquipment)</summary>
    public void ToEquipment(DXItemCell fromCell)
    {
        if (Locked || ReadOnly || GameScene.Game == null) return;
        if (!GameScene.Game.CanWearItem(fromCell.Item, (EquipmentSlot)Slot)) return;

        if (fromCell == SelectedCell) SelectedCell = null;

        bool merge = Item != null && Item.Info == fromCell.Item.Info && Item.Count < Item.Info.StackSize &&
            Item.Flags == fromCell.Item.Flags && Item.AddedStats.Compare(fromCell.Item.AddedStats);

        Locked = true;
        fromCell.Locked = true;
        GameScene.Game.SendItemMove(fromCell.GridType, GridType, fromCell.Slot, Slot, merge);
    }

    /// <summary>移动到指定格 (原版 MoveItem(DXItemCell), 含腰带链接分支)</summary>
    public void MoveItem(DXItemCell toCell)
    {
        if (Locked || ReadOnly || GameScene.Game == null) return;

        // 目标是腰带: 建立链接
        if (toCell.GridType == GridType.Belt)
        {
            ItemInfo info = null;
            ClientUserItem item = null;

            if (GridType == toCell.GridType)
            {
                info = toCell.QuickInfo;
                item = toCell.QuickItem;
            }

            if (Item != null && Item.Info.ShouldLinkInfo)
                toCell.QuickInfo = Item.Info;
            else if (Item != null)
                toCell.QuickItem = Item;

            if (GridType == toCell.GridType)
            {
                toCell.QuickInfo = info;
                toCell.QuickItem = item;
                var link = GameScene.Game.BeltLinks[Slot];
                GameScene.Game.SendBeltLinkChanged(link.Slot, link.LinkInfoIndex, link.LinkItemIndex);
            }

            if (Selected) SelectedCell = null;

            var newLink = GameScene.Game.BeltLinks[toCell.Slot];
            GameScene.Game.SendBeltLinkChanged(newLink.Slot, newLink.LinkInfoIndex, newLink.LinkItemIndex);
            return;
        }

        // 源是腰带: 清除链接
        if (GridType == GridType.Belt)
        {
            QuickInfo = null;
            QuickItem = null;
            var link = GameScene.Game.BeltLinks[Slot];
            GameScene.Game.SendBeltLinkChanged(link.Slot, link.LinkInfoIndex, link.LinkItemIndex);
            if (Selected) SelectedCell = null;
            return;
        }

        if (GridType == GridType.PartsStorage && toCell.Item != null && toCell.Item.Info.ItemEffect != ItemEffect.ItemPart) return;
        if (GridType == GridType.Storage && toCell.Item != null && toCell.Item.Info.ItemEffect == ItemEffect.ItemPart) return;

        bool merge = toCell.Item != null && toCell.Item.Info == Item.Info && toCell.Item.Count < toCell.Item.Info.StackSize &&
            Item.Flags == toCell.Item.Flags && Item.AddedStats.Compare(toCell.Item.AddedStats);

        if (Selected) SelectedCell = null;

        Locked = true;
        toCell.Locked = true;
        GameScene.Game.SendItemMove(GridType, toCell.GridType, Slot, toCell.Slot, merge);
    }

    /// <summary>移动到网格的空位/可堆叠格 (原版 MoveItem(DXItemGrid) 简化)</summary>
    public bool MoveItem(DXItemGrid toGrid)
    {
        if (toGrid.GridType == GridType.Belt || toGrid.GridType == GridType.AutoPotion) return false;
        if (Locked || GameScene.Game == null) return false;

        DXItemCell toCell = null;
        bool merge = false;

        foreach (var cell in toGrid.Cells)
        {
            if (cell.Locked || !cell.Enabled) continue;

            var toItem = cell.Item;
            if (toItem == null)
            {
                if (toCell == null) toCell = cell;
                continue;
            }

            if (toItem.Info != Item.Info || toItem.Count >= toItem.Info.StackSize) continue;
            if (Item.Flags != toItem.Flags || !Item.AddedStats.Compare(toItem.AddedStats)) continue;

            toCell = cell;
            merge = true;
            break;
        }

        if (toCell == null) return false;

        if (Selected) SelectedCell = null;
        Locked = true;
        toCell.Locked = true;
        GameScene.Game.SendItemMove(GridType, toCell.GridType, Slot, toCell.Slot, merge);
        return true;
    }

    /// <summary>使用物品: 可穿戴->穿戴, 消耗品/卷轴->C.ItemUse (原版 UseItem 简化)</summary>
    public bool UseItem()
    {
        if (Item == null || Locked || ReadOnly || SelectedCell == this || GameScene.Game == null) return false;
        if (!GameScene.Game.CanUseItem(Item)) return false;

        // 腰带格: 找到背包里的本体使用
        if (GridType == GridType.Belt)
        {
            DXItemCell cell;
            if (QuickInfo != null)
                cell = GameScene.Game.InventoryCells.FirstOrDefault(x => x?.Item?.Info == QuickInfo);
            else
                cell = GameScene.Game.InventoryCells.FirstOrDefault(x => x?.Item == QuickItem);
            return cell?.UseItem() == true;
        }

        switch (Item.Info.ItemType)
        {
            case ItemType.Weapon:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Weapon].ToEquipment(this);
                return true;
            case ItemType.Armour:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Armour].ToEquipment(this);
                return true;
            case ItemType.Torch:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Torch].ToEquipment(this);
                return true;
            case ItemType.Helmet:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Helmet].ToEquipment(this);
                return true;
            case ItemType.Necklace:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Necklace].ToEquipment(this);
                return true;
            case ItemType.Bracelet:
                if (GameScene.Game.EquipmentCells[(int)EquipmentSlot.BraceletL].Item == null)
                    GameScene.Game.EquipmentCells[(int)EquipmentSlot.BraceletL].ToEquipment(this);
                else
                    GameScene.Game.EquipmentCells[(int)EquipmentSlot.BraceletR].ToEquipment(this);
                return true;
            case ItemType.Ring:
                if (GameScene.Game.EquipmentCells[(int)EquipmentSlot.RingL].Item == null)
                    GameScene.Game.EquipmentCells[(int)EquipmentSlot.RingL].ToEquipment(this);
                else
                    GameScene.Game.EquipmentCells[(int)EquipmentSlot.RingR].ToEquipment(this);
                return true;
            case ItemType.Shoes:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Shoes].ToEquipment(this);
                return true;
            case ItemType.Poison:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Poison].ToEquipment(this);
                return true;
            case ItemType.Amulet:
            case ItemType.DarkStone:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Amulet].ToEquipment(this);
                return true;
            case ItemType.Flower:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Flower].ToEquipment(this);
                return true;
            case ItemType.Emblem:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Emblem].ToEquipment(this);
                return true;
            case ItemType.Shield:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Shield].ToEquipment(this);
                return true;
            case ItemType.Costume:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.Costume].ToEquipment(this);
                return true;
            case ItemType.HorseArmour:
                GameScene.Game.EquipmentCells[(int)EquipmentSlot.HorseArmour].ToEquipment(this);
                return true;
            case ItemType.Consumable:
            case ItemType.Scroll:
            case ItemType.ItemPart:
            case ItemType.Book:
                if (GridType != GridType.Inventory && GridType != GridType.PartsStorage) return false;
                if (GameScene.Game.IsUseItemOnCooldown(Item)) return false;

                GameScene.Game.SetUseItemCooldown(Math.Max(250, Item.Info.Durability));
                Locked = true;
                GameScene.Game.SendItemUse(GridType, Slot);
                return true;
            default:
                return false;
        }
    }

    /// <summary>中键: 锁定切换 (原版 Ctrl+中键为聊天链接, 略)</summary>
    public void ToggleLock()
    {
        if (Item == null || GameScene.Game == null) return;
        bool locked = !Item.Flags.HasFlag(UserItemFlags.Locked);
        GameScene.Game.SendItemLock(GridType, Slot, locked);
    }
}
