using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 仓库窗口 (移植自 Client/Scenes/Views/StorageDialog.cs):
/// Interface 121 底图, 10 列可滚动网格 (VisibleHeight=10, StorageSize 决定行数)。
/// M9 简化: 只显示主仓库 (PartsStorage 数据照常填充, 界面隐藏)。
/// </summary>
public partial class StorageDialog : DXWindow
{
    public DXItemGrid Grid;
    public DXVScrollBar ScrollBar;
    public DXButton SortButton;

    public StorageDialog()
    {
        HasTitle = true;
        Text = "仓库";
        Size = new Vector2I(410, 420);

        var bg = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 121,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddControl(bg);

        var close = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)ClientArea.Size.X - 30, 3),
        };
        close.MouseClick += (o, e) => Visible = false;
        AddControl(close);

        SortButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 364,
            Location = new Vector2I((int)ClientArea.Size.X - 47, 41),
        };
        SortButton.MouseClick += (o, e) => GameScene.Game?.SendItemSort(GridType.Storage);
        AddControl(SortButton);

        Grid = new DXItemGrid
        {
            GridSize = new Vector2I(10, 1),
            Location = new Vector2I(19, 61),
            GridType = GridType.Storage,
            ItemGrid = null, // GameScene 注入
            VisibleHeight = 10,
            Border = false,
            GridPadding = 1,
        };
        AddControl(Grid);

        ScrollBar = new DXVScrollBar
        {
            Location = new Vector2I(19 + (int)Grid.Size.X + 1, 61),
            Size = new Vector2I(14, 349),
            VisibleSize = 10,
            Change = 1,
        };
        ScrollBar.ValueChanged += (o, e) => Grid.ScrollValue = ScrollBar.Value;
        AddControl(ScrollBar);

        BindWheel();
    }

    /// <summary>滚轮滚动仓库 (cells 在 RefreshStorage 重建后需重绑)</summary>
    private void BindWheel()
    {
        foreach (var cell in Grid.Cells)
            cell.MouseWheel += ScrollBar.DoMouseWheel;
    }

    /// <summary>StorageSize 包/登录后调用: 行数 = ceil(StorageSize/10)</summary>
    public void RefreshStorage()
    {
        var game = GameScene.Game;
        int size = game?.StorageSize ?? 100;
        Grid.GridSize = new Vector2I(10, Math.Max(10, (int)Math.Ceiling(size / 10f)));
        ScrollBar.MaxValue = Grid.GridSize.Y;
        Grid.ScrollValue = ScrollBar.Value;
        Grid.RefreshGrid();
        BindWheel();
    }
}
