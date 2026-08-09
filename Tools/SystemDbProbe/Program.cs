// SystemDbProbe — 读取 System.db 并生成 Markdown 数据库文档。
//
// 用法:
//   dotnet run --project Tools/SystemDbProbe                       # 统计各表数量
//   dotnet run --project Tools/SystemDbProbe -- --dump docs/database   # 生成全量文档
//
// 生成的文档按集合（表）拆分，每个记录一段「字段 | 值」表；记录数超过
// MaxRecordsPerFile 时自动按 Index 分段为 <Type>.<N>.md。
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Library;
using Library.MirDB;
using Library.SystemModels;
using MirDB;

string root = "Debug/Server/Database/";
string dumpDir = null;
string viewDir = null;
string imagesOut = null;
string storesOut = null;
string bc7Lib = null, bc7Frame = null, bc7Out = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--dump")
    {
        dumpDir = args[++i];
        continue;
    }
    if (args[i] == "--view")
    {
        viewDir = args[++i];
        continue;
    }
    if (args[i] == "--images")
    {
        imagesOut = args[++i];
        continue;
    }
    if (args[i] == "--bc7")
    {
        bc7Lib = args[++i];
        bc7Frame = args[++i];
        bc7Out = args[++i];
        continue;
    }
    if (args[i] == "--stores")
    {
        storesOut = args[++i];
        continue;
    }
    root = args[i];
}
if (!Path.IsPathRooted(root)) root = Path.GetFullPath(root);

if (bc7Lib != null)
{
    Bc7Probe.Run(bc7Lib, int.Parse(bc7Frame), bc7Out);
    return;
}

var session = new Session(SessionMode.Users, root);
session.Initialize(
    typeof(ItemInfo).Assembly,        // LibraryCore（SystemModels）
    typeof(Server.DBModels.AccountInfo).Assembly // ServerLibrary（DBModels）
);

Console.WriteLine($"数据库: {session.SystemPath}");
Console.WriteLine($"版本:   {session.SystemDatabaseVersion}");

if (dumpDir == null && viewDir == null && imagesOut == null && storesOut == null)
{
    void Dump(string label, int count)
        => Console.WriteLine($"{label,-12} {count,6}");

    Dump("地图", session.GetCollection<MapInfo>().Count);
    Dump("怪物", session.GetCollection<MonsterInfo>().Count);
    Dump("物品", session.GetCollection<ItemInfo>().Count);
    Dump("魔法", session.GetCollection<MagicInfo>().Count);
    Dump("NPC", session.GetCollection<NPCInfo>().Count);
    Dump("刷新点", session.GetCollection<RespawnInfo>().Count);
    Dump("任务", session.GetCollection<QuestInfo>().Count);
    Dump("沙巴克", session.GetCollection<CastleInfo>().Count);
    return;
}

if (storesOut != null)
{
    GenerateStores(session, storesOut);
    Console.WriteLine($"商店数据 -> {storesOut}");
    return;
}

if (viewDir != null)
{
    Directory.CreateDirectory(viewDir);
    Directory.CreateDirectory(Path.Combine(viewDir, "items"));
    GenerateViews(session, viewDir);
    Console.WriteLine($"玩家视图 -> {viewDir}");
    return;
}

if (imagesOut != null)
{
    GenerateImages(session, imagesOut);
    Console.WriteLine($"图片映射 -> {imagesOut}");
    return;
}

// ---------- 全量 Markdown 文档生成 ----------

const int MaxRecordsPerFile = 300;

var zhNames = new Dictionary<string, string>
{
    ["BaseStat"] = "基础属性", ["BundleInfo"] = "礼包", ["BundleItemInfo"] = "礼包物品",
    ["CastleFlagInfo"] = "沙巴克旗帜", ["CastleGateInfo"] = "沙巴克城门", ["CastleGuardInfo"] = "沙巴克守卫",
    ["CastleInfo"] = "沙巴克城堡", ["CompanionInfo"] = "宠物", ["CompanionLevelInfo"] = "宠物等级",
    ["CompanionSkillInfo"] = "宠物技能", ["CompanionSpeech"] = "宠物台词", ["CurrencyInfo"] = "货币",
    ["CurrencyInfoImage"] = "货币图标", ["DisciplineInfo"] = "修炼", ["DropInfo"] = "掉落",
    ["DungeonInfo"] = "地下城", ["DungeonMapInfo"] = "地下城地图", ["WorldEventInfo"] = "世界事件",
    ["WorldEventTrigger"] = "世界事件触发器", ["WorldEventInfoTriggerStat"] = "世界事件触发属性",
    ["PlayerEventInfo"] = "玩家事件", ["PlayerEventTrigger"] = "玩家事件触发器",
    ["PlayerEventInfoTriggerStat"] = "玩家事件触发属性", ["MonsterEventInfo"] = "怪物事件",
    ["MonsterEventTrigger"] = "怪物事件触发器", ["MonsterEventInfoTriggerStat"] = "怪物事件触发属性",
    ["BaseEventAction"] = "事件动作（基类）", ["WorldEventAction"] = "世界事件动作",
    ["PlayerEventAction"] = "玩家事件动作", ["MonsterEventAction"] = "怪物事件动作", ["FameInfo"] = "声望", ["FameInfoStat"] = "声望属性",
    ["FameInfoReward"] = "声望奖励", ["FishingInfo"] = "钓鱼", ["FishingDropInfo"] = "钓鱼掉落",
    ["GuardInfo"] = "守卫", ["HelpInfo"] = "帮助", ["HelpPageInfo"] = "帮助页面", ["HelpItemInfo"] = "帮助条目",
    ["InstanceInfo"] = "副本", ["InstanceMapInfo"] = "副本地图", ["InstanceInfoStat"] = "副本属性",
    ["ItemInfo"] = "物品", ["ItemInfoStat"] = "物品属性加成", ["LootBoxInfo"] = "宝箱",
    ["LootBoxItemInfo"] = "宝箱物品", ["MagicInfo"] = "魔法", ["MapInfo"] = "地图",
    ["MapInfoStat"] = "地图属性加成", ["MapRegion"] = "地图区域", ["MilestoneInfo"] = "里程碑",
    ["MilestoneInfoTask"] = "里程碑任务", ["MineInfo"] = "矿点", ["MonsterInfo"] = "怪物",
    ["MonsterInfoStat"] = "怪物属性加成", ["MovementInfo"] = "传送点", ["NPCInfo"] = "NPC",
    ["NPCPage"] = "NPC 页面", ["NPCGood"] = "NPC 商品", ["NPCType"] = "NPC 类型", ["NPCCheck"] = "NPC 检查",
    ["NPCAction"] = "NPC 动作", ["NPCButton"] = "NPC 按钮", ["NPCRequirement"] = "NPC 需求",
    ["NPCValue"] = "NPC 值", ["QuestInfo"] = "任务", ["QuestReward"] = "任务奖励",
    ["QuestRequirement"] = "任务需求", ["QuestTask"] = "任务步骤", ["QuestTaskMonsterDetails"] = "任务怪物明细",
    ["RespawnInfo"] = "刷新点", ["SafeZoneInfo"] = "安全区", ["SetInfo"] = "套装", ["SetInfoStat"] = "套装属性",
    ["StoreInfo"] = "商店", ["SystemDatabaseInfo"] = "数据库信息", ["WeaponCraftStatInfo"] = "武器锻造属性",
};

