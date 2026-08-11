using System.Drawing;
using System.Text;
using System.Text.Json;
using Library;
using Library.SystemModels;
using MirDB;

// DbMigrationTool — read/write the Zircon System.db (MirDB custom binary format).
//
// Usage:
//   DbMigrationTool [--root <dir>] [--backup <dir>] <command> [args...]
//
//   --root <dir>   directory containing System.db (default: Debug/Client/Data)
//   --backup <dir> Session backup dir (default: <root>Backup/)
//
// Commands:
//   (none)         read-only report: all MapInfo + first 10 MapRegion + per-type stats
//   counts         statistics only
//   add-map <fileName> <description> [miniMap]
//   del-map <fileName>
//   edit-map <fileName> <newDescription>
//   set-minimap <fileName> <miniMapFrame>
//   fix-regions <sizeDiffJson> <mapDir> [outputFile]
//   batch-delete-orphans <orphansJson> [mapFilter]
//     delete Zircon-only maps + their MapRegion/MovementInfo/RespawnInfo/NPCInfo/
//     GuardInfo/SafeZoneInfo/MineInfo records (by Index, bottom-up — MirDB's
//     aggregate cascade never fires because DBObject.Delete() aborts on the
//     first get-only computed property). Also nulls any MapInfo reference to a
//     deleted map so the saved file has no dangling indices.
//
//   import-ei <planJson> [outputFile]
//   fix-sabuk <zirconDataJson> [outputFile]
//     restore Sabuk Keep (MapInfo Index=7) to the Zircon-original region set:
//     delete the 65 EI-databased regions + their movements/safezones/NPCs,
//     rebuild the 27 Zircon regions (fresh indices) and their dependents.
//
// NOTE on SessionMode: the original task sketch used SessionMode.Users, but that
// flags every system-data collection read-only (DBCollection ctor), so Save(true)
// would silently skip System.db. SessionMode.System is required for writes.
// Every write command backs up System.db to System.db.bak before saving.

const string DefaultRoot = "/home/tetsuya/development/Zircon/Debug/Client/Data";

Console.OutputEncoding = Encoding.UTF8;

string root = DefaultRoot;
string backup = null;
List<string> posArgs = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--root" when i + 1 < args.Length:
            root = args[++i];
            break;
        case "--backup" when i + 1 < args.Length:
            backup = args[++i];
            break;
        default:
            posArgs.Add(args[i]);
            break;
    }
}

backup ??= Path.Combine(root, "Backup") + Path.DirectorySeparatorChar;

// Session's ctor resolves root against AppDomain.BaseDirectory; normalize
// relative paths against the current working directory instead.
if (!Path.IsPathRooted(root)) root = Path.GetFullPath(root);
if (!Path.IsPathRooted(backup)) backup = Path.GetFullPath(backup);
if (!root.EndsWith(Path.DirectorySeparatorChar)) root += Path.DirectorySeparatorChar;
if (!backup.EndsWith(Path.DirectorySeparatorChar)) backup += Path.DirectorySeparatorChar;

string command = posArgs.Count > 0 ? posArgs[0].ToLowerInvariant() : "report";
string[] cargs = posArgs.Skip(1).ToArray();

Session session = new Session(SessionMode.System, root: root, backup: backup);
session.Initialize(typeof(MapInfo).Assembly);
switch (command)
{
    case "report":
        Report(session);
        break;
    case "counts":
        PrintStats(session);
        break;
    case "dump-monsters":
        DumpMonsters(session);
        break;
    case "dump-all":
        if (cargs.Length < 1) throw new ArgumentException("dump-all <outputJson>");
        DumpAll(session, cargs[0]);
        break;
    case "dump-region-bits":
        if (cargs.Length < 1) throw new ArgumentException("dump-region-bits <mapFileName>");
        DumpRegionBits(session, cargs[0]);
        break;
    case "add-map":
        if (cargs.Length < 2) throw new ArgumentException("add-map <fileName> <description> [miniMap]");
        WriteMap(session, cargs[0], cargs[1], cargs.Length > 2 ? int.Parse(cargs[2]) : 0);
        break;
    case "del-map":
        if (cargs.Length < 1) throw new ArgumentException("del-map <fileName>");
        DeleteMap(session, cargs[0]);
        break;
    case "edit-map":
        if (cargs.Length < 2) throw new ArgumentException("edit-map <fileName> <newDescription>");
        EditMap(session, cargs[0], cargs[1]);
        break;
    case "set-minimap":
        if (cargs.Length < 2) throw new ArgumentException("set-minimap <fileName> <miniMapFrame>");
        SetMiniMap(session, cargs[0], int.Parse(cargs[1]));
        break;
    case "fix-regions":
        if (cargs.Length < 2) throw new ArgumentException("fix-regions <sizeDiffJson> <mapDir> [outputFile]");
        FixRegions(session, cargs[0], cargs[1], cargs.Length > 2 ? cargs[2] : null);
        break;
    case "verify-fix":
        if (cargs.Length < 2) throw new ArgumentException("verify-fix <sizeDiffJson> <mapDir>");
        VerifyFix(session, cargs[0], cargs[1]);
        break;
    case "batch-delete-orphans":
        if (cargs.Length < 1) throw new ArgumentException("batch-delete-orphans <orphansJson> [mapFilter]");
        BatchDeleteOrphans(session, cargs[0], cargs.Length > 1 ? cargs[1] : null);
        break;
    case "import-ei":
        if (cargs.Length < 1) throw new ArgumentException("import-ei <planJson> [outputFile]");
        ImportEI(session, cargs[0], cargs.Length > 1 ? cargs[1] : null);
        break;
    case "import-monsters":
        if (cargs.Length < 1) throw new ArgumentException("import-monsters <monstersJson> [outputFile]");
        ImportMonsters(session, cargs[0], cargs.Length > 1 ? cargs[1] : null);
        break;
    case "del-monster":
        if (cargs.Length < 1) throw new ArgumentException("del-monster <name>");
        DeleteMonster(session, cargs[0]);
        break;
    case "delete-records":
        if (cargs.Length < 2) throw new ArgumentException("delete-records <CollectionName> <indexesJson>");
        DeleteRecords(session, cargs[0], cargs[1]);
        break;
    case "set-safezone-point":
        if (cargs.Length < 4) throw new ArgumentException("set-safezone-point <safeZoneIndex> <mapFileName> <x> <y>");
        SetSafeZonePoint(session, int.Parse(cargs[0]), cargs[1], int.Parse(cargs[2]), int.Parse(cargs[3]));
        break;
    case "trim-safezones":
        TrimSafeZones(session);
        break;
    case "move-respawns":
        if (cargs.Length < 1) throw new ArgumentException("move-respawns <fixesJson>");
        MoveRespawns(session, cargs[0]);
        break;
    case "fix-sabuk":
        if (cargs.Length < 1) throw new ArgumentException("fix-sabuk <zirconDataJson> [outputFile]");
        FixSabuk(session, cargs[0], cargs.Length > 1 ? cargs[1] : null);
        break;
    default:
        throw new ArgumentException($"Unknown command '{command}'");
}

return;

// ---------------- read-only ----------------

void Report(Session s)
{
    var maps = s.GetCollection<MapInfo>().Binding;

    Console.WriteLine("=== All MapInfo ===");
    Console.WriteLine($"{"Idx",-6}{"FileName",-28}{"MiniMap",-8}Description");
    Console.WriteLine(new string('-', 110));
    foreach (var m in maps.OrderBy(x => x.Index))
    {
        Console.WriteLine($"{m.Index,-6}{m.FileName,-28}{m.MiniMap,-8}{m.Description}");
    }

    Console.WriteLine();
    Console.WriteLine("=== First 10 MapRegion ===");
    var regions = s.GetCollection<MapRegion>().Binding;
    foreach (var r in regions.OrderBy(x => x.Index).Take(10))
    {
        string bit = r.BitRegion != null
            ? $"BitRegion[len={r.BitRegion.Length},set={CountSet(r.BitRegion)},first={string.Join(",", FirstSet(r.BitRegion, 5))}]"
            : "BitRegion[null]";
        string pts = r.PointRegion != null
            ? $"PointRegion[count={r.PointRegion.Length},first={string.Join(";", r.PointRegion.Take(3).Select(p => $"({p.X},{p.Y})"))}]"
            : "PointRegion[null]";
        Console.WriteLine($"Region #{r.Index}  Map={r.Map?.FileName ?? "<null>"}  \"{r.Description}\"  Type={r.RegionType}  Size={r.Size}");
        Console.WriteLine($"    {bit}");
        Console.WriteLine($"    {pts}");
    }

    Console.WriteLine();
    PrintStats(s);
}

