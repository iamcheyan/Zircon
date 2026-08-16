using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// E5/B2: 客户端数据层加载器 — zircon/ClientData 三 JSON 运行时装载。
///
/// 事实源 (E5 架构决策): 网页端与 Godot 端共读同一套 ClientData 文件, 文件即唯一事实源;
/// 网页编辑回写后 Godot 重启即生效。
///
/// 加载内容:
///   frame-formulas.json → Library.FrameSet 全部静态字典 (94 表; 2 个原版死声明保持 null)
///   magic-effects.json  → MagicEffectTable._table/_attackTable/两白名单 (godot 结构段)
///   sounds.json         → SoundCatalog.Entries / MagicSoundCatalog.Explicit /
///                         MonsterSoundCatalog.Entries
///
/// 调用点: NetworkManager._Ready (autoload 首位, 先于任何场景脚本消费表)。
/// 容错: 文件缺失/坏条目 GD.PrintErr 并跳过该条目 (不崩客户端);
/// 但 TableSnapshotTool 等价性对账必须零差异 (CI 门禁兜底)。
/// cutover (E5/B4) 后: 硬编码字典本体已删, 本 loader 是数据的唯一来源。
/// </summary>
public static class DataLayer
{
    public static bool Loaded { get; private set; }

    public static void LoadAll(string clientDataDir = null)
    {
        if (Loaded) return;
        string dir = clientDataDir ?? ResolveClientDataDir();
        LoadFrameFormulas(Path.Combine(dir, "frame-formulas.json"));
        LoadMagicEffects(Path.Combine(dir, "magic-effects.json"));
        LoadSounds(Path.Combine(dir, "sounds.json"));
        Loaded = true;
        GD.Print($"[DataLayer] OK dir={dir}");
    }

    // ---------- 目录解析 ----------
    private static string ResolveClientDataDir()
    {
        // 1. 显式环境变量 (工具/测试覆盖)
        string env = System.Environment.GetEnvironmentVariable("ZIRCON_CLIENT_DATA");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;

        // 2. res://ClientData (导出包内; export_presets 已 include)
        if (Godot.FileAccess.FileExists("res://ClientData/magic-effects.json"))
            return ProjectSettings.GlobalizePath("res://ClientData");

        // 3. dev checkout: 工程目录上级 (zircon/ClientData)
        string proj = ProjectSettings.GlobalizePath("res://");
        string dev = Path.GetFullPath(Path.Combine(proj, "..", "ClientData"));
        if (Directory.Exists(dev)) return dev;

        // 4. 进程工作目录
        string cwd = Path.Combine(Directory.GetCurrentDirectory(), "ClientData");
        if (Directory.Exists(cwd)) return cwd;

        GD.PrintErr("[DataLayer] FATAL ClientData 目录未找到 — "
                    + "设 ZIRCON_CLIENT_DATA 或检查 zircon/ClientData 导出");
        return Path.Combine(proj, "..", "ClientData");
    }

    private static JsonDocument ReadJson(string path)
    {
        string text = File.ReadAllText(path);
        return JsonDocument.Parse(text);
    }