// 概要表字段：给主要集合加「快速浏览」表（一行一条记录，便于概览）
var summaries = new Dictionary<string, string[]>
{
    ["MapInfo"] = new[] { "FileName", "Description", "MiniMap", "Light", "Weather", "MinimumLevel", "MaximumLevel" },
    ["MonsterInfo"] = new[] { "MonsterName", "Image", "AI", "Level", "Experience", "ViewRange", "IsBoss", "Undead" },
    ["ItemInfo"] = new[] { "ItemName", "ItemType", "RequiredClass", "Price", "Weight", "StackSize", "Rarity" },
    ["MagicInfo"] = new[] { "Name", "Magic", "Class", "School", "Property", "Icon", "NeedLevel1", "NeedLevel3", "BaseCost", "Delay" },
    ["NPCInfo"] = new[] { "NPCName", "Image", "FaceImage", "GoodsIndex", "MapIcon" },
    ["QuestInfo"] = new[] { "QuestName", "QuestType", "StartNPC", "FinishNPC" },
    ["RespawnInfo"] = new[] { "Monster", "Region", "Delay", "Count", "DropSet", "EventSpawn" },
    ["MovementInfo"] = new[] { "SourceRegion", "DestinationRegion", "Icon", "NeedItem", "RequiredClass" },
    ["SafeZoneInfo"] = new[] { "Region", "BindRegion", "StartClass", "RedZone", "Border" },
    ["CastleInfo"] = new[] { "Name", "Map", "StartTime", "Duration" },
    ["BaseStat"] = new[] { "Class", "Level", "Health", "Mana", "MinAC", "MaxAC", "MinDC", "MaxDC" },
    ["CompanionInfo"] = new[] { "MonsterInfo", "Price", "Available" },
    ["StoreInfo"] = new[] { "Item", "Price", "HuntGoldPrice", "Filter", "Available" },
    ["MineInfo"] = new[] { "Map", "Item", "Chance", "Quantity", "RestockTimeInMinutes" },
    ["GuardInfo"] = new[] { "Map", "Monster", "X", "Y", "Direction" },
    ["CurrencyInfo"] = new[] { "Name", "Abbreviation", "Type", "Category", "DropItem" },
    ["SetInfo"] = new[] { "SetName" },
    ["FameInfo"] = new[] { "Name", "Shape", "Cost", "Order" },
};

List<Type> types = session.Assemblies
    .SelectMany(a => a.GetTypes())
    .Where(t => t.IsSubclassOf(typeof(DBObject)) && !t.IsAbstract)
    .Where(t => t.GetCustomAttribute<UserObjectAttribute>() == null)
    .Distinct()
    .OrderBy(t => t.Name)
    .ToList();

if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);
Directory.CreateDirectory(Path.Combine(dumpDir, "data"));

var enumTypes = new SortedDictionary<string, SortedDictionary<string, (string Value, string Description)>>(StringComparer.Ordinal);
var summary = new List<(string Type, string Zh, int Count, int Parts)>();

foreach (Type type in types)
{
    ADBCollection collection = session.GetCollection(type);
    IList binding = (IList)collection.GetType().GetField("Binding").GetValue(collection);
    int count = binding.Count;

    zhNames.TryGetValue(type.Name, out string zh);
    string title = zh == null ? type.Name : $"{zh}（{type.Name}）";

    if (count == 0)
    {
        summary.Add((type.Name, zh ?? "", 0, 1));
        File.WriteAllText(Path.Combine(dumpDir, "data", $"{type.Name}.md"),
            $"<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改 -->\n\n# {title}\n\n> 集合为空（0 条记录）。\n");
        continue;
    }

    // 收集本类型用到的枚举（供 enums.md）
    foreach (PropertyInfo p in type.GetProperties())
        if (p.PropertyType.IsEnum)
            CollectEnum(p.PropertyType);

    int parts = (count + MaxRecordsPerFile - 1) / MaxRecordsPerFile;
    summary.Add((type.Name, zh ?? "", count, parts));

    for (int part = 0; part < parts; part++)
    {
        int start = part * MaxRecordsPerFile;
        int end = Math.Min(start + MaxRecordsPerFile, count);
        int firstIndex = ((DBObject)binding[start]).Index;
        int lastIndex = ((DBObject)binding[end - 1]).Index;
        var sb = new StringBuilder();
        sb.AppendLine("<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"> 记录 #{firstIndex} – #{lastIndex}，共 {count} 条" +
                      (parts > 1 ? $"（第 {part + 1}/{parts} 部分）" : "") + "。");
        sb.AppendLine();

        if (parts > 1)
        {
            var nav = new List<string> { "[README](../README.md)" };
            if (part > 0) nav.Add($"[← 上一部分]({type.Name}.{part}.md)");
            if (part < parts - 1) nav.Add($"[下一部分 →]({type.Name}.{part + 2}.md)");
            sb.AppendLine(string.Join(" · ", nav));
            sb.AppendLine();
        }

        // 概要表（每个部分列出该部分内的记录）
        if (summaries.TryGetValue(type.Name, out string[] sumFields))
        {
            var props = type.GetProperties().ToDictionary(p => p.Name);
            sb.AppendLine("## 快速浏览");
            sb.AppendLine();
            sb.AppendLine("| # | " + string.Join(" | ", sumFields) + " |");
            sb.AppendLine("|---|" + string.Join("|", sumFields.Select(_ => "---")) + "|");
            for (int i = start; i < end; i++)
            {
                DBObject ob = (DBObject)binding[i];
                var cells = new List<string> { ob.Index.ToString() };
                foreach (string f in sumFields)
                    cells.Add(Escape(Render(props.TryGetValue(f, out PropertyInfo pi) ? SafeGet(pi, ob) : null)));
                sb.AppendLine("| " + string.Join(" | ", cells) + " |");
            }
            sb.AppendLine();
        }

        // 魔法集合附加「按职业分组」视图（只在第一部分输出一次）
        if (type.Name == "MagicInfo" && part == 0)
        {
            var props = type.GetProperties().ToDictionary(p => p.Name);
            var classOrder = new[] { "Warrior", "Wizard", "Taoist", "Assassin" };
            var grouped = binding.Cast<DBObject>()
                .Select(ob => new
                {
                    Ob = ob,
                    Cls = props["Class"] is { } cpi ? SafeGet(cpi, ob)?.ToString() ?? "" : "",
                    Name = props["Name"] is { } npi ? SafeGet(npi, ob)?.ToString() ?? "" : "",
                })
                .GroupBy(x => x.Cls)
                .ToDictionary(g => g.Key, g => g.ToList());
            sb.AppendLine("## 按职业分组（技能速查）");
            sb.AppendLine();
            foreach (string cls in classOrder.Concat(grouped.Keys.Except(classOrder)))
            {
                if (!grouped.TryGetValue(cls, out var list) || list.Count == 0) continue;
                sb.AppendLine($"#### {cls}（{list.Count} 个）");
                sb.AppendLine();
                sb.AppendLine("| # | Name | Magic | 1级 | 2级 | 3级 | 基础耗蓝 | 施法延迟 |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");
                foreach (var x in list)
                {
                    var ob = x.Ob;
                    var cells = new List<string> { ob.Index.ToString(), x.Name };
                    foreach (string f in new[] { "Magic", "NeedLevel1", "NeedLevel2", "NeedLevel3", "BaseCost", "Delay" })
                        cells.Add(Escape(Render(props[f] is { } pi ? SafeGet(pi, ob) : null)));
                    sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                }
                sb.AppendLine();
            }
        }

        for (int i = start; i < end; i++)
        {
            DBObject ob = (DBObject)binding[i];
            string identity = IdentityOf(ob);
            sb.AppendLine($"### #{ob.Index}" + (identity.Length > 0 ? $" · {identity}" : ""));
            sb.AppendLine();
            sb.AppendLine("| 字段 | 值 |");
            sb.AppendLine("|---|---|");

            foreach (PropertyInfo p in type.GetProperties())
            {
                if (ShouldSkip(p)) continue;

                string name = p.Name;
                if (p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DBBindingList<>))
                {
                    IList list = (IList)SafeGet(p, ob);
                    string itemType = p.PropertyType.GetGenericArguments()[0].Name;
                    int n = list?.Count ?? 0;
                    if (n == 0) continue;
                    sb.AppendLine($"| {name} | `{itemType}` × {n} 条（明细见 [{itemType}.md]({itemType}.md)） |");
                    continue;
                }

                object value = SafeGet(p, ob);
                if (value == null) continue;
                sb.AppendLine($"| {name} | {Escape(Render(value))} |");
            }

            // 派生 Stats 字段（如 ItemInfo.Stats 等）
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType != typeof(Stats)) continue;
                object value = SafeGetField(f, ob);
                if (value is not Stats stats || stats.Values.Count == 0) continue;
                sb.AppendLine($"| {f.Name} | {Escape(RenderStats(stats))} |");
            }

            sb.AppendLine();
        }

        string file = parts == 1 ? $"{type.Name}.md" : $"{type.Name}.{part + 1}.md";
        File.WriteAllText(Path.Combine(dumpDir, "data", file), sb.ToString());
        Console.WriteLine($"{type.Name,-28} {count,6} 条 -> data/{file}");
    }
}

