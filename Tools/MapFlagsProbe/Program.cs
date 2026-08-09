// UsersDbProbe — 查角色职业、BindPoint 现状与地图/安全区配置。
using Library.SystemModels;
using MirDB;
using Server.DBModels;
using System.Drawing;

string root = "/home/tetsuya/development/Zircon/Debug/ServerCore/Database/";
var session = new Session(SessionMode.Users, root);
session.Initialize(typeof(ItemInfo).Assembly, typeof(AccountInfo).Assembly);

var chars = session.GetCollection<CharacterInfo>();
foreach (var c in chars.Binding)
{
    var bindMap = c.BindPoint?.BindRegion?.Map?.Index;
    Console.WriteLine($"Char idx={c.Index} name={c.CharacterName} class={c.Class} bindMap={bindMap} curMap={c.CurrentMap?.Index} startclass={c.BindPoint?.StartClass} loc={c.CurrentLocation}");
}

Console.WriteLine("--- SafeZones ---");
var sz = session.GetCollection<SafeZoneInfo>();
foreach (var s in sz.Binding)
{
    var m = s.BindRegion?.Map?.Index;
    if (m == 1 || m == 11 || m == 459)
        Console.WriteLine($"SZ map={m} startClass={s.StartClass} redZone={s.RedZone} bindPts={s.ValidBindPoints.Count} region={s.BindRegion?.ServerDescription}");
}

Console.WriteLine("--- Maps 1/11/459 ---");
var maps = session.GetCollection<MapInfo>();
foreach (var map in maps.Binding)
{
    if (map.Index == 1 || map.Index == 11 || map.Index == 459)
        Console.WriteLine($"Map idx={map.Index} file={map.FileName} desc={map.ServerDescription} minLv={map.MinimumLevel} maxLv={map.MaximumLevel} reqClass={map.RequiredClass}");
}

if (args.Contains("--fix-bind"))
{
    Console.WriteLine("--- Fixing bind/current map to 1 ---");
    var map1 = session.GetCollection<MapInfo>().Binding.First(x => x.Index == 1);
    var sz1 = session.GetCollection<SafeZoneInfo>().Binding
        .First(x => x.BindRegion?.Map?.Index == 1);
    foreach (var c in chars.Binding)
    {
        int cur = c.CurrentMap?.Index ?? -1;
        int bind = c.BindPoint?.BindRegion?.Map?.Index ?? -1;
        string inst = c.CurrentInstance != null ? $"idx={c.CurrentInstance.Index}" : "null";
        if (cur != 1 || bind != 1 || c.CurrentInstance != null || c.CurrentLocation.X < 100 || c.CurrentLocation.Y < 100)
        {
            Console.WriteLine($"  fix {c.CharacterName}: currentMap {cur} -> 1, bind {bind} -> 1, inst {inst} -> null, loc {c.CurrentLocation} -> (162,238)");
            c.CurrentMap = map1;
            c.BindPoint = sz1;
            c.CurrentInstance = null;
            c.CurrentLocation = new Point(162, 238);
        }
    }
    session.Save(true);
    Console.WriteLine("saved.");
}