    // ---------- frame-formulas.json → FrameSet ----------
    private static void LoadFrameFormulas(string path)
    {
        if (!File.Exists(path))
        {
            GD.PrintErr($"[DataLayer] frame-formulas.json 缺失: {path}");
            return;
        }
        using var doc = ReadJson(path);
        var root = doc.RootElement;
        var frameSets = root.GetProperty("frameSets");
        var fsType = typeof(FrameSet);
        int dicts = 0, entries = 0, skipped = 0;

        foreach (var field in fsType.GetFields().Where(f => f.IsStatic))
        {
            string jsonKey = CamelToKey(field.Name);
            if (!frameSets.TryGetProperty(jsonKey, out var tableEl))
                continue;  // ShinsuBig/LobsterSpawn 死声明: 保持 null

            var dict = (IDictionary)field.GetValue(null)
                       ?? throw new InvalidOperationException($"FrameSet.{field.Name} null (静态构造未跑?)");
            dict.Clear();
            foreach (var animProp in tableEl.EnumerateObject())
            {
                if (!TryParseAnim(animProp.Name, out MirAnimation anim))
                {
                    GD.PrintErr($"[DataLayer] 帧表 {jsonKey}.{animProp.Name} 枚举解析失败, 跳过");
                    skipped++;
                    continue;
                }
                var e = animProp.Value;
                var frame = new Frame(
                    e.GetProperty("start").GetInt32(),
                    e.GetProperty("count").GetInt32(),
                    e.GetProperty("offset").GetInt32(),
                    TimeSpan.FromMilliseconds(e.GetProperty("ms").GetInt32()))
                {
                    Reversed = e.GetProperty("reversed").GetBoolean(),
                    StaticSpeed = e.GetProperty("staticSpeed").GetBoolean(),
                };
                if (e.TryGetProperty("delays", out var delays) && delays.ValueKind == JsonValueKind.Object)
                    foreach (var d in delays.EnumerateObject())
                        frame.Delays[int.Parse(d.Name, CultureInfo.InvariantCulture)] =
                            TimeSpan.FromMilliseconds(d.Value.GetInt32());
                dict[anim] = frame;
                entries++;
            }
            dicts++;
        }
        GD.Print($"[DataLayer] frame-formulas: {dicts} 表 {entries} 项"
                 + (skipped > 0 ? $" (跳过 {skipped})" : ""));
    }

    private static string CamelToKey(string fieldName)
    {
        // Players→players; DefaultItem→defaultItem; Companion_Pig→companionPig
        // (frameformulas.py 的 camel() 逆映射)
        var parts = fieldName.Split('_');
        return parts[0][0].ToString().ToLower() + parts[0][1..]
               + string.Join("", parts[1..]);
    }

    private static bool TryParseAnim(string jsonName, out MirAnimation anim)
    {
        // JSON 键 = first_cap(枚举名): "combat1"→Combat1, "standing"→Standing
        return Enum.TryParse(char.ToUpperInvariant(jsonName[0]) + jsonName[1..],
            out anim);
    }

    // ---------- magic-effects.json → MagicEffectTable ----------
    private static void LoadMagicEffects(string path)
    {
        if (!File.Exists(path))
        {
            GD.PrintErr($"[DataLayer] magic-effects.json 缺失: {path}");
            return;
        }
        using var doc = ReadJson(path);
        var root = doc.RootElement;

        // 白名单
        FillWhitelist(root.GetProperty("originalSpellCases"), "OriginalSpellCases");
        FillWhitelist(root.GetProperty("noVisualSpellCases"), "NoVisualSpellCases");

        // 主表
        var tableField = typeof(MagicEffectTable).GetField("_table",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var table = (IDictionary)tableField.GetValue(null)!;
        table.Clear();
        int loaded = 0, skipped = 0;
        foreach (var skill in root.GetProperty("skills").EnumerateObject())
        {
            if (!skill.Value.TryGetProperty("godot", out var godotEl))
                continue;  // 原版-only 条目不在 Godot 结构表 (gen_cs_table 口径)
            if (godotEl.ValueKind == JsonValueKind.Null) continue;
            if (!Enum.TryParse<MagicType>(skill.Name, out var type))
            {
                GD.PrintErr($"[DataLayer] MagicType.{skill.Name} 解析失败, 跳过");
                skipped++;
                continue;
            }
            try
            {
                table[type] = ParseCastEffect(godotEl);
                loaded++;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DataLayer] 技能 {skill.Name} 装载失败: {ex.Message}");
                skipped++;
            }
        }

        // 攻击表
        var attackField = typeof(MagicEffectTable).GetField("_attackTable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var attack = (IDictionary)attackField.GetValue(null)!;
        attack.Clear();
        foreach (var entry in root.GetProperty("attackTable").EnumerateObject())
        {
            if (!Enum.TryParse<MagicType>(entry.Name, out var type))
            {
                GD.PrintErr($"[DataLayer] attackTable.{entry.Name} 解析失败, 跳过");
                skipped++;
                continue;
            }
            try
            {
                attack[type] = ParseImpact(entry.Value);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DataLayer] attackTable.{entry.Name} 装载失败: {ex.Message}");
                skipped++;
            }
        }
        GD.Print($"[DataLayer] magic-effects: cast={loaded} attack={attack.Count}"
                 + (skipped > 0 ? $" (跳过 {skipped})" : ""));
    }