// ---------- enums.md ----------

var enumSb = new StringBuilder();
enumSb.AppendLine("<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改 -->");
enumSb.AppendLine();
enumSb.AppendLine("# 枚举字典（System.db 中使用的所有枚举）");
enumSb.AppendLine();
enumSb.AppendLine("> 字段值为枚举类型时，文档中直接显示枚举成员名。本页列出全部枚举成员及其数值与 Description。");
enumSb.AppendLine();

foreach (KeyValuePair<string, SortedDictionary<string, (string Value, string Description)>> kv in enumTypes)
{
    enumSb.AppendLine($"## {kv.Key}");
    enumSb.AppendLine();
    enumSb.AppendLine("| 成员 | 值 | 说明 |");
    enumSb.AppendLine("|---|---|---|");
    foreach (KeyValuePair<string, (string Value, string Description)> member in kv.Value)
        enumSb.AppendLine($"| {member.Key} | {member.Value.Value} | {Escape(member.Value.Description)} |");
    enumSb.AppendLine();
}
File.WriteAllText(Path.Combine(dumpDir, "data", "enums.md"), enumSb.ToString());
Console.WriteLine($"枚举 -> data/enums.md");

// ---------- stats.md（Stat 枚举 + StatDescription 说明） ----------

var statSb = new StringBuilder();
statSb.AppendLine("<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改 -->");
statSb.AppendLine();
statSb.AppendLine("# 属性字典（Stat 枚举）");
statSb.AppendLine();
statSb.AppendLine("> `ItemInfo.Stats` / `MonsterInfo.Stats` / `MapInfo.BuffStats` 等字段中出现的属性名，均来自本枚举。");
statSb.AppendLine("> 格式列的 `{0}` 即属性值（`{0}-{1}` 表示 Min-Max 区间，`{0:+#0%}` 表示百分比）。");
statSb.AppendLine();
statSb.AppendLine("| 成员 | 值 | 显示名 | 类型 | 备注 |");
statSb.AppendLine("|---|---|---|---|---|");

foreach (object v in Enum.GetValues(typeof(Stat)))
{
    var member = typeof(Stat).GetMember(v.ToString())[0];
    var desc = member.GetCustomAttribute<StatDescription>();
    long val = Convert.ToInt64(v);
    string title = desc?.Title ?? "";
    string mode = desc == null ? "" : desc.Mode.ToString();
    string hint = desc?.UsageHint ?? "";
    string fmt = desc?.Format ?? "";
    statSb.AppendLine($"| {v} | {val} | {Escape(title)} | {mode} | {Escape(hint)}{(string.IsNullOrEmpty(fmt) ? "" : "（格式 " + Escape(fmt) + "）")} |");
}
File.WriteAllText(Path.Combine(dumpDir, "data", "stats.md"), statSb.ToString());
Console.WriteLine($"属性字典 -> data/stats.md");

// ---------- _summary.md ----------

var sumSb = new StringBuilder();
sumSb.AppendLine("<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改 -->");
sumSb.AppendLine();
sumSb.AppendLine("# 集合一览（自动生成）");
sumSb.AppendLine();
sumSb.AppendLine("| 集合 | 中文 | 记录数 | 文档 |");
sumSb.AppendLine("|---|---|---:|---|");
foreach ((string type, string zh, int count, int parts) in summary.OrderByDescending(s => s.Count))
{
    string link = parts == 1
        ? $"[{type}.md](data/{type}.md)"
        : parts <= 12
            ? string.Join(" · ", Enumerable.Range(1, parts).Select(n => $"[{n}](data/{type}.{n}.md)"))
            : $"[1](data/{type}.1.md) … [{parts}](data/{type}.{parts}.md)";
    sumSb.AppendLine($"| `{type}` | {zh} | {count} | {link} |");
}
File.WriteAllText(Path.Combine(dumpDir, "_summary.md"), sumSb.ToString());
Console.WriteLine($"集合一览 -> _summary.md");

void CollectEnum(Type enumType)
{
    if (!enumTypes.TryGetValue(enumType.Name, out SortedDictionary<string, (string Value, string Description)> members))
        enumTypes[enumType.Name] = members = new SortedDictionary<string, (string Value, string Description)>(StringComparer.Ordinal);

    foreach (object v in Enum.GetValues(enumType))
    {
        string desc = enumType.GetMember(v.ToString())[0].GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? "";
        members[v.ToString()] = (Convert.ToInt64(v).ToString(), desc);
    }
}

static bool ShouldSkip(PropertyInfo p)
{
    if (p.GetCustomAttribute<IgnorePropertyAttribute>() != null) return true;
    if (p.GetCustomAttribute<ObsoleteAttribute>() != null) return true;
    // [JsonIgnore] 标记者多为客户端专用或超大字段（如 BitRegion 位图）；
    // 但 DBBindingList 关联是 System.db 中的真实外键，查看器必须展示（只显示条数与链接）。
    if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null &&
        !(p.PropertyType.IsGenericType &&
          p.PropertyType.GetGenericTypeDefinition() == typeof(DBBindingList<>)))
        return true;
    return false;
}

static object SafeGet(PropertyInfo p, object ob)
{
    try { return p.GetValue(ob); }
    catch { return null; }
}
static object SafeGetField(FieldInfo f, object ob)
{
    try { return f.GetValue(ob); }
    catch { return null; }
}

static string IdentityOf(object o)
{
    var props = o.GetType().GetProperties()
        .Where(p => p.GetCustomAttribute<IsIdentityAttribute>() != null)
        .Select(p => SafeGet(p, o))
        .Where(v => v != null)
        .Select(v => v is DBObject db ? Ref(db) : v.ToString())
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .ToList();
    return props.Count == 0 ? "" : string.Join(" / ", props);
}

