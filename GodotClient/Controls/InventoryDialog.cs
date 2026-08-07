using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 背包窗口 (移植自 Client/Scenes/Views/InventoryDialog.cs):
/// Interface 130 底图, 6x8 物品格 + 权重条 + 金币标签 + 排序/移除按钮。
/// </summary>
public partial class InventoryDialog : DXWindow
{
    public DXItemGrid Grid;
    public DXButton SortButton, TrashButton, CloseButton;
    public DXControl WeightBar;
    public DXLabel WeightLabel, GoldLabel, GgLabel;

    private bool _weightInit;

    public InventoryDialog()
    {
        HasTitle = true;
        Text = "背包";
        Size = new Vector2I(263, 458);

        var bg = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 130,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddControl(bg);

        CloseButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)ClientArea.Size.X - 30, 3),
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

        var goldTitle = new DXLabel
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
        AddControl(goldTitle);

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
            IsControl = false,
        };
        AddControl(GoldLabel);

        var ggTitle = new DXLabel
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
        AddControl(ggTitle);

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
            IsControl = false,
        };
        AddControl(GgLabel);

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
    }

    private void TrashItem()
    {
        if (DXItemCell.SelectedCell == null) return;
        var cell = DXItemCell.SelectedCell;
        if (cell.Item == null) return;
        if (cell.GridType != GridType.Inventory && cell.GridType != GridType.Storage) return;

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
        GoldLabel.Text = gold.ToString("N0");
        GgLabel.Text = gg.ToString("N0");
    }
}
