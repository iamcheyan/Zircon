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
    }
}