static string Ref(DBObject db)
{
    string id = IdentityOf(db);
    return id.Length > 0 ? $"{id} (#{db.Index})" : $"(#{db.Index})";
}

static string Render(object value)
{
    switch (value)
    {
        case null:
            return "—";
        case DBObject db:
            return Ref(db);
        case Stats stats:
            return RenderStats(stats);
        case bool b:
            return b ? "true" : "false";
        case DateTime dt:
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        case byte[] bytes:
            return bytes.Length == 0
                ? "[]"
                : "`" + Convert.ToHexString(bytes[..Math.Min(bytes.Length, 16)]) +
                  (bytes.Length > 16 ? $"…`（共 {bytes.Length} 字节）" : "`");
    }

    Type t = value.GetType();
    if (t.IsEnum) return value.ToString();
    if (t.IsArray)
    {
        var arr = (Array)value;
        if (arr.Length == 0) return "[]";
        const int maxItems = 20;
        string suffix = arr.Length > maxItems ? $", …（共 {arr.Length} 项）" : "";
        return "[" + string.Join(", ", arr.Cast<object>().Take(maxItems).Select(Render)) + suffix + "]";
    }

    string s = value.ToString();
    if (string.IsNullOrEmpty(s)) return "—";
    if (s.Length > 2000) return s[..300] + $"…（共 {s.Length} 字符）";
    return s;
}

static string RenderStats(Stats stats)
{
    return string.Join(", ", stats.Values.Select(kv => $"{kv.Key} {kv.Value}"));
}

static string Escape(string s)
{
    if (string.IsNullOrEmpty(s)) return "—";
    return s.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\r", "").Replace("\n", "<br>");
}

// ---------- 玩家视图生成（--view） ----------
// 把 System.db 按「人看的」方式整理：分类浏览、每条目显示关键属性，
// 替代逐字段罗列。原逐字段文档（--dump）保留不动。

