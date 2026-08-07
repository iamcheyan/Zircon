using System;
using System.Collections.Generic;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 键位表 (移植自 Client/Envir/CEnvir.cs 默认键位 + GetKeyBindLabel)。
/// M12 子集: 窗口开关 N/H/O/Q/W/E/V/B/L + 功能 Ctrl+H/Ctrl+A + 方向键。
/// 支持 Control/Alt/Shift 修饰与 Key1/Key2 双键。
/// </summary>
public enum KeyBindAction
{
    None,
    MenuWindow,        // N
    HelpWindow,        // H
    ConfigWindow,      // O
    CharacterWindow,   // Q
    InventoryWindow,   // W
    MagicWindow,       // E
    StorageWindow,     // S
    BeltWindow,        // Z
    ItemPickUp,        // Tab
    QuestTrackerWindow,// L
    MapMiniWindow,     // V
    MapBigWindow,      // B
    ChangeAttackMode,  // Ctrl+H
    ChangePetMode,     // Ctrl+A
}

public class KeyBindInfo
{
    public KeyBindAction Action;
    public Key Key1, Key2 = Key.None;
    public bool Control1, Alt1, Shift1;
    public bool Control2, Alt2, Shift2;

    public KeyBindInfo(KeyBindAction action, Key key1, bool control1 = false, bool alt1 = false, bool shift1 = false)
    {
        Action = action;
        Key1 = key1;
        Control1 = control1;
        Alt1 = alt1;
        Shift1 = shift1;
    }
}

public static class KeyBindManager
{
    public static readonly List<KeyBindInfo> KeyBinds = new()
    {
        new KeyBindInfo(KeyBindAction.MenuWindow, Key.N),
        new KeyBindInfo(KeyBindAction.HelpWindow, Key.H),
        new KeyBindInfo(KeyBindAction.ConfigWindow, Key.O),
        new KeyBindInfo(KeyBindAction.CharacterWindow, Key.Q),
        new KeyBindInfo(KeyBindAction.InventoryWindow, Key.W),
        new KeyBindInfo(KeyBindAction.MagicWindow, Key.E),
        new KeyBindInfo(KeyBindAction.StorageWindow, Key.S),
        new KeyBindInfo(KeyBindAction.BeltWindow, Key.Z),
        new KeyBindInfo(KeyBindAction.ItemPickUp, Key.Tab),
        new KeyBindInfo(KeyBindAction.QuestTrackerWindow, Key.L),
        new KeyBindInfo(KeyBindAction.MapMiniWindow, Key.V),
        new KeyBindInfo(KeyBindAction.MapBigWindow, Key.B),
        new KeyBindInfo(KeyBindAction.ChangeAttackMode, Key.H, control1: true),
        new KeyBindInfo(KeyBindAction.ChangePetMode, Key.A, control1: true),
    };

    /// <summary>按键事件 -> 命中的键位动作 (含修饰键匹配; 无双键项时不匹配 Key2)</summary>
    public static KeyBindAction GetAction(InputEventKey key)
    {
        if (key == null || key.Keycode == Key.None) return KeyBindAction.None;

        bool ctrl = key.CtrlPressed;
        bool alt = key.AltPressed;
        bool shift = key.ShiftPressed;

        foreach (var bind in KeyBinds)
        {
            if (bind.Key1 != key.Keycode) continue;
            if (bind.Control1 != ctrl || bind.Alt1 != alt || bind.Shift1 != shift) continue;
            if (bind.Key2 != Key.None) continue; // 双键键位 M12 不匹配 (首键按下即可)
            return bind.Action;
        }
        return KeyBindAction.None;
    }

    /// <summary>键位显示文本 (原版 GetKeyBindLabel 格式: "Ctrl + H")</summary>
    public static string GetKeyBindLabel(KeyBindAction action)
    {
        var bind = KeyBinds.Find(x => x.Action == action);
        if (bind == null) return "None";

        string text = "";
        if (bind.Control1) text += "Ctrl + ";
        if (bind.Alt1) text += "Alt + ";
        if (bind.Shift1) text += "Shift + ";
        text += GetKeyText(bind.Key1);

        if (bind.Key2 != Key.None)
        {
            text += ", ";
            if (bind.Control2) text += "Ctrl + ";
            if (bind.Alt2) text += "Alt + ";
            if (bind.Shift2) text += "Shift + ";
            text += GetKeyText(bind.Key2);
        }

        return text;
    }

    public static string GetKeyText(Key key)
    {
        // 可读键名: 字母/数字直接显示, 特殊键映射
        if (key >= Key.A && key <= Key.Z) return key.ToString();
        if (key >= Key.Key0 && key <= Key.Key9) return ((char)('0' + (key - Key.Key0))).ToString();
        return key switch
        {
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Tab => "Tab",
            Key.Escape => "Esc",
            Key.F1 => "F1",
            Key.F2 => "F2",
            _ => key.ToString(),
        };
    }
}
