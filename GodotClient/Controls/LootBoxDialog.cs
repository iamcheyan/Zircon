using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 LootBoxDialog：GameInter2 2900 背景、15 格开箱状态与重抽/领取操作。</summary>
public sealed partial class LootBoxDialog : DXWindow
{
    public DXItemGrid Grid { get; }
    public DXLabel Message { get; }
    private readonly ClientUserItem[] _items = new ClientUserItem[LootBoxInfo.SlotSize];
    private readonly DXButton _rerollCount, _rerollButton, _takeItemsButton, _confirmChoiceButton;
    private DXItemCell _selectedLootBox;
    private ClientUserItem _selectedLootBoxItem;
    private LootBoxInfo _info;
    private int _selectedIndex = -1;
    private int _state;

    public LootBoxDialog()
    {
        HasTitle = false; HasFooter = false; HasTopBorder = false; Movable = false; Size = new Vector2I(260, 296); DropShadow = true;
        AddControl(new DXImageControl { LibraryFile = LibraryFile.GameInter2, Index = 2900, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(233, 1) };
        close.MouseClick += (s, e) => Close(); AddControl(close);
        Grid = new DXItemGrid { GridSize = new Vector2I(5, 3), Location = new Vector2I(18, 24), GridType = GridType.LootBox, GridPadding = 4.5f, ItemGrid = _items, ReadOnly = true };
        AddControl(Grid);
        foreach (var cell in Grid.Cells) cell.MouseClick += (s, e) => SelectCell((DXItemCell)s);
        Message = new DXLabel { Text = string.Empty, FontSize = 9, TextColour = Colors.White, DrawOutline = true, OutlineColour = Colors.Black, Size = new Vector2I(235, 62), Location = new Vector2I(14, 170), IsControl = false };
        AddControl(Message);

        _rerollCount = new DXButton { LibraryFile = LibraryFile.GameInter2, Index = 2920, PressedIndex = 2920, HoverIndex = 2920, Location = new Vector2I(15, 235), Size = new Vector2I(128, 20), CanBePressed = false, Visible = false, FontSize = 8 };
        _rerollButton = new DXButton { LibraryFile = LibraryFile.GameInter2, Index = 2926, PressedIndex = 2925, HoverIndex = 2927, Location = new Vector2I(15, 260), Size = new Vector2I(128, 20), Visible = false, FontSize = 8 };
        _rerollButton.MouseClick += (s, e) => Reroll();
        AddControl(_rerollCount); AddControl(_rerollButton);
        _takeItemsButton = ActionButton(Lang.LootBoxItemLabel, 80, 245);
        _confirmChoiceButton = ActionButton(Lang.LootBoxConfirmLabel, 145, 245);
        _takeItemsButton.MouseClick += (s, e) => TakeItems();
        _confirmChoiceButton.MouseClick += (s, e) => ConfirmChoice();
        AddControl(_takeItemsButton); AddControl(_confirmChoiceButton);
    }

    private DXButton ActionButton(string text, int x, int y)
        => new() { Text = text, FontSize = 9, Location = new Vector2I(x, y), Size = new Vector2I(100, 27), LibraryFile = LibraryFile.Interface, Index = -1, Visible = false };

    public void Open(int slot, List<ClientLootBoxItemInfo> contents)
    {
        var item = GameScene.Game?.Inventory?.ElementAtOrDefault(slot);
        if (item?.Info == null || item.Info.ItemType != ItemType.LootBox) return;
        _info = Globals.LootBoxInfoList?.Binding?.FirstOrDefault(x => x.Index == item.Info.Shape);
        if (_info == null || _info.Contents == null || _info.Contents.Count == 0) return;

        _selectedLootBox = GameScene.Game?.InventoryCells?.ElementAtOrDefault(slot);
        _selectedLootBoxItem = item;
        _selectedLootBox?.Locked = true;
        _selectedLootBox?.UpdateBorder();
        _selectedIndex = -1;
        _state = item.AddedStats?[Stat.Counter2] ?? 0;
        int rerollCount = item.AddedStats?[Stat.Counter1] ?? 0;
        string currency = _info.Currency?.Name ?? Lang.LootBoxGoldLabel;
        int unlockedCount = LootBoxCountUnlocked(item.CurrentDurability);
        Message.Text = _state == 1
            ? Lang.LootBoxConfirmLabel2
            : string.Format(Lang.LootBoxRewardLabel, Globals.LootBoxRevealCost * unlockedCount, currency);

        _rerollCount.Text = string.Format(Lang.LootBoxUi376Label, rerollCount);
        _rerollButton.Text = $"重抽 ({Globals.LootBoxRerollCost} {(_info.Currency?.Abbreviation ?? "GG")})";
        _rerollCount.Visible = _state == 1;
        _rerollButton.Visible = _state == 1;
        _rerollButton.Enabled = _state == 1 && rerollCount > 0;
        _takeItemsButton.Visible = _state != 1;
        _confirmChoiceButton.Visible = _state == 1;
        // 原版：状态 0/2 可直接领取全部结果；状态 1 可直接确认重抽后的选择。
        // SelectedIndex 只用于状态 2 的逐格揭示，不是领取/确认的前置条件。
        _takeItemsButton.Enabled = _state != 1;
        _confirmChoiceButton.Enabled = _state == 1;
        ResetCells();

        foreach (var entry in contents ?? new List<ClientLootBoxItemInfo>())
        {
            if (entry == null || entry.Slot < 0 || entry.Slot >= _items.Length) continue;
            var itemInfo = entry.ItemInfo ?? Globals.ItemInfoList?.Binding?.FirstOrDefault(x => x.Index == entry.ItemIndex);
            if (itemInfo != null) _items[entry.Slot] = new ClientUserItem(itemInfo, entry.Amount);
        }
        // 状态 2 表示服务器只允许逐格揭示，空格显示开箱物品并保持锁定。
        if (_state == 2)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null) _items[i] = new ClientUserItem(item.Info, 1);
                Grid.Cells[i].Locked = true;
                Grid.Cells[i].UpdateBorder();
            }
        }
        Grid.RefreshGrid();
        WindowManager.Open(this, GameScene.Game?.UILayer);
    }

    private void ResetCells()
    {
        _selectedIndex = -1;
        for (int i = 0; i < Grid.Cells.Length; i++)
        {
            Grid.Cells[i].Selected = false;
            Grid.Cells[i].Locked = false;
            _items[i] = null;
        }
        _takeItemsButton.Enabled = false;
        _confirmChoiceButton.Enabled = false;
    }

    private void SelectCell(DXItemCell cell)
    {
        if (cell?.Item == null) return;
        _selectedIndex = cell.Slot;
        foreach (var other in Grid.Cells) other.Selected = other == cell;
        if (_state == 2 && cell.Locked)
        {
            var currency = _info?.Currency?.Name ?? Lang.LootBoxGoldLabel;
            int unlockedCount = LootBoxCountUnlocked(_selectedLootBox?.Item?.CurrentDurability ?? 0);
            if (unlockedCount <= 0)
            {
                GameScene.Game?.SendLootBoxReveal(_selectedLootBox.Slot, _selectedIndex);
                return;
            }
            var confirm = new ConfirmDialog(string.Format(Lang.LootBoxUi379Label, Globals.LootBoxRevealCost * unlockedCount, currency), Lang.LootBoxRewardLabel2, () =>
            {
                if (!HasCurrency(_info?.Currency, Globals.LootBoxRevealCost * unlockedCount)) return;
                GameScene.Game?.SendLootBoxReveal(_selectedLootBox.Slot, _selectedIndex);
            });
            WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
            return;
        }
        _takeItemsButton.Enabled = _state != 1;
        _confirmChoiceButton.Enabled = _state == 1;
    }

    private void Reroll()
    {
        if (_selectedLootBox == null || !_rerollButton.Enabled) return;
        string currency = _info?.Currency?.Name ?? Lang.LootBoxGoldLabel;
        var confirm = new ConfirmDialog(string.Format(Lang.LootBoxOkLabel, Globals.LootBoxRerollCost, currency), Lang.LootBoxConfirmLabel3, () =>
        {
            if (!HasCurrency(_info?.Currency, Globals.LootBoxRerollCost)) return;
            _rerollButton.Enabled = false;
            GameScene.Game?.SendLootBoxReroll(_selectedLootBox.Slot);
        });
        WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
    }

    private void TakeItems()
    {
        if (_selectedLootBox == null || !_takeItemsButton.Enabled) return;
        var confirm = new ConfirmDialog(Lang.LootBoxOkLabel2, Lang.LootBoxItemLabel, () =>
        {
            _takeItemsButton.Enabled = false;
            GameScene.Game?.SendLootBoxTake(_selectedLootBox.Slot, _selectedIndex);
        });
        WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
    }

    private void ConfirmChoice()
    {
        if (_selectedLootBox == null || _state != 1 || !_confirmChoiceButton.Enabled) return;
        var confirm = new ConfirmDialog(Lang.LootBoxOkLabel3, Lang.LootBoxConfirmLabel, () =>
        {
            _confirmChoiceButton.Enabled = false;
            GameScene.Game?.SendLootBoxConfirm(_selectedLootBox.Slot);
        });
        WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
    }

    public override void Close()
    {
        if (_selectedLootBox != null && ReferenceEquals(_selectedLootBox.Item, _selectedLootBoxItem))
        {
            _selectedLootBox.Locked = false;
            _selectedLootBox.UpdateBorder();
        }
        _selectedLootBox = null; _selectedLootBoxItem = null; _info = null; ResetCells(); base.Close();
    }

    private static int LootBoxCountUnlocked(int state)
    {
        int count = 0;
        for (int i = 0; i < 16; i++) if ((state & (1 << i)) != 0) count++;
        return count;
    }

    private static bool HasCurrency(CurrencyInfo currency, long amount)
    {
        if (amount < 0) return false;
        var wanted = currency?.Type ?? CurrencyType.GameGold;
        return (GameScene.Game?.Currencies?.FirstOrDefault(x => x?.Info?.Type == wanted)?.Amount ?? 0) >= amount;
    }

    public static bool CanRevealWithoutPrompt(int unlockedCount) => unlockedCount <= 0;
    public static bool CanSpend(long balance, long cost) => balance >= 0 && cost >= 0 && balance >= cost;
    public static bool ShouldUnlockSource(ClientUserItem current, ClientUserItem expected)
        => ReferenceEquals(current, expected);
}