    private static void FillWhitelist(JsonElement arr, string fieldName)
    {
        var field = typeof(MagicEffectTable).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var set = (ICollection<MagicType>)field.GetValue(null)!;
        set.Clear();
        foreach (var item in arr.EnumerateArray())
            set.Add(Enum.Parse<MagicType>(item.GetString()!));
    }

    // ---- CastEffect / ImpactDef / ProjectileDef 解析 (默认值与 C# 初始化器逐一对齐) ----
    private static MagicEffectTable.CastEffect ParseCastEffect(JsonElement e)
    {
        var def = new MagicEffectTable.CastEffect();
        FillCommon(def, e);
        if (e.TryGetProperty("blend", out var blend)) def.Blend = blend.GetBoolean();
        if (e.TryGetProperty("castAtSource", out var cas)) def.CastAtSource = cas.GetBoolean();
        if (e.TryGetProperty("directionFromSource", out var dfs)) def.DirectionFromSource = dfs.GetBoolean();
        if (e.TryGetProperty("directionFromCast", out var dfc)) def.DirectionFromCast = dfc.GetBoolean();
        if (e.TryGetProperty("noTargetVisual", out var ntv)) def.NoTargetVisual = ntv.GetBoolean();
        if (e.TryGetProperty("noLocationVisual", out var nlv)) def.NoLocationVisual = nlv.GetBoolean();
        if (e.TryGetProperty("releaseAtCaster", out var rac)) def.ReleaseAtCaster = rac.GetBoolean();
        if (e.TryGetProperty("projectileLastLocationOnly", out var pll)) def.ProjectileLastLocationOnly = pll.GetBoolean();
        if (e.TryGetProperty("projectileDelayStepMs", out var pds)) def.ProjectileDelayStepMs = pds.GetDouble();
        if (e.TryGetProperty("source", out var src) && src.ValueKind != JsonValueKind.Null)
            def.Source = ParseImpact(src);
        def.SourceAdditional = ParseImpactList(e, "sourceAdditional");
        def.SourcePerLocation = ParseImpactList(e, "sourcePerLocation");
        if (e.TryGetProperty("projectile", out var pr) && pr.ValueKind != JsonValueKind.Null)
            def.Projectile = ParseProjectile(pr);
        if (e.TryGetProperty("targetProjectile", out var tp) && tp.ValueKind != JsonValueKind.Null)
            def.TargetProjectile = ParseProjectile(tp);
        if (e.TryGetProperty("impact", out var im) && im.ValueKind != JsonValueKind.Null)
            def.Impact = ParseImpact(im);
        if (e.TryGetProperty("targetEffect", out var te) && te.ValueKind != JsonValueKind.Null)
            def.TargetEffect = ParseImpact(te);
        if (e.TryGetProperty("mapImpact", out var mi) && mi.ValueKind != JsonValueKind.Null)
            def.MapImpact = ParseImpact(mi);
        def.Additional = ParseImpactList(e, "additional");
        def.AdditionalMapEffects = ParseOffsetImpactList(e, "additionalMapEffects");
        def.AdditionalProjectiles = ParseProjectileList(e, "additionalProjectiles");
        def.TargetAdditionalProjectiles = ParseProjectileList(e, "targetAdditionalProjectiles");
        return def;
    }

    private static MagicEffectTable.ProjectileDef ParseProjectile(JsonElement e)
    {
        var def = new MagicEffectTable.ProjectileDef();
        FillCommon(def, e);
        if (e.TryGetProperty("has16Directions", out var h16)) def.Has16Directions = h16.GetBoolean();
        if (e.TryGetProperty("explode", out var ex)) def.Explode = ex.GetBoolean();
        if (e.TryGetProperty("originOffsetX", out var ox)) def.OriginOffsetX = ox.GetInt32();
        if (e.TryGetProperty("originOffsetY", out var oy)) def.OriginOffsetY = oy.GetInt32();
        if (e.TryGetProperty("originFromTarget", out var oft)) def.OriginFromTarget = oft.GetBoolean();
        if (e.TryGetProperty("arrival", out var ar) && ar.ValueKind != JsonValueKind.Null)
            def.Arrival = ParseImpact(ar);
        if (e.TryGetProperty("arrivalSound", out var asnd)) def.ArrivalSound = ParseSoundIndex(asnd);
        if (e.TryGetProperty("completionSound", out var csnd)) def.CompletionSound = ParseSoundIndex(csnd);
        return def;
    }