static void GenerateViews(Session session, string viewDir)
{
    IList Coll(Type t) => (IList)session.GetCollection(t).GetType().GetField("Binding").GetValue(session.GetCollection(t));

    List<ItemInfo> items = Coll(typeof(ItemInfo)).Cast<ItemInfo>().ToList();
    List<MonsterInfo> monsters = Coll(typeof(MonsterInfo)).Cast<MonsterInfo>().ToList();
    List<MagicInfo> magics = Coll(typeof(MagicInfo)).Cast<MagicInfo>().ToList();
    List<MapInfo> maps = Coll(typeof(MapInfo)).Cast<MapInfo>().ToList();
    List<QuestInfo> quests = Coll(typeof(QuestInfo)).Cast<QuestInfo>().ToList();
    List<NPCInfo> npcs = Coll(typeof(NPCInfo)).Cast<NPCInfo>().ToList();

    const string HeaderNote = "<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --view docs/database/views -->";

    // ---- 中文映射 ----

    static string MirClassZh(MirClass c) => c switch
    {
        MirClass.Warrior => "战士", MirClass.Wizard => "法师", MirClass.Taoist => "道士", MirClass.Assassin => "刺客",
        _ => c.ToString(),
    };
    static string ClassZh(RequiredClass c) => c switch
    {
        RequiredClass.Warrior => "战士", RequiredClass.Wizard => "法师", RequiredClass.Taoist => "道士",
        RequiredClass.Assassin => "刺客", RequiredClass.WarWizTao => "战法道", RequiredClass.WizTao => "法道",
        RequiredClass.AssWar => "刺战", RequiredClass.All => "全职业", _ => "无限制",
    };
    static string ItemTypeZh(ItemType t) => t switch
    {
        ItemType.Nothing => "无", ItemType.Consumable => "消耗品", ItemType.Weapon => "武器", ItemType.Torch => "火把",
        ItemType.Armour => "护甲", ItemType.Helmet => "头盔", ItemType.Necklace => "项链", ItemType.Bracelet => "手镯",
        ItemType.Ring => "戒指", ItemType.Shoes => "鞋子", ItemType.Poison => "毒药", ItemType.Amulet => "护身符",
        ItemType.Meat => "肉类", ItemType.Ore => "矿石", ItemType.Book => "技能书", ItemType.Scroll => "卷轴",
        ItemType.DarkStone => "暗石", ItemType.RefineSpecial => "精炼材料", ItemType.HorseArmour => "马甲",
        ItemType.Flower => "花", ItemType.CompanionFood => "宠物食物", ItemType.CompanionBag => "宠物背包",
        ItemType.CompanionHead => "宠物头饰", ItemType.CompanionBack => "宠物背饰", ItemType.System => "系统物品",
        ItemType.ItemPart => "部件", ItemType.Emblem => "徽章", ItemType.Shield => "盾牌", ItemType.Costume => "时装",
        ItemType.Hook => "钓钩", ItemType.Float => "浮标", ItemType.Bait => "鱼饵", ItemType.Finder => "探测器",
        ItemType.Reel => "卷线器", ItemType.Currency => "货币", ItemType.Bundle => "礼包", ItemType.LootBox => "宝箱",
        ItemType.SocketGem => "宝石", _ => t.ToString(),
    };
    static string WeatherZh(Weather w) => w switch
    {
        Weather.None => "无", Weather.Rain => "雨", Weather.Snow => "雪", Weather.Fog => "雾",
        Weather.Lightning => "雷雨", Weather.SnowFog => "雪雾", Weather.RainLightning => "雷雨",
        Weather.FogLightning => "雾雷", Weather.RainFogLightning => "雨雾雷", _ => w.ToString(),
    };
    static string LightZh(LightSetting l) => l switch
    {
        LightSetting.Default => "默认", LightSetting.Light => "明亮", LightSetting.Night => "黑夜", LightSetting.Twilight => "黄昏",
        _ => l.ToString(),
    };

    // 属性中文名（未列入的显示英文原名；完整字典见 ../data/stats.md）
    var statZh = new Dictionary<string, string>
    {
        ["BaseHealth"]="基础生命", ["BaseMana"]="基础魔法", ["Health"]="生命", ["Mana"]="魔法",
        ["MinAC"]="物防", ["MaxAC"]="物防", ["MinMR"]="魔防", ["MaxMR"]="魔防",
        ["MinDC"]="物攻", ["MaxDC"]="物攻", ["MinMC"]="魔攻", ["MaxMC"]="魔攻", ["MinSC"]="道术", ["MaxSC"]="道术",
        ["Accuracy"]="命中", ["Agility"]="敏捷", ["AttackSpeed"]="攻速", ["Light"]="光照", ["Strength"]="强度", ["Luck"]="幸运",
        ["FireAttack"]="火攻", ["FireResistance"]="火抗", ["IceAttack"]="冰攻", ["IceResistance"]="冰抗",
        ["LightningAttack"]="雷攻", ["LightningResistance"]="雷抗", ["WindAttack"]="风攻", ["WindResistance"]="风抗",
        ["HolyAttack"]="神圣攻击", ["HolyResistance"]="神圣抗性", ["DarkAttack"]="暗黑攻击", ["DarkResistance"]="暗黑抗性",
        ["PhantomAttack"]="幻影攻击", ["PhantomResistance"]="幻影抗性", ["PhysicalResistance"]="物理抗性", ["PoisonResistance"]="毒抗",
        ["FireAffinity"]="火亲和", ["IceAffinity"]="冰亲和", ["LightningAffinity"]="雷亲和", ["WindAffinity"]="风亲和",
        ["HolyAffinity"]="神圣亲和", ["DarkAffinity"]="暗黑亲和", ["PhantomAffinity"]="幻影亲和",
        ["Comfort"]="舒适度", ["LifeSteal"]="吸血", ["ReflectDamage"]="反伤", ["ExperienceRate"]="经验加成",
        ["DropRate"]="爆率加成", ["GoldRate"]="金币加成", ["SkillRate"]="技能熟练度", ["PickUpRadius"]="拾取半径",
        ["Healing"]="治疗", ["HealingCap"]="治疗上限", ["Invisibility"]="隐身", ["HealthPercent"]="生命加成",
        ["ManaPercent"]="魔法加成", ["MCPercent"]="魔法加成", ["DCPercent"]="物攻加成", ["SCPercent"]="道术加成",
        ["CriticalChance"]="暴击率", ["CriticalDamage"]="暴击伤害", ["MagicShield"]="魔法盾", ["Cloak"]="披风",
        ["CloakDamage"]="披风伤害", ["PKPoint"]="PK值", ["BagWeight"]="背包负重", ["WearWeight"]="穿戴负重",
        ["HandWeight"]="手持负重", ["ItemReviveTime"]="物品复活时间", ["MaxRefineChance"]="精炼成功率",
        ["PetDCPercent"]="宠物攻加成", ["BossTracker"]="Boss追踪", ["PlayerTracker"]="玩家追踪",
        ["CompanionRate"]="宠物生成率", ["WeightRate"]="负重加成", ["MagicDefencePercent"]="魔防加成",
        ["PhysicalDefencePercent"]="物防加成", ["MonsterExperience"]="怪物经验", ["MonsterGold"]="怪物金币",
        ["MonsterDrop"]="怪物爆率", ["MonsterDamage"]="怪物伤害", ["MonsterHealth"]="怪物生命",
        ["ProtectionRing"]="保护戒指", ["ClearRing"]="清除戒指", ["TeleportRing"]="传送戒指",
        ["FrostBiteDamage"]="霜咬伤害", ["FrostBiteChance"]="霜咬概率", ["ParalysisChance"]="麻痹概率",
        ["SlowChance"]="减速概率", ["SilenceChance"]="沉默概率", ["BlockChance"]="格挡概率",
        ["EvasionChance"]="闪避概率", ["IgnoreStealth"]="识破隐身", ["Rebirth"]="重生", ["Focus"]="专注",
        ["SizePercent"]="体型", ["Invincibility"]="无敌", ["SoulResonance"]="灵魂共鸣", ["Fame"]="声望",
        ["ElementalSwords"]="元素剑", ["RoamDistance"]="游荡距离", ["ThrowDistance"]="投掷距离", ["AutoCast"]="自动施法",
        ["Experience"]="经验", ["DeathDrops"]="死亡掉落", ["FragmentRate"]="碎片率", ["MapSummoning"]="地图召唤",
        ["AvailableHuntGold"]="可用金币", ["AvailableHuntGoldCap"]="金币上限", ["CompanionInventory"]="宠物背包",
        ["CompanionBagWeight"]="宠物负重", ["CompanionHunger"]="宠物饥饿", ["CompanionCollection"]="宠物收集",
        ["RecallSet"]="召回套装", ["ItemIndex"]="物品索引", ["BaseExperienceRate"]="基础经验加成",
        ["BaseGoldRate"]="基础金币加成", ["BaseDropRate"]="基础爆率加成", ["MaxMonsterExperience"]="经验上限",
        ["MaxMonsterGold"]="金币上限", ["MaxMonsterDrop"]="爆率上限", ["MaxMonsterDamage"]="伤害上限",
        ["MaxMonsterHealth"]="生命上限",
    };
    string Zh(Stat s) => statZh.TryGetValue(s.ToString(), out string z) ? z : s.ToString();
    static bool IsMin(Stat s) => s.ToString().StartsWith("Min");
    static Stat MaxOf(Stat s) => (Stat)Enum.Parse(typeof(Stat), "Max" + s.ToString()[3..]);

    // 「物攻 6-18 · 魔防 3-3 · 攻速 +1」；Min/Max 对合并为区间，0 值跳过
    string ViewStats(Stats stats)
    {
        if (stats == null || stats.Values.Count == 0) return "";
        var parts = new List<(int Order, string Text)>();
        var used = new HashSet<Stat>();
        foreach (KeyValuePair<Stat, int> kv in stats.Values)
        {
            Stat k = kv.Key;
            if (used.Contains(k)) continue;
            string name = Zh(k);
            if (IsMin(k) && stats.Values.TryGetValue(MaxOf(k), out int max))
            {
                used.Add(MaxOf(k));
                parts.Add(((int)k, $"{name} {kv.Value}-{max}"));
            }
            else
                parts.Add(((int)k, $"{name} {(kv.Value > 0 ? "+" : "")}{kv.Value}"));
        }
        return string.Join(" · ", parts.OrderBy(p => p.Order).Select(p => p.Text));
    }

    // 掉落摘要：概率 1/N（N 小=概率高），取前若干种
    string DropList(IEnumerable<DropInfo> drops, int max = 6)
    {
        List<DropInfo> list = drops.Where(d => d.Item != null).OrderBy(d => d.Chance).ToList();
        if (list.Count == 0) return "";
        var cells = new List<string>();
        foreach (DropInfo d in list.Take(max))
        {
            string amount = d.Amount > 1 ? $" ×{d.Amount}" : "";
            string chance = d.Chance > 0 ? $" 1/{d.Chance}" : "";
            string set = d.DropSet > 0 ? $"（组{d.DropSet}）" : "";
            cells.Add($"{Escape(d.Item.ItemName)}{amount}{chance}{set}");
        }
        return string.Join(" · ", cells) + (list.Count > max ? $" …共 {list.Count} 种" : "");
    }

    // 刷新位置摘要：地图 ×数量 聚合
    string RespawnList(MonsterInfo m)
    {
        if (m.Respawns == null || m.Respawns.Count == 0) return "";
        List<string> groups = m.Respawns
            .Where(r => r.Region?.Map != null)
            .GroupBy(r => r.Region.Map.FileName)
            .Select(g => $"{Escape(g.Key)} ×{g.Sum(r => r.Count)}")
            .ToList();
        return string.Join("、", groups.Take(6)) + (groups.Count > 6 ? $" …共 {groups.Count} 处" : "");
    }

    // ---- skills.md ----

    var sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# 职业技能总览");
    sb.AppendLine();
    sb.AppendLine($"> 四职业全部 {magics.Count} 个技能：表格速查，条目看详情。");
    sb.AppendLine();
    foreach (MirClass cls in new[] { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist, MirClass.Assassin })
    {
        List<MagicInfo> list = magics.Where(m => m.Class == cls).OrderBy(m => m.NeedLevel1).ToList();
        sb.AppendLine($"## {MirClassZh(cls)}（{list.Count} 个）");
        sb.AppendLine();
        sb.AppendLine("| # | 名称 | 类型 | 属性 | 威力 | 耗蓝 | 延迟 | 等级门槛 |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (MagicInfo m in list)
            sb.AppendLine($"| {m.Index} | {Escape(m.Name)} | {m.Magic} | {m.Property} | {m.MinBasePower}-{m.MaxBasePower} | {m.BaseCost} | {m.Delay} | {m.NeedLevel1}/{m.NeedLevel2}/{m.NeedLevel3} |");
        sb.AppendLine();
        foreach (MagicInfo m in list)
        {
            sb.AppendLine($"### {m.Index} · {Escape(m.Name)}");
            sb.AppendLine();
            sb.AppendLine($"- 类型：{m.Magic} · 派系：{m.School} · 属性：{m.Property} · 图标：{m.Icon}");
            sb.AppendLine($"- 威力：{m.MinBasePower}-{m.MaxBasePower}，每级 +{m.MinLevelPower}-{m.MaxLevelPower} · 耗蓝：{m.BaseCost}，每级 +{m.LevelCost} · 延迟：{m.Delay}ms");
            sb.AppendLine($"- 等级门槛：{m.NeedLevel1} / {m.NeedLevel2} / {m.NeedLevel3} 级 · 熟练度：{m.Experience1} / {m.Experience2} / {m.Experience3}");
            if (!string.IsNullOrEmpty(m.Description))
                sb.AppendLine($"- 说明：{Escape(m.Description)}");
            sb.AppendLine();
        }
    }
    File.WriteAllText(Path.Combine(viewDir, "skills.md"), sb.ToString());
    Console.WriteLine($"技能 -> skills.md（{magics.Count} 个）");

    // ---- monsters.md ----

    sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# 怪物图鉴");
    sb.AppendLine();
    sb.AppendLine($"> 共 {monsters.Count} 种怪物：属性/刷新/掉落一目了然。属性名含义见 [属性字典](../data/stats.md)。");
    sb.AppendLine();
    List<MonsterInfo> bosses = monsters.Where(m => m.IsBoss).OrderBy(m => m.Level).ToList();
    sb.AppendLine($"## Boss（{bosses.Count} 个）");
    sb.AppendLine();
    foreach (MonsterInfo m in bosses) AppendMonster(sb, m);
    var tiers = new (int Lo, int Hi, string Title)[] {
        (0, 29, $"新手区（0-29 级，{monsters.Count(m => !m.IsBoss && m.Level <= 29)} 种）"),
        (30, 59, $"中级区（30-59 级，{monsters.Count(m => !m.IsBoss && m.Level is >= 30 and <= 59)} 种）"),
        (60, 89, $"高级区（60-89 级，{monsters.Count(m => !m.IsBoss && m.Level is >= 60 and <= 89)} 种）"),
        (90, int.MaxValue, $"顶级区（90 级以上，{monsters.Count(m => !m.IsBoss && m.Level >= 90)} 种）"),
    };
    foreach ((int lo, int hi, string title) in tiers)
    {
        List<MonsterInfo> list = monsters.Where(m => !m.IsBoss && m.Level >= lo && m.Level <= hi).OrderBy(m => m.Level).ThenBy(m => m.MonsterName).ToList();
        if (list.Count == 0) continue;
        sb.AppendLine($"## {title}");
        sb.AppendLine();
        foreach (MonsterInfo m in list) AppendMonster(sb, m);
    }
    File.WriteAllText(Path.Combine(viewDir, "monsters.md"), sb.ToString());
    Console.WriteLine($"怪物 -> monsters.md（{monsters.Count} 种）");

    void AppendMonster(StringBuilder b, MonsterInfo m)
    {
        b.AppendLine($"### {m.Index} · {Escape(m.MonsterName)} · {m.Level} 级");
        b.AppendLine();
        string stats = ViewStats(m.Stats);
        var feat = new List<string>();
        if (m.IsBoss) feat.Add("Boss");
        if (m.Undead) feat.Add("亡灵");
        if (m.CanTame) feat.Add("可捕捉");
        if (m.CanPush) feat.Add("可推动");
        b.AppendLine($"- 属性：{(stats.Length > 0 ? stats : "—")}");
        b.AppendLine($"- 特征：{(feat.Count > 0 ? string.Join("、", feat) : "普通")} · 视野 {m.ViewRange} · 攻击间隔 {m.AttackDelay}ms · 经验 {m.Experience}");
        string resp = RespawnList(m);
        if (resp.Length > 0) b.AppendLine($"- 刷新：{resp}");
        string drops = DropList(m.Drops);
        if (drops.Length > 0) b.AppendLine($"- 掉落：{drops}");
        b.AppendLine();
    }

    // ---- items/*.md ----

    static string ItemCategory(ItemType t) => t switch
    {
        ItemType.Weapon => "weapons",
        ItemType.Armour or ItemType.Helmet or ItemType.Shoes or ItemType.Shield or ItemType.Costume or ItemType.HorseArmour => "armour",
        ItemType.Necklace or ItemType.Bracelet or ItemType.Ring or ItemType.Amulet => "jewellery",
        ItemType.Consumable or ItemType.Poison or ItemType.Book or ItemType.Scroll or ItemType.Meat or ItemType.Flower or ItemType.Torch or ItemType.Bait or ItemType.Float or ItemType.Hook or ItemType.Finder or ItemType.Reel => "consumables",
        _ => "materials",
    };
    var itemFiles = new (string File, string Title, ItemType[] Types)[]
    {
        ("weapons", "武器", new[] { ItemType.Weapon }),
        ("armour", "防具（护甲 / 头盔 / 鞋子 / 盾牌 / 时装 / 马甲）", new[] { ItemType.Armour, ItemType.Helmet, ItemType.Shoes, ItemType.Shield, ItemType.Costume, ItemType.HorseArmour }),
        ("jewellery", "饰品（项链 / 戒指 / 手镯 / 护身符）", new[] { ItemType.Necklace, ItemType.Bracelet, ItemType.Ring, ItemType.Amulet }),
        ("consumables", "消耗品（药水 / 毒药 / 技能书 / 卷轴 / 肉类 / 花 / 火把 / 钓鱼用品）", new[] { ItemType.Consumable, ItemType.Poison, ItemType.Book, ItemType.Scroll, ItemType.Meat, ItemType.Flower, ItemType.Torch, ItemType.Bait, ItemType.Float, ItemType.Hook, ItemType.Finder, ItemType.Reel }),
        ("materials", "材料与其他（矿石 / 暗石 / 精炼 / 宠物用品 / 部件 / 徽章 / 宝石 / 货币 / 礼包 / 宝箱）", new[] { ItemType.Ore, ItemType.DarkStone, ItemType.RefineSpecial, ItemType.CompanionFood, ItemType.CompanionBag, ItemType.CompanionHead, ItemType.CompanionBack, ItemType.ItemPart, ItemType.Emblem, ItemType.SocketGem, ItemType.Currency, ItemType.Bundle, ItemType.LootBox, ItemType.System, ItemType.Nothing }),
    };
    foreach ((string file, string title, ItemType[] types) in itemFiles)
    {
        List<ItemInfo> list = items.Where(i => types.Contains(i.ItemType)).OrderBy(i => i.RequiredAmount).ThenBy(i => i.ItemName).ToList();
        sb = new StringBuilder();
        sb.AppendLine(HeaderNote);
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"> 共 {list.Count} 件。条目按需要等级排序；属性名含义见 [属性字典](../../data/stats.md)。");
        sb.AppendLine();
        sb.AppendLine("[← 返回总览](../README.md)");
        sb.AppendLine();
        foreach (ItemInfo it in list)
        {
            string cls = ClassZh(it.RequiredClass);
            string lvl = it.RequiredAmount > 0 ? $" · {it.RequiredAmount} 级" : "";
            sb.AppendLine($"### {it.Index} · {Escape(it.ItemName)}（{cls}{lvl}）");
            sb.AppendLine();
            string stats = ViewStats(it.Stats);
            if (stats.Length > 0) sb.AppendLine($"- 属性：{stats}");
            var misc = new List<string> { $"类型 {ItemTypeZh(it.ItemType)}", $"价格 {it.Price}", $"重量 {it.Weight}" };
            if (it.Durability > 0) misc.Add($"耐久 {it.Durability}");
            if (it.CanRepair) misc.Add("可修理");
            if (it.StackSize > 1) misc.Add($"可堆叠 {it.StackSize}");
            if (it.Rarity != Rarity.Common) misc.Add($"稀有度 {it.Rarity}");
            sb.AppendLine($"- {string.Join(" · ", misc)}");
            string drops = DropList(it.Drops);
            if (drops.Length > 0) sb.AppendLine($"- 掉落：{drops}");
            if (it.Set != null) sb.AppendLine($"- 套装：{Escape(it.Set.SetName)}");
            if (!string.IsNullOrEmpty(it.Description)) sb.AppendLine($"- 说明：{Escape(it.Description)}");
            sb.AppendLine();
        }
        File.WriteAllText(Path.Combine(viewDir, "items", $"{file}.md"), sb.ToString());
        Console.WriteLine($"物品-{file} -> items/{file}.md（{list.Count} 件）");
    }

    // ---- maps.md ----

    sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# 地图总览");
    sb.AppendLine();
    sb.AppendLine($"> 共 {maps.Count} 张地图：等级范围、环境、其中刷新的怪物。");
    sb.AppendLine();
    foreach (MapInfo map in maps.OrderBy(m => m.MinimumLevel).ThenBy(m => m.FileName))
    {
        string mapTitle = string.IsNullOrEmpty(map.Description) ? map.FileName : map.Description;
        sb.AppendLine($"### {map.Index} · {Escape(mapTitle)}");
        sb.AppendLine();
        var info = new List<string> { $"文件 {Escape(map.FileName)}", $"等级 {map.MinimumLevel}-{map.MaximumLevel}", $"光照 {LightZh(map.Light)}", $"天气 {WeatherZh(map.Weather)}" };
        if (map.RequiredClass != RequiredClass.None && map.RequiredClass != RequiredClass.All)
            info.Add($"职业限制 {ClassZh(map.RequiredClass)}");
        if (map.CanHorse) info.Add("可骑马");
        if (map.CanMine) info.Add("可挖矿");
        if (map.CanMarriageRecall) info.Add("可夫妻召唤");
        sb.AppendLine($"- {string.Join(" · ", info)}");
        List<(MonsterInfo Monster, int Count)> monGroups = map.Regions
            .Where(r => r.Respawns != null)
            .SelectMany(r => r.Respawns)
            .Where(r => r.Monster != null)
            .GroupBy(r => r.Monster)
            .Select(g => (g.Key, Count: g.Sum(r => r.Count)))
            .OrderByDescending(x => x.Count)
            .ToList();
        if (monGroups.Count > 0)
        {
            var cells = monGroups.Select(g =>
                $"{Escape(g.Monster.MonsterName)} ×{g.Count}" + (g.Monster.IsBoss ? "（Boss）" : ""));
            sb.AppendLine($"- 怪物：{string.Join("、", cells)}");
        }
        sb.AppendLine();
    }
    File.WriteAllText(Path.Combine(viewDir, "maps.md"), sb.ToString());
    Console.WriteLine($"地图 -> maps.md（{maps.Count} 张）");

    // ---- quests.md ----

    sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# 任务");
    sb.AppendLine();
    sb.AppendLine($"> 共 {quests.Count} 个任务：接取 / 目标 / 奖励。");
    sb.AppendLine();
    foreach (QuestInfo q in quests.OrderBy(q => q.QuestName))
    {
        sb.AppendLine($"### {q.Index} · {Escape(q.QuestName)}（{q.QuestType}）");
        sb.AppendLine();
        string start = q.StartNPC != null ? Escape(q.StartNPC.NPCName) : "—";
        string finish = q.FinishNPC != null ? Escape(q.FinishNPC.NPCName) : "—";
        sb.AppendLine($"- 接取：{start} → 完成：{finish}");
        if (!string.IsNullOrEmpty(q.AcceptText)) sb.AppendLine($"- 说明：{Escape(q.AcceptText)}");
        if (q.Tasks != null && q.Tasks.Count > 0)
        {
            var tasks = new List<string>();
            foreach (QuestTask t in q.Tasks)
            {
                string verb = t.Task switch
                {
                    QuestTaskType.KillMonster => "杀死",
                    QuestTaskType.GainItem => "收集",
                    QuestTaskType.VisitRegion => "前往",
                    _ => t.Task.ToString(),
                };
                var target = new List<string>();
                string mob = t.MobDescription;
                if (string.IsNullOrEmpty(mob) && t.MonsterDetails != null && t.MonsterDetails.Count > 0)
                    mob = t.MonsterDetails[0].Monster?.MonsterName ?? "";
                if (!string.IsNullOrEmpty(mob)) target.Add(Escape(mob));
                if (t.Amount > 0) target.Add($"×{t.Amount}");
                if (t.RegionParameter?.Map != null) target.Add($"（{Escape(t.RegionParameter.Map.FileName)}）");
                if (t.MonsterDetails != null && t.MonsterDetails.Count > 0)
                {
                    target.Add("（" + string.Join("、", t.MonsterDetails.Select(d =>
                        $"{Escape(d.Monster?.MonsterName ?? "?")} 1/{d.Chance}")) + "）");
                }
                tasks.Add($"{verb} {string.Join(" ", target)}");
            }
            sb.AppendLine($"- 目标：{string.Join(" · ", tasks)}");
        }
        if (q.Rewards != null && q.Rewards.Count > 0)
        {
            var rewards = q.Rewards.Select(r =>
            {
                string itemName = r.Item?.ItemName ?? "金币";
                string amt = r.Amount > 1 ? $" ×{r.Amount}" : "";
                string cls = r.Class != RequiredClass.None ? $"（{ClassZh(r.Class)}）" : "";
                return $"{Escape(itemName)}{amt}{cls}";
            });
            string choice = q.Rewards.Any(r => r.Choice) ? "（可选其一）" : "";
            sb.AppendLine($"- 奖励：{string.Join(" · ", rewards)}{choice}");
        }
        sb.AppendLine();
    }
    File.WriteAllText(Path.Combine(viewDir, "quests.md"), sb.ToString());
    Console.WriteLine($"任务 -> quests.md（{quests.Count} 个）");

    // ---- npcs.md ----

    sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# NPC");
    sb.AppendLine();
    sb.AppendLine($"> 共 {npcs.Count} 个 NPC：位置 / 功能 / 关联任务。");
    sb.AppendLine();
    foreach (NPCInfo n in npcs.OrderBy(n => n.Region?.Map?.FileName).ThenBy(n => n.NPCName))
    {
        sb.AppendLine($"### {n.Index} · {Escape(n.NPCName)}");
        sb.AppendLine();
        var info = new List<string>();
        if (n.Region?.Map != null) info.Add($"地图 {Escape(n.Region.Map.FileName)}");
        info.Add($"图标 {n.Image} · 头像 {n.FaceImage}");
        if (n.GoodsIndex > 0) info.Add($"商店 {n.GoodsIndex} 页");
        if (n.StartQuests != null && n.StartQuests.Count > 0) info.Add($"可接任务 {n.StartQuests.Count} 个");
        if (n.FinishQuests != null && n.FinishQuests.Count > 0) info.Add($"可交任务 {n.FinishQuests.Count} 个");
        sb.AppendLine($"- {string.Join(" · ", info)}");
        if (n.EntryPage != null && !string.IsNullOrEmpty(n.EntryPage.Description))
            sb.AppendLine($"- 介绍：{Escape(n.EntryPage.Description)}");
        sb.AppendLine();
    }
    File.WriteAllText(Path.Combine(viewDir, "npcs.md"), sb.ToString());
    Console.WriteLine($"NPC -> npcs.md（{npcs.Count} 个）");

    // ---- README.md ----

    sb = new StringBuilder();
    sb.AppendLine(HeaderNote);
    sb.AppendLine();
    sb.AppendLine("# 游戏数据总览（玩家视图）");
    sb.AppendLine();
    sb.AppendLine($"> 把 System.db 的 {items.Count} 件物品 / {monsters.Count} 种怪物 / {magics.Count} 个技能 / {maps.Count} 张地图 / {quests.Count} 个任务 / {npcs.Count} 个 NPC 整理成「人看的」分类视图。");
    sb.AppendLine($"> 原始逐字段数据保留在 [../data](../data)（说明见 [../README.md](../README.md)）。");
    sb.AppendLine();
    sb.AppendLine("## 板块");
    sb.AppendLine();
    sb.AppendLine("| 板块 | 文件 | 内容 |");
    sb.AppendLine("|---|---|---|");
    sb.AppendLine("| 职业技能 | [skills.md](skills.md) | 四职业全部技能：威力 / 耗蓝 / 等级门槛 / 说明 |");
    sb.AppendLine("| 怪物图鉴 | [monsters.md](monsters.md) | Boss 与等级分区：属性 / 刷新地图 / 掉落 |");
    sb.AppendLine("| 物品 · 武器 | [items/weapons.md](items/weapons.md) | 全部武器 |");
    sb.AppendLine("| 物品 · 防具 | [items/armour.md](items/armour.md) | 护甲 / 头盔 / 鞋子 / 盾牌 / 时装 |");
    sb.AppendLine("| 物品 · 饰品 | [items/jewellery.md](items/jewellery.md) | 项链 / 戒指 / 手镯 / 护身符 |");
    sb.AppendLine("| 物品 · 消耗品 | [items/consumables.md](items/consumables.md) | 药水 / 技能书 / 卷轴 / 肉类 / 钓鱼用品 |");
    sb.AppendLine("| 物品 · 材料 | [items/materials.md](items/materials.md) | 矿石 / 宝石 / 货币 / 礼包等 |");
    sb.AppendLine("| 地图 | [maps.md](maps.md) | 等级范围 / 环境 / 怪物分布 |");
    sb.AppendLine("| 任务 | [quests.md](quests.md) | 接取 / 目标 / 奖励 |");
    sb.AppendLine("| NPC | [npcs.md](npcs.md) | 位置 / 功能 / 关联任务 |");
    sb.AppendLine();
    sb.AppendLine("## 阅读约定");
    sb.AppendLine();
    sb.AppendLine("- 职业：战 = 战士，法 = 法师，道 = 道士，刺 = 刺客；「全」= 全职业");
    sb.AppendLine("- 属性：物攻 = 物理攻击，魔攻 = 魔法攻击，道术 = 道术攻击，物防 / 魔防 = 物理 / 魔法防御；完整属性字典见 [../data/stats.md](../data/stats.md)");
    sb.AppendLine("- 掉落「1/30」表示三十分之一概率；「组N」为不同刷新点的掉落组");
    sb.AppendLine("- 图标 / 头像数字为客户端图片资源编号");
    File.WriteAllText(Path.Combine(viewDir, "README.md"), sb.ToString());
    Console.WriteLine($"索引 -> README.md");
}