void PrintStats(Session s)
{
    Console.WriteLine("=== Collection statistics ===");
    var types = new (string Name, Library.MirDB.ADBCollection c)[]
    {
        ("MapInfo", s.GetCollection<MapInfo>()),
        ("MapRegion", s.GetCollection<MapRegion>()),
        ("MovementInfo", s.GetCollection<MovementInfo>()),
        ("RespawnInfo", s.GetCollection<RespawnInfo>()),
        ("NPCInfo", s.GetCollection<NPCInfo>()),
        ("GuardInfo", s.GetCollection<GuardInfo>()),
        ("SafeZoneInfo", s.GetCollection<SafeZoneInfo>()),
        ("MineInfo", s.GetCollection<MineInfo>()),
        ("MonsterInfo", s.GetCollection<MonsterInfo>()),
        ("ItemInfo", s.GetCollection<ItemInfo>()),
    };
    int total = 0;
    foreach (var (name, c) in types)
    {
        Console.WriteLine($"{name,-16}{c.Count,7}");
        total += c.Count;
    }
    Console.WriteLine($"{"TOTAL",-16}{total,7}");
    Console.WriteLine($"SystemDatabaseVersion: {session.SystemDatabaseVersion}");
    Console.WriteLine($"System.db path:        {session.SystemPath}");
}

static int CountSet(System.Collections.BitArray b)
{
    int n = 0;
    foreach (bool bit in b) if (bit) n++;
    return n;
}

static IEnumerable<int> FirstSet(System.Collections.BitArray b, int max)
{
    int found = 0;
    for (int i = 0; i < b.Length && found < max; i++)
        if (b[i]) { yield return i; found++; }
}

// ---------------- dump helpers ----------------

void DumpMonsters(Session s)
{
    var monsters = s.GetCollection<MonsterInfo>().Binding;
    Console.WriteLine($"{"Index",-7}{"MonsterName",-40}{"AI",-5}{"Level",-6}{"IsBoss",-7}Image");
    Console.WriteLine(new string('-', 100));
    foreach (var m in monsters.OrderBy(x => x.Index))
    {
        Console.WriteLine($"{m.Index,-7}{m.MonsterName,-40}{m.AI,-5}{m.Level,-6}{m.IsBoss,-7}{m.Image}");
    }
    Console.WriteLine($"Total: {monsters.Count}");
}

// Dump every SystemModels collection to JSON (wiki_all.json / stores / images feed).
//
// Serializes each DBObject's public persisted properties:
//   - scalar/enum/string/int/float/bool  -> as-is
//   - DBObject refs (MapInfo etc.)       -> its Index (int), with a <Type>Name
//     string beside it when the target has a Name-like property
//   - DBBindingList<T>                    -> [{Index, <Type>Name}] (ints)
//   - Point[]/BitArray                    -> explicit int arrays
//   - everything else                    -> null (never JSON-circular)
void DumpAll(Session s, string outPath)
{
    var asm = typeof(MapInfo).Assembly;
    var types = asm.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(DBObject)) && !t.IsAbstract)
        .OrderBy(t => t.Name)
        .ToList();

    var result = new Dictionary<string, object>();
    foreach (var type in types)
    {
        var coll = s.GetCollection(type);
        var bindingProp = coll.GetType().GetField("Binding");
        if (bindingProp == null) continue;
        var list = (System.Collections.IList)bindingProp.GetValue(coll);
        var rows = new List<Dictionary<string, object>>();
        foreach (var ob in list)
        {
            var row = new Dictionary<string, object>();
            foreach (var p in type.GetProperties())
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                // skip runtime-only get-only computed props (already serialized under
                // the persisted field the wiki reads)
                if (p.GetMethod.IsStatic) continue;
                object v;
                try { v = p.GetValue(ob); }
                catch { continue; }
                if (v == null) { row[p.Name] = null; continue; }
                var pt = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                if (pt.IsEnum)
                {
                    row[p.Name] = v.ToString();
                }
                else if (pt == typeof(bool) || pt == typeof(int) || pt == typeof(long)
                    || pt == typeof(short) || pt == typeof(byte) || pt == typeof(float) || pt == typeof(double))
                {
                    row[p.Name] = v;
                }
                else if (pt == typeof(string))
                {
                    row[p.Name] = (string)v;
                }
                else if (pt == typeof(Point))
                {
                    var po = (Point)v;
                    row[p.Name] = new[] { po.X, po.Y };
                }
                else if (pt == typeof(Point[]))
                {
                    row[p.Name] = ((Point[])v).Select(q => new[] { q.X, q.Y }).ToList();
                }
                else if (pt == typeof(System.Collections.BitArray))
                {
                    var ba = (System.Collections.BitArray)v;
                    var bytes = new byte[(ba.Length + 7) / 8];
                    ba.CopyTo(bytes, 0);
                    row[p.Name] = Convert.ToHexString(bytes);
                }
                else if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(DBBindingList<>))
                {
                    var items = new List<object>();
                    foreach (var item in (System.Collections.IEnumerable)v)
                    {
                        var ip = item.GetType().GetProperty("Index");
                        var nm = item.GetType().GetProperty("Name")
                                 ?? item.GetType().GetProperty("Description")
                                 ?? item.GetType().GetProperty("MonsterName")
                                 ?? item.GetType().GetProperty("ItemName");
                        items.Add(new Dictionary<string, object>
                        {
                            ["Index"] = ip?.GetValue(item),
                            [item.GetType().Name] = nm?.GetValue(item)
                        });
                    }
                    row[p.Name] = items;
                }
                else if (pt.IsSubclassOf(typeof(DBObject)))
                {
                    var index = pt.GetProperty("Index")?.GetValue(v);
                    var nm = pt.GetProperty("Name")?.GetValue(v)
                             ?? pt.GetProperty("Description")?.GetValue(v)
                             ?? pt.GetProperty("MonsterName")?.GetValue(v)
                             ?? pt.GetProperty("ItemName")?.GetValue(v)
                             ?? pt.GetProperty("FileName")?.GetValue(v);
                    var d = new Dictionary<string, object> { ["Index"] = index };
                    if (nm != null) d[pt.Name] = nm;
                    row[p.Name] = d;
                }
                else row[p.Name] = null;
            }
            rows.Add(row);
        }
        result[type.Name] = new Dictionary<string, object> { ["count"] = rows.Count, ["rows"] = rows };
    }
    var json = JsonSerializer.Serialize(result,
        new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    File.WriteAllText(outPath, json);
    Console.WriteLine($"dump-all: {result.Count} collections, {result.Values.Sum(x => ((Dictionary<string, object>)x)["count"] is int c ? c : 0)} rows -> {outPath}");
}

void DumpRegionBits(Session s, string mapFileName)
{
    var map = s.GetCollection<MapInfo>().Binding
        .FirstOrDefault(m => string.Equals(m.FileName, mapFileName, StringComparison.OrdinalIgnoreCase));
    if (map == null) throw new InvalidOperationException($"MapInfo '{mapFileName}' not found");

    var regions = s.GetCollection<MapRegion>().Binding
        .Where(r => r.Map == map)
        .OrderBy(r => r.Index)
        .ToList();

    Console.WriteLine($"Map {map.FileName} \"{map.Description}\": {regions.Count} regions");
    foreach (var r in regions)
    {
        Console.WriteLine($"Region #{r.Index} \"{r.Description}\" Type={r.RegionType} Size={r.Size}");
        if (r.BitRegion != null)
        {
            byte[] bytes = new byte[(r.BitRegion.Length + 7) / 8];
            r.BitRegion.CopyTo(bytes, 0);
            Console.WriteLine($"  BitRegion len={r.BitRegion.Length}");
            Console.WriteLine($"  HEX: {Convert.ToHexString(bytes)}");
        }
        else if (r.PointRegion != null)
        {
            Console.WriteLine($"  PointRegion count={r.PointRegion.Length}: {string.Join(" ", r.PointRegion.Select(p => $"({p.X},{p.Y})"))}");
        }
        else
        {
            Console.WriteLine("  (empty region)");
        }
    }
}

// ---------------- write ----------------

void BackupDb(Session s)
{
    // Explicit backup per assignment: cp System.db System.db.bak
    if (!File.Exists(s.SystemPath)) throw new FileNotFoundException("System.db not found", s.SystemPath);
    string bak = s.SystemPath + ".bak";
    File.Copy(s.SystemPath, bak, overwrite: true);
    Console.WriteLine($"Backup: {bak}");
}

