using System;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 角色窗口 (移植自 Client/Scenes/Views/CharacterDialog.cs 的装备部分):
/// Interface 110 底图, 17 个基础装备槽 (EquipmentSlot 0-16, 钓鱼槽 17-21 不建格)。
/// 空槽底图按原版索引绘制在格子下层 (20% 透明)。
/// </summary>
public partial class CharacterDialog : DXWindow
{
    public DXItemCell[] Grid;
    public DXLabel WeightLabel;

    // 槽位 -> 空槽底图索引 (Interface 库; 无底图的槽不画)
    private static readonly int[] SlotBackgrounds = new int[17]
    {
        -1,  // Weapon
        -1,  // Armour
        -1,  // Shield
        -1,  // Helmet
        104, // Emblem
        82,  // HorseArmour
        38,  // Torch
        33,  // Necklace
        32,  // BraceletL
        32,  // BraceletR
        31,  // RingL
        31,  // RingR
        81,  // Flower
        40,  // Poison
        39,  // Amulet
        36,  // Shoes
        34,  // Costume
    };

    private static readonly Vector2I[] SlotPositions = new Vector2I[17]
    {
        new(58, 122),   // Weapon
        new(120, 123),  // Armour
        new(170, 170),  // Shield
        new(140, 90),   // Helmet
        new(244, 118),  // Emblem
        new(283, 118),  // HorseArmour
        new(10, 196),   // Torch
        new(10, 157),   // Necklace
        new(244, 157),  // BraceletL
        new(283, 157),  // BraceletR
        new(244, 196),  // RingL
        new(283, 196),  // RingR
        new(244, 235),  // Flower
        new(244, 274),  // Poison
        new(283, 235),  // Amulet
        new(10, 235),   // Shoes
        new(10, 118),   // Costume
    };

    public CharacterDialog()
    {
        HasTitle = true;
        Text = "角色";
        Size = new Vector2I(322, 400);

        var bg = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 110,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddControl(bg);

        // 人物纸娃娃 (在背景之上、装备槽之下, 原版 CharacterTab_BeforeChildrenDraw)
        var doll = new PaperDoll();
        doll.Position = new Vector2(130, 251); // 原版 (130,270) 相对 CharacterTab, 减标题栏 19
        AddChild(doll);

        var close = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)ClientArea.Size.X - 30, 3),
        };
        close.MouseClick += (o, e) => Visible = false;
        AddControl(close);

        Grid = new DXItemCell[17];

        for (int i = 0; i < 17; i++)
        {
            int idx = i;
            var cell = new DXItemCell
            {
                Location = SlotPositions[i] + new Vector2I(0, 19), // 底图标题栏下方
                ItemGrid = null, // GameScene 注入 Equipment
                Slot = i,
                GridType = GridType.Equipment,
                Hidden = false,
            };
            int bgIndex = SlotBackgrounds[i];
            if (bgIndex >= 0)
            {
                cell.BeforeDraw += (o, e) => DrawSlotBackground(cell, bgIndex);
            }
            AddControl(cell);
            Grid[i] = cell;
        }

        WeightLabel = new DXLabel
        {
            TextColour = Colors.White,
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Location = new Vector2I(10, 335),
            AutoSize = false,
            Size = new Vector2I(300, 18),
            FontSize = 8,
            IsControl = false,
        };
        AddControl(WeightLabel);
    }

    private void DrawSlotBackground(DXItemCell cell, int index)
    {
        if (cell.Item != null) return;

        var tex = MirSkin.GetTexture(LibraryFile.Interface, index);
        if (tex == null) return;

        var imgSize = tex.GetSize();
        float x = (cell.Size.X - imgSize.X) / 2f;
        float y = (cell.Size.Y - imgSize.Y) / 2f;
        cell.DrawTextureRect(tex, new Rect2(x, y, imgSize.X, imgSize.Y), false, new Color(1f, 1f, 1f, 0.2f));
    }

    public void SetWeight(int wearWeight, int handWeight)
    {
        WeightLabel.Text = $"负重 {wearWeight} / 手持 {handWeight}";
    }
}
