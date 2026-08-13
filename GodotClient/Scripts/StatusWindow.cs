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
        Text = Lang.StatusWindowCharacterLabel;
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

        string[] keys = { Lang.CharacterCharacterTabLabel, Lang.MainPanelClassHint, Lang.StatusWindowUi494Label, Lang.MagicDialogTitle, Lang.SelectLocationLabel, Lang.StatusWindowUi495Label, Lang.StatusWindowUi496Label, Lang.StatusWindowUi497Label };
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
        SetRow(0, Lang.CharacterCharacterTabLabel, name);
        SetRow(1, Lang.MainPanelClassHint, className);
        SetRow(2, Lang.StatusWindowUi494Label, maxHp > 0 ? $"{hp} / {maxHp}" : "-");
        SetRow(3, Lang.MagicDialogTitle, maxMana > 0 ? $"0 / {maxMana}" : "-");
        SetRow(4, Lang.SelectLocationLabel, $"({cellX}, {cellY})");
        SetRow(5, Lang.StatusWindowUi495Label, DirectionName(dir));
        SetRow(6, Lang.StatusWindowUi496Label, mapName);
        SetRow(7, Lang.StatusWindowUi497Label, objCount.ToString());
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
            MirDirection.Up => Lang.StatusWindowUi502Label,
            MirDirection.UpRight => Lang.StatusWindowUi503Label,
            MirDirection.Right => Lang.StatusWindowUi504Label,
            MirDirection.DownRight => Lang.StatusWindowUi505Label,
            MirDirection.Down => Lang.StatusWindowUi506Label,
            MirDirection.DownLeft => Lang.StatusWindowUi507Label,
            MirDirection.Left => Lang.StatusWindowUi508Label,
            MirDirection.UpLeft => Lang.StatusWindowUi509Label,
            _ => dir.ToString(),
        };
    }
}
