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

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--dump")
    {
        dumpDir = args[++i];
        continue;
    }
    root = args[i];
}
if (!Path.IsPathRooted(root)) root = Path.GetFullPath(root);

var session = new Session(SessionMode.Users, root);
session.Initialize(
    typeof(ItemInfo).Assembly,        // LibraryCore（SystemModels）
    typeof(Server.DBModels.AccountInfo).Assembly // ServerLibrary（DBModels）
);

Console.WriteLine($"数据库: {session.SystemPath}");
Console.WriteLine($"版本:   {session.SystemDatabaseVersion}");

if (dumpDir == null)
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
