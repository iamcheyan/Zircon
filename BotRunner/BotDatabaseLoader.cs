using Library;
using Library.MirDB;
using Library.SystemModels;
using MirDB;
using System.Reflection;

namespace Zircon.BotRunner;

/// <summary>
/// Loads the same client-side System.db reference tables that are needed by
/// Packet.CompleteObject while deserializing server packets.
/// </summary>
public static class BotDatabaseLoader
{
    public static Session Session { get; private set; }

    public static void Load(string root)
    {
        root = Path.GetFullPath(root);
        if (!root.EndsWith(Path.DirectorySeparatorChar)) root += Path.DirectorySeparatorChar;
        Session = new Session(SessionMode.Users, root) { BackUp = false };
        Session.Initialize(Assembly.GetAssembly(typeof(ItemInfo)));
        if (!Session.SystemDatabaseExists)
            throw new FileNotFoundException($"System.db not found in {root}");

        Globals.ItemInfoList = Session.GetCollection<ItemInfo>();
        Globals.MagicInfoList = Session.GetCollection<MagicInfo>();
        Globals.MapInfoList = Session.GetCollection<MapInfo>();
        Globals.CurrencyInfoList = Session.GetCollection<CurrencyInfo>();
        Globals.InstanceInfoList = Session.GetCollection<InstanceInfo>();
        Globals.NPCPageList = Session.GetCollection<NPCPage>();
        Globals.MonsterInfoList = Session.GetCollection<MonsterInfo>();
        Globals.FishingInfoList = Session.GetCollection<FishingInfo>();
        Globals.StoreInfoList = Session.GetCollection<StoreInfo>();
        Globals.NPCInfoList = Session.GetCollection<NPCInfo>();
        Globals.MovementInfoList = Session.GetCollection<MovementInfo>();
        Globals.QuestInfoList = Session.GetCollection<QuestInfo>();
        Globals.QuestTaskList = Session.GetCollection<QuestTask>();
        Globals.CompanionInfoList = Session.GetCollection<CompanionInfo>();
        Globals.CompanionLevelInfoList = Session.GetCollection<CompanionLevelInfo>();
        Globals.DisciplineInfoList = Session.GetCollection<DisciplineInfo>();
        Globals.FameInfoList = Session.GetCollection<FameInfo>();
        Globals.BundleInfoList = Session.GetCollection<BundleInfo>();
        Globals.LootBoxInfoList = Session.GetCollection<LootBoxInfo>();
        Globals.HelpInfoList = Session.GetCollection<HelpInfo>();
        Globals.MilestoneInfoList = Session.GetCollection<MilestoneInfo>();
        Globals.MilestoneTaskInfoList = Session.GetCollection<MilestoneInfoTask>();

        Console.WriteLine($"[DB] System.db loaded: items={Globals.ItemInfoList.Count}, maps={Globals.MapInfoList.Count}, monsters={Globals.MonsterInfoList.Count}, magics={Globals.MagicInfoList.Count}");

        // 供给链诊断: 卖/买 NPC 与关键地图的 CanAutoPath(服务端寻路开关)
        foreach (var npc in Globals.NPCInfoList.Binding)
        {
            bool sell = npc.EntryPage != null && new[] { npc.EntryPage }
                .Concat(npc.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>())
                .Any(x => x.DialogType == NPCDialogType.BuySell && x.Types?.Count > 0);
            if (sell)
                Console.WriteLine($"[DB] sell npc: {npc.NPCName} map={npc.Region?.Map?.Index}({npc.Region?.Map?.FileName}) autoPath={npc.Region?.Map?.CanAutoPath}");
        }
        var homeMap = Globals.MapInfoList.Binding.FirstOrDefault(m => m.Index == 1);
        Console.WriteLine($"[DB] map1 autoPath={homeMap?.CanAutoPath} lights={homeMap?.Light}");

        // 供给 NPC 的 region 坐标与 map1 守卫位置(诊断寻路可达性)
        foreach (var name in new[] { "David", "Mr. Kang", "Isaac", "Lennard", "Amy" })
        {
            var npc = Globals.NPCInfoList.Binding.FirstOrDefault(n =>
                n.NPCName?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            if (npc?.Region?.Map != null)
            {
                var pts = npc.Region.GetPoints(MapInfoWidth(npc.Region.Map));
                var first = pts != null && pts.Count > 0 ? pts.First() : System.Drawing.Point.Empty;
                Console.WriteLine($"[DB] {name}: map={npc.Region.Map.Index} regionPt={first} pts={pts?.Count ?? 0}");
            }
        }
        if (homeMap?.Guards != null)
        {
            var guardList = homeMap.Guards.Select(g => $"{g.Monster?.Index}:{(g.X, g.Y)}").Take(6);
            Console.WriteLine($"[DB] map1 guards(total {homeMap.Guards.Count}): {string.Join(" ", guardList)}");
        }
        // 卖护身符(Amulet)的 NPC —— 道士召唤必需品(沿入口页链走 3 层找货架)
        foreach (var npc in Globals.NPCInfoList.Binding)
        {
            var seen = new HashSet<NPCPage>();
            var queue = new Queue<NPCPage>();
            if (npc.EntryPage != null) { queue.Enqueue(npc.EntryPage); seen.Add(npc.EntryPage); }
            bool sellsAmulet = false;
            for (int depth = 0; queue.Count > 0 && depth < 40; depth++)
            {
                var page = queue.Dequeue();
                if (page.Goods != null && page.Goods.Any(g => g.Item?.ItemType == ItemType.Amulet))
                { sellsAmulet = true; break; }
                foreach (var b in page.Buttons ?? Enumerable.Empty<NPCButton>())
                    if (b.DestinationPage != null && seen.Add(b.DestinationPage))
                        queue.Enqueue(b.DestinationPage);
            }
            if (sellsAmulet)
            {
                var amulets = npc.EntryPage != null
                    ? Traverse(npc.EntryPage).SelectMany(p => p.Goods ?? Enumerable.Empty<NPCGood>())
                        .Where(g => g.Item?.ItemType == ItemType.Amulet)
                        .Select(g => $"{g.Item.ItemName}(shape={g.Item.Shape})").Distinct()
                    : Enumerable.Empty<string>();
                Console.WriteLine($"[DB] amulet seller: {npc.NPCName} map={npc.Region?.Map?.Index} goods=[{string.Join(",", amulets)}]");
            }
        }
    }

    private static IEnumerable<NPCPage> Traverse(NPCPage start)
    {
        var seen = new HashSet<NPCPage> { start };
        var queue = new Queue<NPCPage>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var page = queue.Dequeue();
            yield return page;
            foreach (var b in page.Buttons ?? Enumerable.Empty<NPCButton>())
                if (b.DestinationPage != null && seen.Add(b.DestinationPage))
                    queue.Enqueue(b.DestinationPage);
        }
    }

    private static int MapInfoWidth(Library.SystemModels.MapInfo map)
    {
        // 与 BotAgent.MapWidthOf 相同的 .map 头解析; loader 阶段没有 BotMap,
        // 这里直接读文件头取宽度。
        string path = Path.Combine("Debug/Client/Map", $"{map.FileName}.map");
        if (!File.Exists(path)) return 1000;
        using var fs = File.OpenRead(path);
        var buf = new byte[52];
        fs.ReadExactly(buf);
        return BitConverter.ToInt16(buf, 0);
    }
}
