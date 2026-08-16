using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

namespace ZirconClient.Scripts;

/// <summary>
/// E5/B1·B3: 硬编码表 (或 DataLayer 装载后的表) 全量快照导出 — 等价性证明的取证工具。
/// 反射读取, 不改任何表类源码; 稳定序列化: 键排序 / 枚举转字符串 / TimeSpan→ms /
/// Color→[r,g,b,a] (6位小数)。用法: MapTestScene --table-snapshot=&lt;path&gt;。
/// before/after 两份快照逐键逐字段 diff 必须全等 (Python: json ==)。
/// </summary>
public static class TableSnapshotTool
{
    public static void DumpAll(string path)
    {
        var root = new SortedDictionary<string, object>
        {
            ["magicEffectTable"] = DumpMagicEffectTable(),
            ["soundCatalog"] = DumpSoundCatalog(),
            ["magicSoundCatalog"] = DumpMagicSoundCatalog(),
            ["monsterSoundCatalog"] = DumpMonsterSoundCatalog(),
            ["frameSet"] = DumpFrameSet(),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(root, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
        GD.Print($"[TableSnapshot] PASS -> {Path.GetFullPath(path)}");
    }

    // ---------- 各表 ----------
    private static object DumpMagicEffectTable()
    {
        var t = typeof(MagicEffectTable);
        return new SortedDictionary<string, object>
        {
            ["_table"] = DumpDict(StaticField(t, "_table")),
            ["_attackTable"] = DumpDict(StaticField(t, "_attackTable")),
            ["OriginalSpellCases"] = DumpSet(StaticField(t, "OriginalSpellCases")),
            ["NoVisualSpellCases"] = DumpSet(StaticField(t, "NoVisualSpellCases")),
            ["colors"] = DumpColors(),
        };
    }

    private static object DumpColors()
    {
        // 静态颜色常量 (Godot Color) — Colour 字段的最终数值事实源
        var out_ = new SortedDictionary<string, object>();
        foreach (var f in typeof(MagicEffectTable).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.FieldType != typeof(Color)) continue;
            out_[f.Name] = ToStable(f.GetValue(null));
        }
        return out_;
    }

    private static object DumpSoundCatalog() => DumpDict(StaticField(typeof(SoundCatalog), "Entries"));

    private static object DumpMagicSoundCatalog()
    {
        var explicit_ = StaticField(typeof(MagicSoundCatalog), "Explicit");
        // Dictionary<(MagicType, phase), SoundSpec[]> → {"magic|phase": [specs...]}
        var out_ = new SortedDictionary<string, object>();
        foreach (DictionaryEntry e in (IDictionary)explicit_)
        {
            var key = e.Key.ToString() ?? "";  // "(FireBall, Start)"
            var inner = key.Trim('(', ')').Split(',');
            var norm = $"{inner[0].Trim()}|{inner[1].Trim()}";
            out_[norm] = ToStable(e.Value);
        }
        return out_;
    }

    private static object DumpMonsterSoundCatalog()
        => DumpDict(StaticField(typeof(MonsterSoundCatalog), "Entries"));

    private static object DumpFrameSet()
    {
        var out_ = new SortedDictionary<string, object>();
        foreach (var f in typeof(Library.FrameSet).GetFields(BindingFlags.Public | BindingFlags.Static)
                     .OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            var v = f.GetValue(null);
            if (v == null)
            {
                out_[f.Name] = null;  // 死声明 (ShinsuBig/LobsterSpawn) — 快照也记录
                continue;
            }
            var per = new SortedDictionary<string, object>();
            foreach (DictionaryEntry e in (IDictionary)v)
                per[e.Key.ToString() ?? ""] = ToStable(e.Value);
            out_[f.Name] = per;
        }
        return out_;
    }

    // ---------- 反射基建 ----------
    private static object StaticField(Type t, string name)
    {
        var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (f == null) throw new InvalidOperationException($"{t.Name}.{name} not found");
        return f.GetValue(null) ?? throw new InvalidOperationException($"{t.Name}.{name} null");
    }

    private static object DumpDict(object dict)
    {
        var per = new SortedDictionary<string, object>();
        foreach (DictionaryEntry e in (IDictionary)dict)
            per[e.Key.ToString() ?? ""] = ToStable(e.Value);
        return per;
    }

    private static object DumpSet(object set)
    {
        var items = new List<string>();
        foreach (var item in (IEnumerable)set) items.Add(item?.ToString() ?? "");
        items.Sort(StringComparer.Ordinal);
        return items;
    }

    private static object ToStable(object? v)
    {
        if (v == null) return null!;
        var t = v.GetType();
        if (t.IsEnum) return v.ToString()!;
        if (v is bool or int or long or string) return v;
        if (v is float ff) return Math.Round(ff, 6);
        if (v is double dd2) return Math.Round(dd2, 6);
        if (v is TimeSpan ts) return ts.TotalMilliseconds;
        if (v is Color c) return new List<object> { R6(c.R), R6(c.G), R6(c.B), R6(c.A) };
        if (v is System.Drawing.Color sd) return new List<object> { sd.R, sd.G, sd.B, sd.A };
        if (v is Array arr)
        {
            var items = new List<object>();
            foreach (var item in arr) items.Add(ToStable(item));
            return items;
        }
        if (v is IEnumerable and not IDictionary)
        {
            var items2 = new List<object>();
            foreach (var item in (IEnumerable)v) items2.Add(ToStable(item));
            return items2;
        }
        if (v is IDictionary idict)
        {
            var per = new SortedDictionary<string, object>();
            foreach (DictionaryEntry e in idict) per[e.Key.ToString() ?? ""] = ToStable(e.Value);
            return per;
        }
        // record struct / class: 按字段名展开
        var obj = new SortedDictionary<string, object>();
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (f.IsStatic) continue;
            obj[f.Name] = ToStable(f.GetValue(v));
        }
        if (obj.Count == 0) return v.ToString()!;
        return obj;
    }

    private static double R6(float x) => Math.Round(x, 6);
}