    private static MagicEffectTable.ImpactDef ParseImpact(JsonElement e)
    {
        // OffsetImpactDef (AdditionalMapEffects 专属)
        MagicEffectTable.ImpactDef def = e.TryGetProperty("offsetX", out _)
            || e.TryGetProperty("offsetY", out _)
            ? new MagicEffectTable.OffsetImpactDef()
            : new MagicEffectTable.ImpactDef();
        FillCommon(def, e);
        if (def is MagicEffectTable.OffsetImpactDef off)
        {
            if (e.TryGetProperty("offsetX", out var ox)) off.OffsetX = ox.GetInt32();
            if (e.TryGetProperty("offsetY", out var oy)) off.OffsetY = oy.GetInt32();
        }
        if (e.TryGetProperty("soundFrame", out var sf)) def.SoundFrame = sf.GetInt32();
        if (e.TryGetProperty("soundFrameSound", out var sfs)) def.SoundFrameSound = ParseSoundIndex(sfs);
        if (e.TryGetProperty("directionStartIndices", out var dsi) && dsi.ValueKind == JsonValueKind.Array)
            def.DirectionStartIndices = dsi.EnumerateArray().Select(x => x.GetInt32()).ToArray();
        return def;
    }

    /// <summary>三类 def 共有字段 (默认值即 C# 字段初始化器, 只覆盖 JSON 显式项)。</summary>
    private static void FillCommon(object def, JsonElement e)
    {
        var t = def.GetType();
        void Set(string field, object value)
            => t.GetField(field)!.SetValue(def, value);
        if (e.TryGetProperty("file", out var f))
            Set("File", Enum.Parse<LibraryFile>(f.GetString()!));
        if (e.TryGetProperty("startIndex", out var si)) Set("StartIndex", si.GetInt32());
        if (e.TryGetProperty("frameCount", out var fc)) Set("FrameCount", fc.GetInt32());
        if (e.TryGetProperty("delayMs", out var dm)) Set("DelayMs", dm.GetInt32());
        if (e.TryGetProperty("colour", out var co)) Set("Colour", ParseColour(co));
        if (e.TryGetProperty("blendRate", out var br)) Set("BlendRate", br.GetSingle());
        if (e.TryGetProperty("opacity", out var op)) Set("Opacity", op.GetSingle());
        if (e.TryGetProperty("skip", out var sk)) Set("Skip", sk.GetInt32());
        if (e.TryGetProperty("frameLight", out var fl)) Set("FrameLight", fl.GetInt32());
        if (e.TryGetProperty("drawType", out var dt))
            Set("DrawType", Enum.Parse<MirEffectNode.EffectLayer>(dt.GetString()!));
        if (e.TryGetProperty("startDelayMs", out var sd)) Set("StartDelayMs", sd.GetDouble());
        if (e.TryGetProperty("distanceDelayMs", out var dd)) Set("DistanceDelayMs", dd.GetInt32());
        if (e.TryGetProperty("directionFromSource", out var dfs)) Set("DirectionFromSource", dfs.GetBoolean());
        if (e.TryGetProperty("directionFromCast", out var dfc)) Set("DirectionFromCast", dfc.GetBoolean());
        if (e.TryGetProperty("noColourKey", out var nck)) Set("NoColourKey", nck.GetBoolean());
    }

