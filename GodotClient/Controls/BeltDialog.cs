using System;
using System.Linq;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 腰带窗口 (移植自 Client/Scenes/Views/BeltDialog.cs):
/// 无标题无边框, 10 格横向 (Globals.MaxBeltCount=10), 每格右上角数字 1-9,0。
/// 格内 QuickInfo/QuickItem 由 UpdateLinks 从 BeltLinks 恢复。
/// </summary>
public partial class BeltDialog : DXWindow
{
    public ClientBeltLink[] Links;
    public DXItemGrid Grid;

    public BeltDialog()
    {
        HasTitle = false;
        HasFooter = false;
        HasTopBorder = false;
        Size = new Vector2I(10 * (DXItemCell.CellWidth - 1) + 1, DXItemCell.CellHeight - 1 + 1);

        Links = new ClientBeltLink[Globals.MaxBeltCount];
        for (int i = 0; i < Globals.MaxBeltCount; i++)
            Links[i] = new ClientBeltLink { Slot = i };

        Grid = new DXItemGrid
        {
            GridSize = new Vector2I(10, 1),
            Location = Vector2I.Zero,
            GridType = GridType.Belt,
            GridPadding = 0,
            Border = false,
        };
        AddControl(Grid);

        for (int i = 0; i < Grid.Cells.Length; i++)
        {
            int slot = i;
            var label = new DXLabel
            {
                Text = ((slot + 1) % 10).ToString(),
                FontSize = 8,
                TextColour = new Color(1f, 0.9f, 0.5f),
                DrawOutline = true,
                OutlineColour = Colors.Black,
                Location = new Vector2I(-2, -1),
                IsControl = false,
            };
            Grid.Cells[slot].AddControl(label);
        }
    }

    /// <summary>从 BeltLinks 恢复格子链接 (登录时调用)</summary>
    public void UpdateLinks()
    {
        if (GameScene.Game == null) return;

        foreach (var link in Links)
        {
            if (link.Slot < 0 || link.Slot >= Grid.Cells.Length) continue;

            if (link.LinkInfoIndex > 0)
            {
                var info = Globals.ItemInfoList.Binding.FirstOrDefault(x => x.Index == link.LinkInfoIndex);
                if (info != null) Grid.Cells[link.Slot].QuickInfo = info;
            }
            else if (link.LinkItemIndex > 0)
            {
                var item = GameScene.Game.Inventory.FirstOrDefault(x => x?.Index == link.LinkItemIndex);
                if (item != null) Grid.Cells[link.Slot].QuickItem = item;
            }
        }
        Grid.RefreshGrid();
    }
}
