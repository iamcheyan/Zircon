using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 背包窗口 (移植自 Client/Scenes/Views/InventoryDialog.cs):
/// Interface 130 底图, 6x8 物品格 + 权重条 + 金币标签 + 排序/移除按钮。
/// </summary>
public partial class InventoryDialog : DXWindow
{
    public DXItemGrid Grid;
    public DXButton SortButton, TrashButton, SellButton, CloseButton;
    public DXControl WalletButton;
    public DXControl WeightBar;
    public DXLabel WeightLabel, GoldLabel, GgLabel;
    public readonly List<DXItemCell> SelectedItems = new();
    public readonly List<ItemType> SellableItemTypes = new();

    public InventoryMode InvMode { get; private set; } = InventoryMode.Normal;
    public bool IsSellMode => InvMode == InventoryMode.Sell;

    private bool _weightInit;
    private CurrencyInfo _primaryCurrency;
    private DXLabel _titleLabel, _goldTitle, _ggTitle;
    private readonly List<CellLinkInfo> _pendingSellLinks = new();

    public InventoryDialog()
    {
        // 原版 InventoryDialog 直接使用 Interface 130 背景图。
        HasTitle = false;
        Movable = true;
        Text = "背包";
        Size = new Vector2I(264, 436);

        var bg = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 130,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddControl(bg);

        // 原版虽然没有 DXWindow 标题栏，但背景图内部仍有独立的标题文字层。
        // 不能只给 DXWindow.Text 赋值，否则 HasTitle=false 时不会绘制标题。
        _titleLabel = new DXLabel
        {
            Text = "背包",
            FontSize = 10,
            TextColour = new Color(1f, .85f, .3f),
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Align = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center,
            AutoSize = false,
            Location = new Vector2I(52, 4),
            Size = new Vector2I(160, 20),
            IsControl = false,
        };
        AddControl(_titleLabel);

        CloseButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)Size.X - 30, 3),
        };
        CloseButton.MouseClick += (o, e) => Visible = false;
        AddControl(CloseButton);

        Grid = new DXItemGrid
        {
            GridSize = new Vector2I(6, 8),
            Location = new Vector2I(20, 39),
            GridPadding = 1,
            GridType = GridType.Inventory,
            ItemGrid = null, // GameScene 注入
        };
        AddControl(Grid);

        WeightBar = new DXControl
        {
            Location = new Vector2I(53, 355),
            Size = MirSkin.GetSize(LibraryFile.GameInter, 360),
        };
        WeightBar.BeforeDraw += DrawWeightFill;
        AddControl(WeightBar);

        WeightLabel = new DXLabel
        {
            TextColour = Colors.White,
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Align = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center,
            AutoSize = false,
            Size = new Vector2I(200, 18),
            IsControl = false,
        };
        AddControl(WeightLabel);

        _goldTitle = new DXLabel
        {
            Text = "金币",
            TextColour = new Color(0.85f, 0.68f, 0.2f),
            Location = new Vector2I(55, 381),
            AutoSize = false,
            Size = new Vector2I(97, 20),
            FontSize = 8,
            VAlign = VerticalAlignment.Center,
            IsControl = false,
        };
        AddControl(_goldTitle);

        GoldLabel = new DXLabel
        {
            Text = "0",
            TextColour = Colors.White,
            Location = new Vector2I(80, 381),
            AutoSize = false,
            Size = new Vector2I(97, 20),
            FontSize = 8,
            Align = HorizontalAlignment.Right,
            VAlign = VerticalAlignment.Center,
        };
        GoldLabel.MouseClick += (o, e) => GameScene.Game?.SelectCurrency(
            GameScene.Game.Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.Gold));
        AddControl(GoldLabel);

        _ggTitle = new DXLabel
        {
            Text = "GG",
            TextColour = new Color(1f, 0.55f, 0.2f),
            Location = new Vector2I(55, 400),
            AutoSize = false,
            Size = new Vector2I(97, 20),
            FontSize = 8,
            VAlign = VerticalAlignment.Center,
            IsControl = false,
        };
        AddControl(_ggTitle);

        GgLabel = new DXLabel
        {
            Text = "0",
            TextColour = Colors.White,
            Location = new Vector2I(80, 400),
            AutoSize = false,
            Size = new Vector2I(97, 20),
            FontSize = 8,
            Align = HorizontalAlignment.Right,
            VAlign = VerticalAlignment.Center,
        };
        GgLabel.MouseClick += (o, e) => GameScene.Game?.SelectCurrency(
            GameScene.Game.Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.GameGold));
        AddControl(GgLabel);

        WalletButton = new DXControl
        {
            Location = new Vector2I(8, 380),
            Size = new Vector2I(45, 40),
            // 原版 WalletLabel 没有可见文字或按钮底图，只提供点击热区。
            BackColour = Colors.Transparent,
            Border = false,
        };
        WalletButton.MouseClick += (o, e) => GameScene.Game?.ToggleCurrencyWindow();
        AddControl(WalletButton);

        SortButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 364,
            Location = new Vector2I(180, 384),
        };
        SortButton.MouseClick += (o, e) => GameScene.Game?.SendItemSort(GridType.Inventory);
        AddControl(SortButton);

        TrashButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 358,
            Location = new Vector2I(218, 384),
        };
        TrashButton.MouseClick += (o, e) => TrashItem();
        AddControl(TrashButton);

        SellButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 354,
            Location = new Vector2I(218, 384),
            Visible = false,
            Enabled = false,
            TooltipText = "出售选中物品",
        };
        SellButton.MouseClick += (o, e) => SellSelected();
        AddControl(SellButton);
    }

    private void TrashItem()
    {
        if (DXItemCell.SelectedCell == null) return;
        var cell = DXItemCell.SelectedCell;
        if (cell.Item == null) return;
        if (cell.GridType != GridType.Inventory) return;
        if (cell.Item.Flags.HasFlag(UserItemFlags.Locked) || cell.Item.Flags.HasFlag(UserItemFlags.Marriage)) return;

        cell.Locked = true;
        cell.UpdateBorder();
        DXItemCell.SelectedCell = null;
        GameScene.Game?.SendItemDelete(cell.GridType, cell.Slot);
    }

    public override void _Ready()
    {
        base._Ready();
        CenterWeightLabel();
        GameScene.Game?.RefreshInventoryWeights();
    }

    private void DrawWeightFill(object sender, EventArgs e)
    {
        var game = GameScene.Game;
        if (game == null) return;
        if (game.BagWeight <= 0) return;

        var stats = game.PlayerStats;
        if (stats == null || stats[Stat.BagWeight] <= 0) return;

        float percent = Math.Clamp(game.BagWeight / (float)stats[Stat.BagWeight], 0f, 1f);
        if (percent <= 0) return;

        var tex = MirSkin.GetTexture(LibraryFile.GameInter, 360);
        if (tex == null) return;
        var imgSize = tex.GetSize();
        WeightBar.DrawTextureRect(tex, new Rect2(0, 0, imgSize.X * percent, imgSize.Y), false);
    }

    public void CenterWeightLabel()
    {
        if (WeightLabel == null || WeightBar == null) return;
        var size = MirSkin.MeasureText(WeightLabel.Text, WeightLabel.FontSize);
        WeightLabel.Location = new Vector2I(
            WeightBar.Location.X + (int)((WeightBar.Size.X - size.X) / 2),
            WeightBar.Location.Y + (int)((WeightBar.Size.Y - size.Y) / 2));
    }

    /// <summary>GameScene 数据注入</summary>
    public void SetWeight(int bagWeight)
    {
        WeightLabel.Text = $"{bagWeight}";
        CenterWeightLabel();
        WeightBar.QueueRedraw();
    }

    public void SetCurrency(long gold, long gg)
    {
        if (!IsSellMode)
        {
            GoldLabel.Text = gold.ToString("N0");
            GgLabel.Text = gg.ToString("N0");
            return;
        }

        var current = GameScene.Game?.Currencies?.FirstOrDefault(x => x.Info == _primaryCurrency);
        GoldLabel.Text = (current?.Amount ?? 0).ToString("N0");
        GgLabel.Text = SaleTotal().ToString("N0");
    }

    /// <summary>原版 InventoryDialog.SellMode：背包负责多选物品和提交 NPCSell。</summary>
    public void SellMode(CurrencyInfo currency, IEnumerable<ItemType> sellableTypes)
    {
        _primaryCurrency = currency ?? Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.Type == CurrencyType.Gold);
        SellableItemTypes.Clear();
        if (sellableTypes != null) SellableItemTypes.AddRange(sellableTypes);
        ClearSaleSelection();
        InvMode = InventoryMode.Sell;
        _titleLabel.Text = "背包 [出售]";
        _goldTitle.Text = _primaryCurrency?.Abbreviation ?? "金币";
        _ggTitle.Text = "总计";
        _ggTitle.TextColour = new Color(.4f, .65f, 1f);
        GgLabel.Text = "0";
        TrashButton.Visible = false;
        SellButton.Visible = true;
        SellButton.Enabled = false;
        SetCurrency(0, 0);
    }

    /// <summary>原版 InventoryDialog.NormalMode：离开 NPC 出售状态并恢复普通按钮。</summary>
    public void NormalMode()
    {
        ClearSaleSelection();
        SellableItemTypes.Clear();
        _primaryCurrency = null;
        InvMode = InventoryMode.Normal;
        _titleLabel.Text = "背包";
        _goldTitle.Text = "金币";
        _goldTitle.TextColour = new Color(.85f, .68f, .2f);
        _ggTitle.Text = "GG";
        _ggTitle.TextColour = new Color(1f, .55f, .2f);
        TrashButton.Visible = true;
        SellButton.Visible = false;
        SellButton.Enabled = false;
        SetCurrency(
            GameScene.Game?.Currencies?.FirstOrDefault(x => x.Info?.Type == CurrencyType.Gold)?.Amount ?? 0,
            GameScene.Game?.Currencies?.FirstOrDefault(x => x.Info?.Type == CurrencyType.GameGold)?.Amount ?? 0);
    }

    /// <summary>出售模式下由背包格调用；返回 true 表示已消费这次点击。</summary>
    public bool TrySelectForSale(DXItemCell cell)
    {
        if (!IsSellMode || cell?.Item == null || cell.GridType != GridType.Inventory) return false;
        if (cell.Locked || cell.Item.Flags.HasFlag(UserItemFlags.Locked) ||
            cell.Item.Flags.HasFlag(UserItemFlags.Worthless) ||
            cell.Item.Flags.HasFlag(UserItemFlags.Marriage) ||
            cell.Item.Info?.CanSell != true ||
            (SellableItemTypes.Count > 0 && !SellableItemTypes.Contains(cell.Item.Info.ItemType)))
            return true;

        if (SelectedItems.Contains(cell))
        {
            SelectedItems.Remove(cell);
            cell.SaleSelected = false;
        }
        else
        {
            SelectedItems.Add(cell);
            cell.SaleSelected = true;
        }

        DXItemCell.SelectedCell = null;
        GgLabel.Text = SaleTotal().ToString("N0");
        SellButton.Enabled = SelectedItems.Count > 0;
        SellButton.TooltipText = SelectedItems.Count == 1 ? "出售" : "全部出售";
        return true;
    }

    private long SaleTotal()
    {
        decimal rate = _primaryCurrency?.ExchangeRate > 0M ? _primaryCurrency.ExchangeRate : 1M;
        return SelectedItems.Where(x => x?.Item != null)
            .Sum(x => (long)(x.Item.Price(x.Item.Count) / rate));
    }

    private void SellSelected()
    {
        if (!IsSellMode || SelectedItems.Count == 0 || GameScene.Game?.IsObserver == true) return;
        var links = SelectedItems.Where(x => x?.Item != null && !x.Locked)
            .Select(x => new CellLinkInfo { GridType = GridType.Inventory, Slot = x.Slot, Count = x.Item.Count })
            .ToList();
        if (links.Count == 0) return;

        foreach (var link in links)
        {
            var cell = Grid?.Cells?.FirstOrDefault(x => x.Slot == link.Slot);
            if (cell == null) continue;
            cell.Locked = true;
            cell.UpdateBorder();
            _pendingSellLinks.Add(link);
        }
        foreach (var cell in SelectedItems) cell.SaleSelected = false;
        SelectedItems.Clear();
        SellButton.Enabled = false;
        GameScene.Game?.SendNPCSell(links);
    }

    public void ItemsChanged(IEnumerable<CellLinkInfo> links, bool success)
    {
        var changed = new HashSet<int>((links ?? Enumerable.Empty<CellLinkInfo>())
            .Where(x => x?.GridType == GridType.Inventory).Select(x => x.Slot));
        if (changed.Count == 0) return;
        foreach (var cell in Grid?.Cells ?? Array.Empty<DXItemCell>())
        {
            if (cell != null && changed.Contains(cell.Slot))
            {
                cell.SaleSelected = false;
                if (!success) cell.Locked = false;
                cell.UpdateBorder();
            }
        }
        _pendingSellLinks.RemoveAll(x => changed.Contains(x.Slot));
    }

    private void ClearSaleSelection()
    {
        foreach (var cell in SelectedItems) if (cell != null) cell.SaleSelected = false;
        SelectedItems.Clear();
        GgLabel.Text = "0";
        DXItemCell.SelectedCell = null;
    }
}