// ---------- 图片映射导出（--images） ----------
// 输出各实体在客户端图库中的图片编号 (JSON)，供百科渲染使用。
//   monsters:  MonsterName -> Image 枚举名 (MonsterImage)
//   items:     ItemName    -> Image (StoreItem/Inventory 帧号)
//   skills:    Name        -> Icon (MIcon 帧号)
//   npcs:      NPCName     -> { image, face } (NPC.wil / NPCface.wil)
//   companions:MonsterName -> { price, available } (宠物外观 = 绑定怪物)
static void GenerateImages(Session session, string outPath)
{
    IList Coll(Type t) => (IList)session.GetCollection(t).GetType().GetField("Binding").GetValue(session.GetCollection(t));

    var monsters = Coll(typeof(MonsterInfo)).Cast<MonsterInfo>()
        .GroupBy(m => m.MonsterName)
        .ToDictionary(g => g.Key, g => g.First().Image.ToString());
    var items = Coll(typeof(ItemInfo)).Cast<ItemInfo>()
        .GroupBy(i => i.ItemName)
        .ToDictionary(g => g.Key, g => g.First().Image);
    var skills = Coll(typeof(MagicInfo)).Cast<MagicInfo>()
        .GroupBy(s => s.Name)
        .ToDictionary(g => g.Key, g => g.First().Icon);
    var npcs = Coll(typeof(NPCInfo)).Cast<NPCInfo>()
        .GroupBy(n => n.NPCName)
        .ToDictionary(g => g.Key, g => new { image = g.First().Image, face = g.First().FaceImage });
    var companions = Coll(typeof(CompanionInfo)).Cast<CompanionInfo>()
        .GroupBy(c => c.MonsterInfo?.MonsterName ?? "?")
        .ToDictionary(g => g.Key, g => new { price = g.First().Price, available = g.First().Available });

    var obj = new
    {
        monsters,
        items,
        skills,
        npcs,
        companions,
    };
    string json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
    File.WriteAllText(outPath, json);
    Console.WriteLine($"怪物 {monsters.Count} / 物品 {items.Count} / 技能 {skills.Count} / NPC {npcs.Count} / 宠物 {companions.Count}");
}

