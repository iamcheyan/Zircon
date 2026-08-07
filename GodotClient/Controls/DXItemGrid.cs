using System;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 物品网格 (移植自 Client/Controls/DXItemGrid.cs):
/// 按 GridSize 创建 DXItemCell 数组, 支持滚动 (ScrollValue/VisibleHeight,
/// Storage/PartsStorage 用)。格子直接绑定同一个底层 ItemGrid 数组。
/// </summary>
public partial class DXItemGrid : DXControl
{
    private Vector2I _gridSize;
    public Vector2I GridSize
    {
        get => _gridSize;
        set
        {
            if (_gridSize == value) return;
            _gridSize = value;
            UpdateSize();
        }
    }

    private float _gridPadding = 1f;
    public float GridPadding
    {
        get => _gridPadding;
        set
        {
            if (_gridPadding == value) return;
            _gridPadding = value;
            UpdateSize();
        }
    }

    private int _visibleHeight = int.MaxValue;
    public int VisibleHeight
    {
        get => _visibleHeight;
        set
        {
            if (_visibleHeight == value) return;
            _visibleHeight = value;
            UpdateSize();
        }
    }

    private int _scrollValue;
    public int ScrollValue
    {
        get => _scrollValue;
        set
        {
            int v = Math.Max(0, Math.Min(value, Math.Max(0, GridSize.Y - VisibleHeight)));
            if (_scrollValue == v) return;
            _scrollValue = v;
            UpdateGridDisplay();
        }
    }

    public GridType GridType;
    public ClientUserItem[] ItemGrid;
    public bool Linked;
    public bool AllowLink;
    public bool ReadOnly;

    public DXItemCell[] Cells;

    public DXItemCell this[int slot] => Cells[slot];

    private float Step => DXItemCell.CellWidth - 1 + (GridPadding * 2);

    private void UpdateSize()
    {
        Size = new Vector2(
            GridSize.X * Step + 1,
            Math.Min(GridSize.Y, VisibleHeight) * Step + 1);
        CreateGrid();
        QueueRedraw();
    }

    public void CreateGrid()
    {
        if (Cells != null)
        {
            foreach (var cell in Cells)
            {
                if (cell != null) cell.Dispose();
            }
            Cells = null;
        }

        int count = GridSize.X * GridSize.Y;
        Cells = new DXItemCell[count];

        for (int y = 0; y < GridSize.Y; y++)
        {
            for (int x = 0; x < GridSize.X; x++)
            {
                int slot = y * GridSize.X + x;
                var cell = new DXItemCell
                {
                    Location = new Vector2I(
                        (int)(x * Step + GridPadding),
                        (int)(y * Step + GridPadding)),
                    Slot = slot,
                    HostGrid = this,
                    ItemGrid = ItemGrid,
                    GridType = GridType,
                    ReadOnly = ReadOnly,
                };
                AddControl(cell);
                Cells[slot] = cell;
            }
        }

        UpdateGridDisplay();
    }

    /// <summary>滚动后重新摆放格子 (隐藏行外格子)</summary>
    public void UpdateGridDisplay()
    {
        if (Cells == null) return;

        for (int y = 0; y < GridSize.Y; y++)
        {
            for (int x = 0; x < GridSize.X; x++)
            {
                var cell = Cells[y * GridSize.X + x];
                if (cell == null) continue;

                if (y < ScrollValue || y >= ScrollValue + VisibleHeight)
                {
                    cell.Visible = false;
                    continue;
                }

                cell.Visible = true;
                cell.Location = new Vector2I(
                    (int)(x * Step + GridPadding),
                    (int)((y - ScrollValue) * Step + GridPadding));
            }
        }
    }

    /// <summary>重刷所有格子显示 (数组批量变更后调用)</summary>
    public void RefreshGrid()
    {
        if (Cells == null) return;
        foreach (var cell in Cells)
        {
            cell?.RefreshItem();
        }
    }
}