// Batch-delete Zircon-only maps and every associated record.
//
// MirDB's DBObject.Delete() reflection loop aborts on the first get-only
// computed property (MapInfo.PlayerDescription/ServerDescription, MapRegion.
// ServerDescription, NPCInfo.RegionName, RespawnInfo.RegionName, ...) BEFORE it
// reaches the aggregate DBBindingList properties (MapInfo.Guards/Regions/Mining,
// MapRegion.NPCs/Respawns/...). So the MapInfo -> MapRegion -> Respawn cascade
// NEVER fires in practice: del-map leaves all dependent records behind as
// orphans. This command therefore deletes bottom-up by explicit index, which is
// deterministic and does not rely on reflection order.
//
// Per orphan map (from the orphans JSON):
//   1-6. dependents by Index: MovementInfo, RespawnInfo, SafeZoneInfo,
//        GuardInfo, MineInfo, NPCInfo (NPC dialog pages are NPCPage records
//        referenced via EntryPage and are left in place: they hold no map
//        reference and are outside the delete scope)
//   7.   MapRegion: detach QuestTasks (quests are out of scope — ImportEI
//        re-imports the EI quest set; detaching keeps the records loadable),
//        then delete the region
//   8.   MapInfo
// Finally, records that still reference a deleted map (CastleInfo.Map,
// QuestInfo.Map, NPCInfo.MapParameter1, MapInfo.ReconnectMap, event types, ...)
// have that reference nulled so the saved file contains no dangling indices.
//
// Usage: batch-delete-orphans <orphansJson> [mapFilter]
void BatchDeleteOrphans(Session s, string jsonPath, string mapFilter)
{
    BackupDb(s);

    if (!File.Exists(jsonPath)) throw new FileNotFoundException("orphan maps json not found", jsonPath);

    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
    JsonElement maps = doc.RootElement.GetProperty("orphan_maps");

    var errors = new List<string>();
    int mapsDeleted = 0, regionsDeleted = 0, respawnsDeleted = 0, npcsDeleted = 0,
        guardsDeleted = 0, movementsDeleted = 0, safezonesDeleted = 0, minesDeleted = 0,
        questsDetached = 0;
    var deletedMapIndices = new HashSet<int>();

    foreach (JsonElement entry in maps.EnumerateArray())
    {
        string fileName = entry.GetProperty("db_filename").GetString();
        if (mapFilter != null && !string.Equals(fileName, mapFilter, StringComparison.OrdinalIgnoreCase)) continue;

        var map = s.GetCollection<MapInfo>().Binding
            .FirstOrDefault(x => string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            errors.Add($"MapInfo '{fileName}' not found — skipped");
            continue;
        }
        string ctx = $"map {fileName}";

        // 1-5. leaf dependents by Index (case-insensitive matching not needed:
        //      indices come straight from the DB dump)
        movementsDeleted += DeleteByIndex<MovementInfo>(s, ReadIndices(entry, "movement_indices"), errors, ctx);
        respawnsDeleted += DeleteByIndex<RespawnInfo>(s, ReadIndices(entry, "respawn_indices"), errors, ctx);
        safezonesDeleted += DeleteByIndex<SafeZoneInfo>(s, ReadIndices(entry, "safezone_indices"), errors, ctx);
        guardsDeleted += DeleteByIndex<GuardInfo>(s, ReadIndices(entry, "guard_indices"), errors, ctx);
        minesDeleted += DeleteByIndex<MineInfo>(s, ReadIndices(entry, "mine_indices"), errors, ctx);

        // 6. NPCInfo by index (its aggregate child list Requirements is empty
        //    in this DB; NPC dialog pages are NPCPage records referenced via
        //    EntryPage and are intentionally left in place — they hold no map
        //    reference and are outside the delete scope)
        npcsDeleted += DeleteByIndex<NPCInfo>(s, ReadIndices(entry, "npc_indices"), errors, ctx);

        // 7. MapRegion: detach QuestTasks (keep quest records), then delete
        var regionIds = ReadIndices(entry, "region_indices");
        foreach (var region in s.GetCollection<MapRegion>().Binding.Where(x => regionIds.Contains(x.Index)).ToList())
        {
            foreach (var qt in region.QuestTasks.ToList())
            {
                try { qt.RegionParameter = null; questsDetached++; }
                catch (Exception e) { errors.Add($"{ctx}: detach QuestTask #{qt.Index} failed: {e.GetType().Name}: {e.Message}"); }
            }
            try { region.Delete(); regionsDeleted++; }
            catch (ArgumentException) { regionsDeleted++; }
            catch (Exception e) { errors.Add($"{ctx}: Region #{region.Index} delete failed: {e.GetType().Name}: {e.Message}"); }
        }

        // 8. MapInfo
        try { map.Delete(); mapsDeleted++; deletedMapIndices.Add(map.Index); }
        catch (ArgumentException) { mapsDeleted++; deletedMapIndices.Add(map.Index); }
        catch (Exception e) { errors.Add($"{ctx}: MapInfo delete failed: {e.GetType().Name}: {e.Message}"); }
    }

    // 9. Null out references to the deleted maps so the saved file has no
    //    dangling indices (MirDB would null them at next load anyway).
    int refsDetached = DetachMapReferences(s, deletedMapIndices, errors);

    s.Save(true);

    Console.WriteLine("=== batch-delete-orphans result ===");
    Console.WriteLine($"maps deleted        : {mapsDeleted}");
    Console.WriteLine($"regions deleted     : {regionsDeleted}");
    Console.WriteLine($"respawns deleted    : {respawnsDeleted}");
    Console.WriteLine($"npcs deleted        : {npcsDeleted}");
    Console.WriteLine($"guards deleted      : {guardsDeleted}");
    Console.WriteLine($"movements deleted   : {movementsDeleted}");
    Console.WriteLine($"safezones deleted   : {safezonesDeleted}");
    Console.WriteLine($"mines deleted       : {minesDeleted}");
    Console.WriteLine($"quest tasks detached: {questsDetached}");
    Console.WriteLine($"map refs detached   : {refsDetached}");
    Console.WriteLine($"errors              : {errors.Count}");
    foreach (var e in errors) Console.WriteLine($"  ! {e}");
    if (mapFilter != null) Console.WriteLine($"(filtered to map '{mapFilter}')");

    try
    {
        File.WriteAllText("/tmp/investigate/delete_result.txt",
            $"batch-delete-orphans  {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"json: {jsonPath}  filter: {mapFilter ?? "(all)"}\n" +
            $"maps deleted: {mapsDeleted}  regions: {regionsDeleted}  respawns: {respawnsDeleted}  " +
            $"npcs: {npcsDeleted}  guards: {guardsDeleted}  " +
            $"movements: {movementsDeleted}  safezones: {safezonesDeleted}  mines: {minesDeleted}  " +
            $"quest tasks detached: {questsDetached}  map refs detached: {refsDetached}  errors: {errors.Count}\n" +
            (errors.Count > 0 ? string.Join("\n", errors) + "\n" : ""));
        Console.WriteLine("Wrote /tmp/investigate/delete_result.txt");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Could not write /tmp/investigate/delete_result.txt: {e.Message}");
    }
}

void WriteMap(Session s, string fileName, string description, int miniMap)
{
    BackupDb(s);

    var collection = s.GetCollection<MapInfo>();
    if (collection.Binding.Any(m => string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException($"MapInfo '{fileName}' already exists");

    var map = collection.CreateNewObject();
    map.FileName = fileName;
    map.Description = description;
    map.MiniMap = miniMap;

    s.Save(true);
    Console.WriteLine($"Added MapInfo #{map.Index}: {fileName} - {description} (MiniMap={miniMap})");
}

void DeleteMap(Session s, string fileName)
{
    BackupDb(s);

    var collection = s.GetCollection<MapInfo>();
    var map = collection.Binding.FirstOrDefault(m => string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase));
    if (map == null) throw new InvalidOperationException($"MapInfo '{fileName}' not found");

    // LibraryCore Session.Delete() reflection loop (Session.cs ~577) calls
    // SetValue(ob, null) on MapInfo's get-only computed property "Dungeon" and
    // throws — but only AFTER the record was already removed from the
    // collection and CollectionChanged was set. Catch and save; dependents
    // referencing the deleted map resolve to null on next load.
    try { map.Delete(); }
    catch (ArgumentException) { }

    s.Save(true);
    Console.WriteLine($"Deleted MapInfo #{map.Index}: {fileName}");
}

void EditMap(Session s, string fileName, string newDescription)
{
    BackupDb(s);

    var collection = s.GetCollection<MapInfo>();
    var map = collection.Binding.FirstOrDefault(m => string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase));
    if (map == null) throw new InvalidOperationException($"MapInfo '{fileName}' not found");

    map.Description = newDescription;
    s.Save(true);
    Console.WriteLine($"Updated MapInfo #{map.Index}: {fileName} -> \"{newDescription}\"");
}

void SetMiniMap(Session s, string fileName, int miniMapFrame)
{
    BackupDb(s);

    var collection = s.GetCollection<MapInfo>();
    var map = collection.Binding.FirstOrDefault(m => string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase));
    if (map == null) throw new InvalidOperationException($"MapInfo '{fileName}' not found");
    if (miniMapFrame < 0) throw new ArgumentOutOfRangeException(nameof(miniMapFrame));

    int oldFrame = map.MiniMap;
    map.MiniMap = miniMapFrame;
    s.Save(true);
    Console.WriteLine($"Updated MapInfo #{map.Index}: {fileName} MiniMap {oldFrame} -> {miniMapFrame}");
}

// ---------------- fix-regions ----------------