    private static Color ParseColour(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.String)
        {
            // MagicEffectTable 静态色名 (Fire/Ice/.../White)
            var field = typeof(MagicEffectTable).GetField(e.GetString()!,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field == null)
                throw newFormatException($"颜色名 {e.GetString()} 不是 MagicEffectTable 静态色");
            return (Color)field.GetValue(null)!;
        }
        if (e.ValueKind == JsonValueKind.Array)
        {
            var c = e.EnumerateArray().Select(x => x.GetSingle()).ToArray();
            return c.Length == 4 ? new Color(c[0], c[1], c[2], c[3]) : new Color(c[0], c[1], c[2]);
        }
        throw new FormatException($"colour 形态不支持: {e.ValueKind}");
    }

    private static SoundIndex ParseSoundIndex(JsonElement e)
        => Enum.Parse<SoundIndex>(e.GetString()!);

    private static List<MagicEffectTable.ImpactDef> ParseImpactList(JsonElement e, string prop)
    {
        var list = new List<MagicEffectTable.ImpactDef>();
        if (e.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                list.Add(ParseImpact(item));
        return list;
    }

    private static List<MagicEffectTable.OffsetImpactDef> ParseOffsetImpactList(JsonElement e, string prop)
    {
        var list = new List<MagicEffectTable.OffsetImpactDef>();
        if (e.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                list.Add((MagicEffectTable.OffsetImpactDef)ParseImpact(item));
        return list;
    }

    private static List<MagicEffectTable.ProjectileDef> ParseProjectileList(JsonElement e, string prop)
    {
        var list = new List<MagicEffectTable.ProjectileDef>();
        if (e.TryGetProperty(prop, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var item in arr.EnumerateArray())
                list.Add(ParseProjectile(item));
        return list;
    }

    // ---------- sounds.json → 三张 catalog ----------
    private static void LoadSounds(string path)
    {
        if (!File.Exists(path))
        {
            GD.PrintErr($"[DataLayer] sounds.json 缺失: {path}");
            return;
        }
        using var doc = ReadJson(path);
        var root = doc.RootElement;

        // SoundCatalog.Entries (readonly 字段持 Dictionary 实例 — 原地清空重填)
        var entriesField = typeof(SoundCatalog).GetField("Entries")!;
        var entries = (IDictionary)entriesField.GetValue(null)!;
        entries.Clear();
        foreach (var item in root.GetProperty("sounds").EnumerateObject())
        {
            var v = item.Value;
            entries[Enum.Parse<SoundIndex>(item.Name)] = new SoundEntry(
                v.GetProperty("file").GetString()!,
                Enum.Parse<SoundCategory>(v.GetProperty("category").GetString()!),
                v.GetProperty("loop").GetBoolean());
        }

        // MagicSoundCatalog.Explicit: "magic|phase" 键
        var explicitField = typeof(MagicSoundCatalog).GetField("Explicit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var explicitDict = (IDictionary)explicitField.GetValue(null)!;
        explicitDict.Clear();
        foreach (var magic in root.GetProperty("magic").EnumerateObject())
        {
            var type = Enum.Parse<MagicType>(magic.Name);
            foreach (var phase in magic.Value.EnumerateObject())
            {
                var ph = Enum.Parse<MagicSoundPhase>(phase.Name);
                var specs = phase.Value.EnumerateArray()
                    .Select(s => new SoundSpec(
                        Enum.Parse<SoundIndex>(s.GetProperty("sound").GetString()!),
                        Enum.Parse<MagicSoundGate>(s.GetProperty("gate").GetString()!)))
                    .ToArray();
                explicitDict[(type, ph)] = specs;
            }
        }

        // MonsterSoundCatalog.Entries
        var monsterField = typeof(MonsterSoundCatalog).GetField("Entries")!;
        var monster = (IDictionary)monsterField.GetValue(null)!;
        monster.Clear();
        foreach (var item in root.GetProperty("monster").EnumerateObject())
        {
            var v = item.Value;
            monster[Enum.Parse<MonsterImage>(item.Name)] = new MonsterSoundSet(
                Enum.Parse<SoundIndex>(v.GetProperty("attack").GetString()!),
                Enum.Parse<SoundIndex>(v.GetProperty("struck").GetString()!),
                Enum.Parse<SoundIndex>(v.GetProperty("die").GetString()!));
        }
        GD.Print($"[DataLayer] sounds: {entries.Count} / {explicitDict.Count} / {monster.Count}");
    }

    private static Exception newFormatException(string msg) => new FormatException(msg);
}