// ---------- 商店数据导出（--stores） ----------
// NPC 商店 = NPCGood 按 GoodsIndex 分组，NPCInfo.GoodsIndex 指向同组。
// 输出 NPC（含地图/商店页）与货品（含物品名/倍率），供百科「商店」板块渲染。
static void GenerateStores(Session session, string outPath)
{
    IList Coll(Type t) => (IList)session.GetCollection(t).GetType().GetField("Binding").GetValue(session.GetCollection(t));

    var npcs = Coll(typeof(NPCInfo)).Cast<NPCInfo>()
        .Select(n => new
        {
            index = n.Index,
            name = n.NPCName,
            map = n.Region?.ServerDescription ?? "",
            mapFile = n.Region?.Map?.FileName ?? "",
            goodsIndex = n.GoodsIndex,
            image = n.Image,
            face = n.FaceImage,
        })
        .OrderBy(x => x.goodsIndex).ThenBy(x => x.index)
        .ToList();

    var goods = Coll(typeof(NPCGood)).Cast<NPCGood>()
        .Select(g => new
        {
            index = g.Index,
            item = g.Item?.ItemName ?? "",
            itemIndex = g.Item?.Index ?? 0,
            rate = g.Rate,
            goodsIndex = g.GoodsIndex,
        })
        .OrderBy(x => x.goodsIndex).ThenBy(x => x.index)
        .ToList();

    var obj = new
    {
        npcs,
        goods,
        storeCount = goods.Select(g => g.goodsIndex).DefaultIfEmpty(0).Max(),
    };
    string json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
    File.WriteAllText(outPath, json);
    Console.WriteLine($"NPC {npcs.Count} / 货品 {goods.Count} / 商店页 {obj.storeCount}");
}