// Fix out-of-bounds MapRegion coordinates after map size changes.
//
// MirDB stores a region either as:
//   BitRegion  : BitArray over the whole map grid, bit i = (i % width, i / width)
//                with the width of the map at edit time (runtime decode uses the
//                CURRENT map width -> coordinates scramble after a resize).
//   PointRegion: Point[] of absolute coordinates.
//
// For every map listed in <sizeDiffJson> (from /tmp/investigate/size_diff.json):
//   - BitRegion regions are decoded with the OLD width into absolute points,
//     filtered to the NEW map bounds, and re-stored as PointRegion.
//   - PointRegion regions have out-of-bounds points removed.
//   - A region left with zero points is deleted (its dependents resolve to null,
//     same cascade as del-map).
//
// New map dimensions are read from the current .map file header (Width/Height =
// LE Int16 at offsets 22/24, matching ServerLibrary/Models/Map.cs). The old
// dimensions come from size_diff.json, which was generated from the pre-migration
// backup; D202 is special-cased because the backup D202.map was already
// overwritten by the EI copy at backup time (300x300), while the DB BitRegion
// was encoded against the true Zircon original D202.map.bak-zircon (350x350,
// verified: bit capacity 122504 == ceil(350*350/8)*8).
//
// Usage: fix-regions <sizeDiffJson> <mapDir> [outputFile]
void FixRegions(Session s, string sizeDiffJsonPath, string mapDir, string outputFile = null)
{
    BackupDb(s);

    using var log = new StreamWriter(outputFile ?? Path.Combine(Path.GetTempPath(), "fix_regions_result.txt"), append: false);
    void Emit(string line)
    {
        Console.WriteLine(line);
        log.WriteLine(line);
    }

    if (!File.Exists(sizeDiffJsonPath)) throw new FileNotFoundException("sizeDiffJson not found", sizeDiffJsonPath);

    // ---- parse size_diff.json ----
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(sizeDiffJsonPath));
    JsonElement changed = doc.RootElement.GetProperty("changed");

    var maps = s.GetCollection<MapInfo>().Binding;
    var allRegions = s.GetCollection<MapRegion>().Binding;

    int mapsProcessed = 0, regionsConverted = 0, regionsDeleted = 0, pointsDropped = 0;
    var totals = new Dictionary<string, int>(); // per-map stats

    foreach (JsonElement entry in changed.EnumerateArray())
    {
        string fileWithExt = entry.GetProperty("map").GetString();          // e.g. "D502.map"
        string dbName = fileWithExt.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            ? fileWithExt[..^4]
            : fileWithExt;                                                   // e.g. "D502"
        string currentFile = entry.GetProperty("current_file").GetString();  // EI copy on disk, e.g. "d502.map"

        // old dims from size_diff.json; D202 override (see comment above)
        int oldWidth = entry.GetProperty("old_width").GetInt32();
        int oldHeight = entry.GetProperty("old_height").GetInt32();
        if (string.Equals(dbName, "D202", StringComparison.OrdinalIgnoreCase))
        {
            oldWidth = 350;
            oldHeight = 350;
        }

        var map = maps.FirstOrDefault(m => string.Equals(m.FileName, dbName, StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            Emit($"SKIP  {dbName}: MapInfo not found in DB");
            continue;
        }

        // ---- new dims from the current .map file header ----
        string mapPath = Path.Combine(mapDir, currentFile);
        int newWidth, newHeight;
        if (File.Exists(mapPath))
        {
            byte[] hdr = new byte[26];
            using (var fs = File.OpenRead(mapPath))
            {
                int read = fs.Read(hdr, 0, hdr.Length);
                if (read < 26) throw new InvalidOperationException($"{currentFile}: header too short ({read} bytes)");
            }
            newWidth = hdr[23] << 8 | hdr[22];
            newHeight = hdr[25] << 8 | hdr[24];
        }
        else
        {
            // fall back to size_diff.json new dims
            newWidth = entry.GetProperty("new_width").GetInt32();
            newHeight = entry.GetProperty("new_height").GetInt32();
            Emit($"WARN  {dbName}: {currentFile} not found in {mapDir}, using size_diff.json dims {newWidth}x{newHeight}");
        }
        if (newWidth <= 0 || newHeight <= 0) throw new InvalidOperationException($"{currentFile}: bad header dims {newWidth}x{newHeight}");

        var regions = allRegions.Where(r => r.Map == map).OrderBy(r => r.Index).ToList();
        if (regions.Count == 0)
        {
            Emit($"SKIP  {dbName} ({currentFile}): no MapRegion records");
            continue;
        }

        int converted = 0, trimmed = 0, deleted = 0, droppedBits = 0;
        foreach (MapRegion region in regions)
        {
            if (region.BitRegion != null)
            {
                // decode with OLD width -> absolute points
                var kept = new List<Point>();
                for (int i = 0; i < region.BitRegion.Length; i++)
                {
                    if (!region.BitRegion[i]) continue;
                    int x = i % oldWidth;
                    int y = i / oldWidth;
                    if (x < newWidth && y < newHeight)
                        kept.Add(new Point(x, y));
                    else
                        droppedBits++;
                }

                if (kept.Count == 0)
                {
                    region.Delete();
                    deleted++;
                }
                else
                {
                    region.PointRegion = kept.ToArray();
                    region.BitRegion = null;
                    region.Size = kept.Count;
                    converted++;
                }
            }
            else if (region.PointRegion != null)
            {
                int before = region.PointRegion.Length;
                Point[] kept = region.PointRegion.Where(p => p.X < newWidth && p.Y < newHeight).ToArray();
                if (kept.Length != before)
                {
                    trimmed += before - kept.Length;
                    if (kept.Length == 0)
                    {
                        region.Delete();
                        deleted++;
                    }
                    else
                    {
                        region.PointRegion = kept;
                        region.Size = kept.Length;
                    }
                }
            }
        }

        mapsProcessed++;
        regionsConverted += converted;
        regionsDeleted += deleted;
        pointsDropped += droppedBits + trimmed;
        totals[dbName] = (converted << 16) | (trimmed << 8) | deleted;

        Emit($"MAP   {dbName,-8} {currentFile,-14} {oldWidth}x{oldHeight} -> {newWidth}x{newHeight}  regions={regions.Count}  bitToPoint={converted}  bitPointsDropped={droppedBits}  pointTrimmed={trimmed}  deleted={deleted}");
    }

    Emit("");
    Emit($"TOTAL maps processed: {mapsProcessed}/{changed.GetArrayLength()}");
    Emit($"TOTAL BitRegion->PointRegion: {regionsConverted}");
    Emit($"TOTAL regions deleted (empty after trim): {regionsDeleted}");
    Emit($"TOTAL points dropped (out of new bounds): {pointsDropped}");

    s.Save(true);
    Emit($"Saved. System.db -> {s.SystemPath}");
}

