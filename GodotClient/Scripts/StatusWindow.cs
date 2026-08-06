using System.Collections.Generic;
using Godot;
using Library;
using ZirconClient.Controls;

namespace ZirconClient.Scripts;

/// <summary>
/// 角色状态窗口 (M11 控件库接入演示): 绑定玩家真实数据, F2 开关。
/// 后续 M12 会做正式 MainPanel HUD, 本窗口保留为可开关的详细信息面板。
/// </summary>
public partial class StatusWindow : DXWindow
{
    private readonly List<DXLabel> _rows = new();

    public StatusWindow()
    {
        Name = "StatusWindow";
        Text = "角色状态";
        Position = new Vector2(40, 80);
        Size = new Vector2(280, 230);
    }

    public override void _Ready()
    {
        base._Ready();

        // 背景贴图 (旧窗口标准做法: 一张 Interface 图做窗口底)
        AddControl(new DXImageControl
        {
            Name = "Background",
            Index = 164,
            LibraryFile = LibraryFile.Interface,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        string[] keys = { "角色", "职业", "生命", "魔法", "位置", "方向", "地图", "周围物体" };
        float y = 36;
        foreach (string key in keys)
        {
            var row = new DXLabel
            {
                Name = "Row_" + key,
                Text = key + ": -",
                FontSize = 13,
                TextColour = new Color(1f, 0.95f, 0.75f),
                Location = new Vector2I(14, (int)y),
            };
            AddControl(row);
            _rows.Add(row);
            y += 23;
        }
    }

    /// <summary>刷新显示内容 (GameScene 节流调用; 文本没变化时不重绘)</summary>
    public void Refresh(string name, string className, int hp, int maxHp, int maxMana,
                        int cellX, int cellY, MirDirection dir, string mapName, int objCount)
    {
        if (_rows.Count < 8) return;
        SetRow(0, "角色", name);
        SetRow(1, "职业", className);
        SetRow(2, "生命", maxHp > 0 ? $"{hp} / {maxHp}" : "-");
        SetRow(3, "魔法", maxMana > 0 ? $"0 / {maxMana}" : "-");
        SetRow(4, "位置", $"({cellX}, {cellY})");
        SetRow(5, "方向", DirectionName(dir));
        SetRow(6, "地图", mapName);
        SetRow(7, "周围物体", objCount.ToString());
    }

    private void SetRow(int index, string key, string value)
    {
        string text = $"{key}: {value}";
        if (_rows[index].Text == text) return;
        _rows[index].Text = text;
    }

    private static string DirectionName(MirDirection dir)
    {
        return dir switch
        {
            MirDirection.Up => "上",
            MirDirection.UpRight => "右上",
            MirDirection.Right => "右",
            MirDirection.DownRight => "右下",
            MirDirection.Down => "下",
            MirDirection.DownLeft => "左下",
            MirDirection.Left => "左",
            MirDirection.UpLeft => "左上",
            _ => dir.ToString(),
        };
    }
}