// Read-only verification for fix-regions: for every map in size_diff.json,
// assert no BitRegion remains and every PointRegion point is inside the new
// map bounds (read from the current .map header).
void VerifyFix(Session s, string sizeDiffJsonPath, string mapDir)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(sizeDiffJsonPath));
    JsonElement changed = doc.RootElement.GetProperty("changed");

    var maps = s.GetCollection<MapInfo>().Binding;
    var allRegions = s.GetCollection<MapRegion>().Binding;

    int checkedMaps = 0, checkedRegions = 0, bitRemnants = 0, oobPoints = 0, errors = 0;

    foreach (JsonElement entry in changed.EnumerateArray())
    {
        string fileWithExt = entry.GetProperty("map").GetString();
        string dbName = fileWithExt.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            ? fileWithExt[..^4]
            : fileWithExt;
        string currentFile = entry.GetProperty("current_file").GetString();

        int newWidth, newHeight;
        string mapPath = Path.Combine(mapDir, currentFile);
        if (File.Exists(mapPath))
        {
            byte[] hdr = new byte[26];
            using (var fs = File.OpenRead(mapPath))
            {
                int read = fs.Read(hdr, 0, hdr.Length);
                if (read < 26) throw new InvalidOperationException($"{currentFile}: header too short");
            }
            newWidth = hdr[23] << 8 | hdr[22];
            newHeight = hdr[25] << 8 | hdr[24];
        }
        else
        {
            newWidth = entry.GetProperty("new_width").GetInt32();
            newHeight = entry.GetProperty("new_height").GetInt32();
        }

        var map = maps.FirstOrDefault(m => string.Equals(m.FileName, dbName, StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            Console.WriteLine($"SKIP  {dbName}: MapInfo not found (deleted as orphan)");
            continue;
        }

        var regions = allRegions.Where(r => r.Map == map).OrderBy(r => r.Index).ToList();
        checkedMaps++;

        foreach (MapRegion region in regions)
        {
            checkedRegions++;
            bool bad = false;
            if (region.BitRegion != null)
            {
                bitRemnants++;
                Console.WriteLine($"ERROR {dbName} #{region.Index} \"{region.Description}\": BitRegion still present");
                bad = true;
            }
            if (region.PointRegion != null)
            {
                foreach (Point p in region.PointRegion)
                {
                    if (p.X < 0 || p.Y < 0 || p.X >= newWidth || p.Y >= newHeight)
                    {
                        oobPoints++;
                        if (oobPoints <= 20)
                            Console.WriteLine($"ERROR {dbName} #{region.Index} \"{region.Description}\": OOB point ({p.X},{p.Y}) vs {newWidth}x{newHeight}");
                        bad = true;
                    }
                }
            }
            if (bad) errors++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Verify: {checkedMaps} maps, {checkedRegions} regions checked, {bitRemnants} BitRegion remnants, {oobPoints} OOB points, {errors} bad regions");
}

// ---------------- batch-delete-orphans helpers ----------------

static HashSet<int> ReadIndices(JsonElement entry, string prop)
{
    var set = new HashSet<int>();
    if (entry.TryGetProperty(prop, out JsonElement arr))
    {
        foreach (JsonElement e in arr.EnumerateArray())
            if (e.TryGetInt32(out int v)) set.Add(v);
    }
    return set;
}

// Delete every record of type T whose Index is in `indices`. MirDB removes the
// record from its collection before the reflection loop hits a get-only
// computed property, so ArgumentException still means "deleted from the DB".
static int DeleteByIndex<T>(Session s, HashSet<int> indices, List<string> errors, string context)
    where T : DBObject, new()
{
    int n = 0;
    foreach (T ob in s.GetCollection<T>().Binding.Where(x => indices.Contains(x.Index)).ToList())
    {
        try { ob.Delete(); n++; }
        catch (ArgumentException) { n++; }
        catch (Exception e) { errors.Add($"{context}: {typeof(T).Name} #{ob.Index} delete failed: {e.GetType().Name}: {e.Message}"); }
    }
    return n;
}

// Null out every MapInfo-typed reference that points to a deleted map, so the
// saved file contains no dangling indices (MirDB would do the same at load).
static int DetachMapReferences(Session s, HashSet<int> deletedMapIndices, List<string> errors)
{
    int n = 0;

    n += Detach(s, s.GetCollection<CastleInfo>().Binding, x => x.Map, (x, v) => x.Map = v, deletedMapIndices, errors, "CastleInfo.Map");
    n += Detach(s, s.GetCollection<DungeonMapInfo>().Binding, x => x.Map, (x, v) => x.Map = v, deletedMapIndices, errors, "DungeonMapInfo.Map");
    n += Detach(s, s.GetCollection<QuestTaskMonsterDetails>().Binding, x => x.Map, (x, v) => x.Map = v, deletedMapIndices, errors, "QuestTaskMonsterDetails.Map");
    n += Detach(s, s.GetCollection<NPCAction>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "NPCAction.MapParameter1");
    n += Detach(s, s.GetCollection<PlayerEventTrigger>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "PlayerEventTrigger.MapParameter1");
    n += Detach(s, s.GetCollection<MonsterEventTrigger>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "MonsterEventTrigger.MapParameter1");
    n += Detach(s, s.GetCollection<WorldEventAction>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "WorldEventAction.MapParameter1");
    n += Detach(s, s.GetCollection<PlayerEventAction>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "PlayerEventAction.MapParameter1");
    n += Detach(s, s.GetCollection<MonsterEventAction>().Binding, x => x.MapParameter1, (x, v) => x.MapParameter1 = v, deletedMapIndices, errors, "MonsterEventAction.MapParameter1");
    n += Detach(s, s.GetCollection<MapInfo>().Binding, x => x.ReconnectMap, (x, v) => x.ReconnectMap = v, deletedMapIndices, errors, "MapInfo.ReconnectMap");
    n += Detach(s, s.GetCollection<MapInfoStat>().Binding, x => x.Map, (x, v) => x.Map = v, deletedMapIndices, errors, "MapInfoStat.Map");

    return n;
}

static int Detach<T>(Session s, IList<T> binding, Func<T, MapInfo> get, Action<T, MapInfo> set,
    HashSet<int> deletedMapIndices, List<string> errors, string label) where T : DBObject
{
    int n = 0;
    foreach (T ob in binding.Where(x => get(x) != null && deletedMapIndices.Contains(get(x).Index)).ToList())
    {
        try { set(ob, null); n++; }
        catch (Exception e) { errors.Add($"{label} detach failed on #{ob.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    return n;
}

// ---------------- import-ei ----------------

// Import the EI (英雄杀) configuration for the 485 EI-only maps into System.db.
//
// The <planJson> (built offline by build_import_plan.py) contains only records
// that passed validation:
//   - map files exist (case-insensitive) and coords are inside the map bounds
//   - respawn monsters resolved 中->英 via monster_name_map and exist in the DB
//   - all records are scoped to the 485 EI-only maps (movements need >=1
//     endpoint on a new map)
//
// For every map we create a MapInfo (FileName = actual .map name, Description =
// Chinese name from 英雄杀 MapInfo.txt / EI Mapinfo.txt, MiniMap converted to
// the deployed 287-frame MiniMap.Zl layout). Every data record gets its own
// single-point MapRegion (absolute coords as PointRegion) and the associated
// NPCInfo / RespawnInfo / SafeZoneInfo / MovementInfo / GuardInfo record.
void ImportEI(Session s, string planJsonPath, string outputFile = null)
{
    BackupDb(s);

    using var log = new StreamWriter(outputFile ?? Path.Combine(Path.GetTempPath(), "import_ei_result.txt"), append: false);
    void Emit(string line)
    {
        Console.WriteLine(line);
        log.WriteLine(line);
    }

    if (!File.Exists(planJsonPath)) throw new FileNotFoundException("planJson not found", planJsonPath);

    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(planJsonPath));
    JsonElement root = doc.RootElement;

    var mapCol = s.GetCollection<MapInfo>();
    var regionCol = s.GetCollection<MapRegion>();
    var npcCol = s.GetCollection<NPCInfo>();
    var guardCol = s.GetCollection<GuardInfo>();
    var szCol = s.GetCollection<SafeZoneInfo>();
    var respCol = s.GetCollection<RespawnInfo>();
    var movCol = s.GetCollection<MovementInfo>();
    var monsterCol = s.GetCollection<MonsterInfo>();

    // existing maps by FileName (case-insensitive)
    var existingMaps = new Dictionary<string, MapInfo>(StringComparer.OrdinalIgnoreCase);
    foreach (var m in mapCol.Binding) existingMaps[m.FileName] = m;

    // DB monsters by name (case-insensitive)
    var dbMonsters = new Dictionary<string, MonsterInfo>(StringComparer.OrdinalIgnoreCase);
    foreach (var mo in monsterCol.Binding) dbMonsters[mo.MonsterName] = mo;

    int mapsAdded = 0, mapsSkipped = 0;
    var mapByPlan = new Dictionary<string, MapInfo>(StringComparer.OrdinalIgnoreCase);

    // ---- 1. MapInfo records for the 485 new maps ----
    foreach (JsonElement m in root.GetProperty("maps").EnumerateArray())
    {
        string fileName = m.GetProperty("fileName").GetString();
        if (existingMaps.ContainsKey(fileName))
        {
            mapsSkipped++;
            mapByPlan[fileName] = existingMaps[fileName];
            continue;
        }
        var map = mapCol.CreateNewObject();
        map.FileName = fileName;
        map.Description = m.GetProperty("description").GetString() ?? fileName;
        map.MiniMap = m.TryGetProperty("miniMap", out var mm) ? mm.GetInt32() : 0;
        mapsAdded++;
        existingMaps[fileName] = map;
        mapByPlan[fileName] = map;
    }

    // ---- 2. helper: create a single-point region ----
    static MapRegion MakeRegion(DBCollection<MapRegion> col, MapInfo map, string desc, RegionType type, int x, int y)
    {
        var r = col.CreateNewObject();
        r.Map = map;
        r.Description = desc;
        r.RegionType = type;
        r.PointRegion = new[] { new Point(x, y) };
        r.Size = 1;
        return r;
    }

    int npcsAdded = 0, guardsAdded = 0, szAdded = 0, respAdded = 0, movAdded = 0;
    var skipped = new List<string>();

    // ---- 3. SafeZones ----
    foreach (JsonElement e in root.GetProperty("safezones").EnumerateArray())
    {
        string mapName = e.GetProperty("map").GetString();
        if (!mapByPlan.TryGetValue(mapName, out var map)) { skipped.Add($"SafeZone map {mapName} missing"); continue; }
        var region = MakeRegion(regionCol, map, "Safe Zone", RegionType.Area, e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32());
        var sz = szCol.CreateNewObject();
        sz.Region = region;
        sz.BindRegion = region;
        szAdded++;
    }

    // ---- 4. Guards ----
    foreach (JsonElement e in root.GetProperty("guards").EnumerateArray())
    {
        string mapName = e.GetProperty("map").GetString();
        if (!mapByPlan.TryGetValue(mapName, out var map)) { skipped.Add($"Guard map {mapName} missing"); continue; }
        string monName = e.GetProperty("monster").GetString();
        if (!dbMonsters.TryGetValue(monName, out var mon)) { skipped.Add($"Guard monster {monName} missing"); continue; }
        var g = guardCol.CreateNewObject();
        g.Map = map;
        g.Monster = mon;
        g.X = e.GetProperty("x").GetInt32();
        g.Y = e.GetProperty("y").GetInt32();
        g.Direction = (MirDirection)e.GetProperty("dir").GetInt32();
        guardsAdded++;
    }

    // ---- 5. NPCs ----
    foreach (JsonElement e in root.GetProperty("npcs").EnumerateArray())
    {
        string mapName = e.GetProperty("map").GetString();
        if (!mapByPlan.TryGetValue(mapName, out var map)) { skipped.Add($"NPC map {mapName} missing"); continue; }
        string npcName = e.GetProperty("name").GetString();
        string desc = e.TryGetProperty("desc", out var d) && d.GetString() is string ds && ds.Length > 0 ? ds : npcName;
        var region = MakeRegion(regionCol, map, desc, RegionType.Npc, e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32());
        var npc = npcCol.CreateNewObject();
        npc.Region = region;
        npc.NPCName = npcName;
        npc.Image = e.GetProperty("image").GetInt32();
        npcsAdded++;
    }

    // ---- 6. Respawns ----
    foreach (JsonElement e in root.GetProperty("respawns").EnumerateArray())
    {
        string mapName = e.GetProperty("map").GetString();
        if (!mapByPlan.TryGetValue(mapName, out var map)) { skipped.Add($"Respawn map {mapName} missing"); continue; }
        string monName = e.GetProperty("monster").GetString();
        if (!dbMonsters.TryGetValue(monName, out var mon)) { skipped.Add($"Respawn monster {monName} missing"); continue; }
        var region = MakeRegion(regionCol, map, $"{mon.MonsterName} Spawn", RegionType.Spawn, e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32());
        var resp = respCol.CreateNewObject();
        resp.Monster = mon;
        resp.Region = region;
        resp.Count = e.GetProperty("count").GetInt32();
        resp.Delay = 1;
        resp.EasterEventChance = 50; // match existing DB convention
        respAdded++;
    }

    // ---- 7. Movements (SourceRegion + DestinationRegion) ----
    // Endpoints may be on existing OR new maps; look up in the full map set.
    foreach (JsonElement e in root.GetProperty("movements").EnumerateArray())
    {
        string srcName = e.GetProperty("srcMap").GetString();
        string dstName = e.GetProperty("dstMap").GetString();
        if (!existingMaps.TryGetValue(srcName, out var srcMap)) { skipped.Add($"Movement src map {srcName} missing"); continue; }
        if (!existingMaps.TryGetValue(dstName, out var dstMap)) { skipped.Add($"Movement dst map {dstName} missing"); continue; }
        var src = MakeRegion(regionCol, srcMap, "Teleport Source", RegionType.Connection, e.GetProperty("srcX").GetInt32(), e.GetProperty("srcY").GetInt32());
        var dst = MakeRegion(regionCol, dstMap, "Teleport Destination", RegionType.Connection, e.GetProperty("dstX").GetInt32(), e.GetProperty("dstY").GetInt32());
        var mov = movCol.CreateNewObject();
        mov.SourceRegion = src;
        mov.DestinationRegion = dst;
        movAdded++;
    }

    // ---- 8. Save ----
    s.Save(true);

    Emit("=== import-ei results ===");
    Emit($"MapInfo added:      {mapsAdded}   (skipped already-present: {mapsSkipped})");
    Emit($"MapRegion added:    {szAdded + npcsAdded + respAdded + movAdded * 2}");
    Emit($"SafeZoneInfo added: {szAdded}");
    Emit($"GuardInfo added:    {guardsAdded}");
    Emit($"NPCInfo added:      {npcsAdded}");
    Emit($"RespawnInfo added:  {respAdded}");
    Emit($"MovementInfo added: {movAdded}");
    Emit($"Skip notes:         {skipped.Count}");
    foreach (string line in skipped.Take(50)) Emit($"  SKIP {line}");
    Emit($"Saved. System.db -> {s.SystemPath}");

    // ---- 9. verification ----
    Emit("");
    Emit("=== post-import counts ===");
    Emit($"MapInfo     {mapCol.Count}");
    Emit($"MapRegion   {regionCol.Count}");
    Emit($"SafeZoneInfo{szCol.Count}");
    Emit($"GuardInfo   {guardCol.Count}");
    Emit($"NPCInfo     {npcCol.Count}");
    Emit($"RespawnInfo {respCol.Count}");
    Emit($"MovementInfo{movCol.Count}");
}

// ---------------- del-monster ----------------

// Delete every MonsterInfo row with the given name (case-insensitive).
// Used to clean up import mistakes before re-import.
void DeleteMonster(Session s, string name)
{
    BackupDb(s);
    var monsterCol = s.GetCollection<MonsterInfo>();
    var rows = monsterCol.Binding.Where(x => string.Equals(x.MonsterName, name, StringComparison.OrdinalIgnoreCase)).ToList();
    foreach (var m in rows)
    {
        // RespawnInfo references must be detached first (they hold Monster FK)
        var respawns = s.GetCollection<RespawnInfo>().Binding
            .Where(r => r.Monster == m).ToList();
        foreach (var r in respawns) r.Monster = null;
        m.Delete();
    }
    s.Save(true);
    Console.WriteLine($"Deleted MonsterInfo rows: {rows.Count} (name='{name}')");
}

// ---------------- import-monsters ----------------

// Import the EI (英雄杀) new-monster roster into System.db.
//
// The <monstersJson> is an array of:
//   { "name", "image", "ai", "level", "experience", "viewRange", "coolEye",
//     "undead", "canPush", "canTame", "attackDelay", "moveDelay", "isBoss",
//     "flag", "faceImage" }
// image is the MonsterImage enum name (already added to LibraryCore/Enum.cs and
// MonsterLookup.cs). Existing monsters (by name, case-insensitive) are skipped.
void ImportMonsters(Session s, string jsonPath, string outputFile = null)
{
    BackupDb(s);

    using var log = new StreamWriter(outputFile ?? Path.Combine(Path.GetTempPath(), "import_monsters_result.txt"), append: false);
    void Emit(string line)
    {
        Console.WriteLine(line);
        log.WriteLine(line);
    }

    if (!File.Exists(jsonPath)) throw new FileNotFoundException("monsters json not found", jsonPath);

    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
    var monsterCol = s.GetCollection<MonsterInfo>();
    var existing = new HashSet<string>(monsterCol.Binding.Select(x => x.MonsterName), StringComparer.OrdinalIgnoreCase);

    int added = 0, skipped = 0;
    var errors = new List<string>();

    foreach (JsonElement e in doc.RootElement.EnumerateArray())
    {
        string name = e.GetProperty("name").GetString();
        if (existing.Contains(name))
        {
            skipped++;
            continue;
        }
        string image = e.GetProperty("image").GetString();
        if (!Enum.TryParse<MonsterImage>(image, out var imageEnum))
        {
            errors.Add($"bad image enum '{image}' for {name}");
            continue;
        }
        var m = monsterCol.CreateNewObject();
        m.MonsterName = name;
        m.Image = imageEnum;
        m.AI = e.GetProperty("ai").GetInt32();
        m.Level = e.GetProperty("level").GetInt32();
        m.Experience = e.GetProperty("experience").GetDecimal();
        m.ViewRange = e.GetProperty("viewRange").GetInt32();
        m.CoolEye = e.GetProperty("coolEye").GetInt32();
        m.Undead = e.GetProperty("undead").GetBoolean();
        m.CanPush = e.GetProperty("canPush").GetBoolean();
        m.CanTame = e.GetProperty("canTame").GetBoolean();
        m.AttackDelay = e.GetProperty("attackDelay").GetInt32();
        m.MoveDelay = e.GetProperty("moveDelay").GetInt32();
        m.IsBoss = e.GetProperty("isBoss").GetBoolean();
        m.Flag = e.GetProperty("flag").GetString() is string fl && Enum.TryParse<MonsterFlag>(fl, out var flagEnum)
            ? flagEnum
            : MonsterFlag.None;
        m.FaceImage = e.TryGetProperty("faceImage", out var fi) ? fi.GetInt32() : 0;
        added++;
    }

    s.Save(true);
    Emit("=== import-monsters results ===");
    Emit($"MonsterInfo added: {added}   (skipped existing: {skipped})");
    foreach (var err in errors) Emit($"ERROR: {err}");
}

// ---------------- delete-records ----------------

// Delete records of a collection by explicit DB Index list (JSON array of ints).
// Used to remove guards/movements whose coordinates fail validation on the
// deployed (EI) map files — they can never spawn/work and only spam errors.
void DeleteRecords(Session s, string collectionName, string indexesJson)
{
    BackupDb(s);
    var indexes = JsonSerializer.Deserialize<List<int>>(File.ReadAllText(indexesJson))
                  ?? throw new ArgumentException("indexes json must be an int array");
    var set = new HashSet<int>(indexes);
    var errors = new List<string>();
    int deleted = collectionName.ToLowerInvariant() switch
    {
        "guardinfo" => DeleteByIndex<GuardInfo>(s, set, errors, "delete-by-index"),
        "movementinfo" => DeleteByIndex<MovementInfo>(s, set, errors, "delete-by-index"),
        "npcinfo" => DeleteByIndex<NPCInfo>(s, set, errors, "delete-by-index"),
        "respawninfo" => DeleteByIndex<RespawnInfo>(s, set, errors, "delete-by-index"),
        _ => throw new ArgumentException($"unsupported collection '{collectionName}' (GuardInfo/MovementInfo/NPCInfo/RespawnInfo)"),
    };
    s.Save(true);
    Console.WriteLine($"Deleted {deleted} records from {collectionName} (requested {indexes.Count})");
    foreach (var e in errors) Console.WriteLine($"ERROR: {e}");
}

// ---------------- set-safezone-point ----------------

// Move a SafeZoneInfo's region (and bind region) to a single walkable point.
// Used to relocate EI safezones whose map-center coordinate is a wall.
void SetSafeZonePoint(Session s, int safeZoneIndex, string mapFileName, int x, int y)
{
    BackupDb(s);
    var sz = s.GetCollection<SafeZoneInfo>().Binding.FirstOrDefault(z => z.Index == safeZoneIndex)
             ?? throw new ArgumentException($"SafeZoneInfo #{safeZoneIndex} not found");
    var map = s.GetCollection<MapInfo>().Binding
        .FirstOrDefault(m => string.Equals(m.FileName, mapFileName, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"MapInfo '{mapFileName}' not found");
    foreach (var region in new[] { sz.Region, sz.BindRegion })
    {
        if (region == null) continue;
        region.Map = map;
        region.PointRegion = new[] { new Point(x, y) };
        region.Size = 1;
    }
    s.Save(true);
    Console.WriteLine($"SafeZone #{safeZoneIndex} moved to {mapFileName} ({x},{y})");
}

// ---------------- trim-safezones ----------------

// For every SafeZoneInfo region, drop PointRegion points that are not walkable
// on the deployed map file (the old Zircon safezones were designed for the
// original map layouts; after the EI map swap some points land on walls).
// Walkable check mirrors ServerLibrary/Models/Map.cs Load(): flag 0x01|0x02.
void TrimSafeZones(Session s)
{
    BackupDb(s);
    var mapDir = Path.Combine(Path.GetDirectoryName(s.SystemPath), "..", "Map");
    if (!Directory.Exists(mapDir)) mapDir = "Debug/ServerCore/Map/";
    int trimmed = 0, unchanged = 0;
    foreach (var sz in s.GetCollection<SafeZoneInfo>().Binding)
    {
        foreach (var region in new[] { sz.Region, sz.BindRegion })
        {
            if (region?.Map == null || region.PointRegion == null || region.PointRegion.Length == 0) continue;
            string mapFile = region.Map.FileName + ".map";
            string path = Path.Combine(mapDir, mapFile);
            if (!File.Exists(path)) continue;
            byte[] fileBytes = File.ReadAllBytes(path);
            int w = fileBytes[23] << 8 | fileBytes[22];
            int h = fileBytes[25] << 8 | fileBytes[24];
            int offSet = 28 + w * h / 4 * 3;
            var keep = region.PointRegion.Where(p =>
            {
                if (p.X < 0 || p.X >= w || p.Y < 0 || p.Y >= h) return false;
                int idx = offSet + (p.X * h + p.Y) * 14;
                if (idx + 1 >= fileBytes.Length) return false;
                byte flag = fileBytes[idx];
                return (flag & 0x02) == 2 && (flag & 0x01) == 1;
            }).ToArray();
            if (keep.Length != region.PointRegion.Length)
            {
                region.PointRegion = keep.Length > 0 ? keep : region.PointRegion;
                if (keep.Length > 0) trimmed++;
            }
            else unchanged++;
        }
    }
    s.Save(true);
    Console.WriteLine($"Trimmed {trimmed} safezone regions (kept valid walkable points), {unchanged} unchanged");
}

// ---------------- move-respawns ----------------

// Relocate RespawnInfo region points that land on non-walkable cells to the
// nearest walkable cell (computed offline by the migration script). Input JSON:
// [{ "index", "map", "x", "y", "nx", "ny" }, ...]
void MoveRespawns(Session s, string fixesJson)
{
    BackupDb(s);
    using var doc = JsonDocument.Parse(File.ReadAllText(fixesJson));
    var respawns = s.GetCollection<RespawnInfo>().Binding.ToDictionary(x => x.Index);
    var mapsByName = s.GetCollection<MapInfo>().Binding
        .ToDictionary(x => x.FileName, StringComparer.OrdinalIgnoreCase);
    int moved = 0, skipped = 0;
    foreach (JsonElement f in doc.RootElement.EnumerateArray())
    {
        int idx = f.GetProperty("index").GetInt32();
        if (!respawns.TryGetValue(idx, out var resp) || resp.Region == null)
        {
            skipped++; continue;
        }
        string mapName = f.GetProperty("map").GetString();
        if (!mapsByName.TryGetValue(mapName, out var map)) { skipped++; continue; }
        resp.Region.Map = map;
        resp.Region.PointRegion = new[] { new Point(f.GetProperty("nx").GetInt32(), f.GetProperty("ny").GetInt32()) };
        resp.Region.Size = 1;
        moved++;
    }
    s.Save(true);
    Console.WriteLine($"Moved {moved} respawn regions (skipped {skipped})");
}

// ---------------- fix-sabuk ----------------

// Replace Sabuk Keep's (MapInfo Index=7, FileName="3") map regions with the
// Zircon-original set.
//
// The EI (英雄杀) import added ~38 extra teleport regions + 38 MovementInfo on
// Sabuk Keep with coordinates encoded for a 400x600 map, but the deployed
// 3.map is the Zircon 350x350 original. 18 of those points are out of bounds
// (X up to 354), so entering Sabuk hangs StartGame on
//   [Movement] Bad Origin, Source: Sabuk Keep (3) - Teleport Source, X:354, Y:43
//
// <zirconDataJson> is /tmp/investigate/sabuk_zircon_data.json, produced by
// extract_sabuk.py from System.db.pre-delete (the Zircon-original DB). The
// "zircon" section carries the authoritative Sabuk configuration:
//   27 MapRegion  (incl. BitRegion-encoded 'Sabuk Area', decoded on the
//                  current 350-wide grid)
//   12 MovementInfo (door<->landing pairs; the other endpoint is a region on
//                    another map, resolved in the current DB by original index)
//    1 SafeZoneInfo, 9 NPCInfo, CastleInfo region references
//
// Steps:
//   1. delete every MapRegion on Sabuk plus every dependent record referencing
//      one (MovementInfo by Source/DestinationRegion, SafeZoneInfo by
//      Region/BindRegion, NPCInfo/RespawnInfo by Region, GuardInfo/MineInfo by
//      Map); QuestTask.RegionParameter refs are detached, not deleted
//   2. rebuild the 27 regions with fresh indices (current max + 1) and remap
//      old Zircon index -> new region
//   3. recreate the dependents pointing into the new regions (external
//      endpoints are resolved by original index in the current DB)
//   4. repoint CastleInfo.CastleRegion/ObjectiveRegion/AttackSpawnRegion
//   5. Save(true)
void FixSabuk(Session s, string jsonPath, string outputFile = null)
{
    BackupDb(s);

    using var log = new StreamWriter(outputFile ?? Path.Combine(Path.GetTempPath(), "sabuk_fix_result.txt"), append: false);
    void Emit(string line)
    {
        Console.WriteLine(line);
        log.WriteLine(line);
    }

    if (!File.Exists(jsonPath)) throw new FileNotFoundException("zircon data json not found", jsonPath);

    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
    JsonElement z = doc.RootElement.GetProperty("zircon");

    var mapCol = s.GetCollection<MapInfo>();
    var regionCol = s.GetCollection<MapRegion>();
    var movCol = s.GetCollection<MovementInfo>();
    var respCol = s.GetCollection<RespawnInfo>();
    var szCol = s.GetCollection<SafeZoneInfo>();
    var npcCol = s.GetCollection<NPCInfo>();
    var guardCol = s.GetCollection<GuardInfo>();
    var mineCol = s.GetCollection<MineInfo>();
    var pageCol = s.GetCollection<NPCPage>();

    // ---- 0. Sabuk map ----
    var sabuk = mapCol.Binding.FirstOrDefault(m => m.Index == 7);
    if (sabuk == null) throw new InvalidOperationException("MapInfo Index=7 (Sabuk Keep) not found");
    if (sabuk.FileName != "3") Emit($"WARN  Sabuk Keep FileName is '{sabuk.FileName}' (expected '3')");

    // capture CastleInfo region refs BEFORE deletion (old indices)
    var sabukCastles = s.GetCollection<CastleInfo>().Binding.Where(x => x.Map == sabuk).ToList();
    var castleRefs = sabukCastles
        .Select(c => (c.CastleRegion?.Index ?? 0, c.ObjectiveRegion?.Index ?? 0, c.AttackSpawnRegion?.Index ?? 0))
        .ToList();

    // ---- 1. delete dependents + regions (bottom-up) ----
    int movDel = 0, respDel = 0, szDel = 0, npcDel = 0, guardDel = 0, mineDel = 0, regionDel = 0, questTasksDetached = 0;
    var errors = new List<string>();

    foreach (var m in movCol.Binding.Where(x => x.SourceRegion?.Map == sabuk || x.DestinationRegion?.Map == sabuk).ToList())
    {
        try { m.Delete(); movDel++; }
        catch (ArgumentException) { movDel++; }
        catch (Exception e) { errors.Add($"Movement #{m.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var x in respCol.Binding.Where(x => x.Region?.Map == sabuk).ToList())
    {
        try { x.Delete(); respDel++; }
        catch (ArgumentException) { respDel++; }
        catch (Exception e) { errors.Add($"Respawn #{x.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var x in szCol.Binding.Where(x => x.Region?.Map == sabuk || x.BindRegion?.Map == sabuk).ToList())
    {
        try { x.Delete(); szDel++; }
        catch (ArgumentException) { szDel++; }
        catch (Exception e) { errors.Add($"SafeZone #{x.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var x in npcCol.Binding.Where(x => x.Region?.Map == sabuk).ToList())
    {
        try { x.Delete(); npcDel++; }
        catch (ArgumentException) { npcDel++; }
        catch (Exception e) { errors.Add($"NPC #{x.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var x in guardCol.Binding.Where(x => x.Map == sabuk).ToList())
    {
        try { x.Delete(); guardDel++; }
        catch (ArgumentException) { guardDel++; }
        catch (Exception e) { errors.Add($"Guard #{x.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var x in mineCol.Binding.Where(x => x.Map == sabuk).ToList())
    {
        try { x.Delete(); mineDel++; }
        catch (ArgumentException) { mineDel++; }
        catch (Exception e) { errors.Add($"Mine #{x.Index}: {e.GetType().Name}: {e.Message}"); }
    }
    foreach (var reg in regionCol.Binding.Where(x => x.Map == sabuk).ToList())
    {
        foreach (var qt in reg.QuestTasks.ToList())
        {
            try { qt.RegionParameter = null; questTasksDetached++; }
            catch (Exception e) { errors.Add($"QuestTask #{qt.Index} detach: {e.GetType().Name}: {e.Message}"); }
        }
        try { reg.Delete(); regionDel++; }
        catch (ArgumentException) { regionDel++; }
        catch (Exception e) { errors.Add($"Region #{reg.Index}: {e.GetType().Name}: {e.Message}"); }
    }

    // ---- 2. rebuild the 27 regions with fresh indices ----
    int maxRegionIndex = regionCol.Binding.Count == 0 ? 0 : regionCol.Binding.Max(x => x.Index);
    var remap = new Dictionary<int, MapRegion>();
    var newRegions = new List<(int OldIndex, MapRegion Region)>();
    var jsonRegions = z.GetProperty("regions").EnumerateArray().ToList();

    foreach (JsonElement e in jsonRegions)
    {
        int oldIndex = e.GetProperty("Index").GetInt32();
        string desc = e.GetProperty("Description").GetString() ?? "";
        int type = e.GetProperty("RegionType").GetInt32();
        int size = e.GetProperty("Size").GetInt32();

        var region = regionCol.CreateNewObject();
        region.Map = sabuk;
        region.Description = desc;
        region.RegionType = (RegionType)type;

        // points: PointRegion if present, else decode BitRegion on the 350 grid
        List<Point> points = new List<Point>();
        if (e.TryGetProperty("PointRegion", out JsonElement pr) && pr.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement p in pr.EnumerateArray())
                points.Add(new Point(p[0].GetInt32(), p[1].GetInt32()));
        }
        else if (e.TryGetProperty("BitRegion", out JsonElement br) && br.ValueKind == JsonValueKind.String && br.GetString() is string hex && hex.Length > 0)
        {
            byte[] bytes = Convert.FromHexString(hex);
            var bits = new System.Collections.BitArray(bytes);
            for (int i = 0; i < bits.Length; i++)
                if (bits[i]) points.Add(new Point(i % 350, i / 350));
        }
        region.PointRegion = points.ToArray();
        region.Size = points.Count > 0 ? points.Count : size;

        remap[oldIndex] = region;
        newRegions.Add((oldIndex, region));
    }

    // ---- 3. external endpoint lookup by original index ----
    var extByIndex = new Dictionary<int, JsonElement>();
    foreach (JsonProperty jp in z.GetProperty("ext_regions").EnumerateObject())
        extByIndex[int.Parse(jp.Name)] = jp.Value;

    MapRegion ResolveRegion(int originalIndex, string role)
    {
        if (remap.TryGetValue(originalIndex, out var r)) return r;
        if (!extByIndex.TryGetValue(originalIndex, out JsonElement ext))
            throw new InvalidOperationException($"region #{originalIndex} ({role}): neither one of the 27 Sabuk regions nor in ext_regions");
        var found = regionCol.Binding.FirstOrDefault(x => x.Index == originalIndex);
        int expectedMap = ext.GetProperty("Map").GetInt32();
        if (found == null)
            throw new InvalidOperationException($"region #{originalIndex} ({role}): external endpoint missing from current DB");
        if (found.Map?.Index != expectedMap)
            throw new InvalidOperationException($"region #{originalIndex} ({role}): current DB map {found.Map?.Index} != expected {expectedMap}");
        return found;
    }

    // ---- 4. movements ----
    int movAdd = 0;
    foreach (JsonElement e in z.GetProperty("movements").EnumerateArray())
    {
        var src = ResolveRegion(e.GetProperty("SourceRegion").GetInt32(), "SourceRegion");
        var dst = ResolveRegion(e.GetProperty("DestinationRegion").GetInt32(), "DestinationRegion");
        var m = movCol.CreateNewObject();
        m.SourceRegion = src;
        m.DestinationRegion = dst;
        m.Icon = (MapIcon)e.GetProperty("Icon").GetInt32();
        m.Effect = (MovementEffect)e.GetProperty("Effect").GetInt32();
        m.RequiredClass = (RequiredClass)e.GetProperty("RequiredClass").GetInt32();
        m.SkipValidation = e.GetProperty("SkipValidation").GetBoolean();
        movAdd++;
    }

    // ---- 5. safezones ----
    int szAdd = 0;
    foreach (JsonElement e in z.GetProperty("safezones").EnumerateArray())
    {
        var region = ResolveRegion(e.GetProperty("Region").GetInt32(), "SafeZone Region");
        var bind = ResolveRegion(e.GetProperty("BindRegion").GetInt32(), "SafeZone BindRegion");
        var sz = szCol.CreateNewObject();
        sz.Region = region;
        sz.BindRegion = bind;
        sz.StartClass = (RequiredClass)e.GetProperty("StartClass").GetInt32();
        sz.RedZone = e.GetProperty("RedZone").GetBoolean();
        sz.Border = e.GetProperty("Border").GetBoolean();
        szAdd++;
    }

    // ---- 6. NPCs ----
    int npcAdd = 0;
    foreach (JsonElement e in z.GetProperty("npcs").EnumerateArray())
    {
        var region = ResolveRegion(e.GetProperty("Region").GetInt32(), "NPC Region");
        var npc = npcCol.CreateNewObject();
        npc.Region = region;
        npc.NPCName = e.GetProperty("NPCName").GetString() ?? "";
        npc.Image = e.GetProperty("Image").GetInt32();
        npc.FaceImage = e.GetProperty("FaceImage").GetInt32();
        npc.GoodsIndex = e.GetProperty("GoodsIndex").GetInt32();
        npc.MapIcon = (MapIcon)e.GetProperty("MapIcon").GetInt32();
        int pageIdx = e.GetProperty("EntryPage").GetInt32();
        if (pageIdx != 0)
        {
            var page = pageCol.Binding.FirstOrDefault(p => p.Index == pageIdx);
            if (page == null) errors.Add($"NPC {npc.NPCName}: NPCPage #{pageIdx} missing");
            else npc.EntryPage = page;
        }
        npcAdd++;
    }

    // ---- 7. CastleInfo region remap (old index -> new region) ----
    int castleFixed = 0;
    for (int i = 0; i < sabukCastles.Count; i++)
    {
        var castle = sabukCastles[i];
        var (cr, ob, asr) = castleRefs[i];
        bool changed = false;
        if (cr != 0 && remap.TryGetValue(cr, out var crR)) { castle.CastleRegion = crR; changed = true; }
        if (ob != 0 && remap.TryGetValue(ob, out var obR)) { castle.ObjectiveRegion = obR; changed = true; }
        if (asr != 0 && remap.TryGetValue(asr, out var asR)) { castle.AttackSpawnRegion = asR; changed = true; }
        if (changed) castleFixed++;
    }

    // ---- 8. save + report ----
    s.Save(true);

    Emit("=== fix-sabuk result ===");
    Emit($"sabuk map           : #{sabuk.Index} '{sabuk.FileName}' \"{sabuk.Description}\"");
    Emit($"deleted movements   : {movDel}");
    Emit($"deleted respawns    : {respDel}");
    Emit($"deleted safezones   : {szDel}");
    Emit($"deleted npcs        : {npcDel}");
    Emit($"deleted guards      : {guardDel}");
    Emit($"deleted mines       : {mineDel}");
    Emit($"deleted regions     : {regionDel}");
    Emit($"quest tasks detached: {questTasksDetached}");
    Emit($"regions rebuilt     : {newRegions.Count} (old max index {maxRegionIndex})");
    foreach (var (oldIdx, region) in newRegions.OrderBy(x => x.OldIndex))
        Emit($"  #{region.Index,5} <- old #{oldIdx,-5} \"{region.Description}\"  type={region.RegionType}  size={region.Size}  first={string.Join(";", region.PointRegion.Take(2).Select(p => $"({p.X},{p.Y})"))}");
    Emit($"movements rebuilt   : {movAdd}");
    Emit($"safezones rebuilt   : {szAdd}");
    Emit($"npcs rebuilt        : {npcAdd}");
    Emit($"castles repointed   : {castleFixed}");
    Emit($"errors              : {errors.Count}");
    foreach (var e in errors) Emit($"  ! {e}");
    Emit($"Saved. System.db -> {s.SystemPath}");
}
