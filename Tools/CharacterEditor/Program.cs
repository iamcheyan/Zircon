// CharacterEditor — 账号/角色数据库(Users.db)只读探查与修改工具。
//
// 用法:
//   dotnet run --project Tools/CharacterEditor -- list <db-root> [账号邮箱] [角色名]
//       列出全部账号/角色/物品/魔法/货币;可带过滤条件只看一个账号或角色。
//   dotnet run --project Tools/CharacterEditor -- boost <db-root> --char <角色名> [--level N] [--gold N]
//       给角色加一套按职业/等级匹配的初始装备 + 背包消耗品 + 职业全套技能(不删已有物品)。
//   dotnet run --project Tools/CharacterEditor -- items <db-root> --type <ItemType> [--class <Warrior|Wizard|Taoist|Assassin>] [--min N] [--max N]
//       列出物品模板(挑选装备用), --type 可选 Weapon/Armour/Helmet/Necklace/Bracelet/Ring/Shoes/Amulet/Belt/Consumable/Book...
//
// 注意:
//   * 修改前请先停止服务端, 改完再启动(服务端启动时从磁盘加载角色数据)。
//   * 工具会先备份 Users.db 到 Backup/ 目录(Session 自带行为)。
//   * 物品 Slot 规则: 角色背包 = 0..47, 装备 = 1000 + EquipmentSlot(武器0/衣服1/头盔2/火把3/项链4/
//     左手镯5/右手镯6/左戒指7/右戒指8/鞋子9/毒药10/护身符11/花12/马甲13/徽章14/盾15/时装16)。
//   * 账号仓库 = 0..99(本工具未实现), 材料包 = 2000+。

using System.Reflection;
using Library;
using Library.MirDB;
using Library.SystemModels;
using MirDB;
using Server.DBModels;

static class Program
{
    const string WeaponNames = "武器";
    static string Root = null;
    static Session Session = null;

    static void Main(string[] args)
    {
        if (args.Length < 2) { Usage(); return; }

        string cmd = args[0];
        Root = Path.GetFullPath(args[1]);
        if (!Root.EndsWith(Path.DirectorySeparatorChar)) Root += Path.DirectorySeparatorChar;
        var rest = args.Skip(2).ToArray();

        if (cmd == "inspect")
        {
            var lib = LibraryCache.Get(LibraryFile.GameInter);
            for (int i = 50; i <= 60; i++)
            {
                var img = lib.Images[i];
                Console.WriteLine($"[Index {i}]: W={img?.Width}, H={img?.Height}, OffX={img?.OffSetX}, OffY={img?.OffSetY}");
            }
            return;
        }
        if (cmd == "list") { List(rest); return; }
        if (cmd == "boost") { Boost(rest); return; }
        if (cmd == "equip") { Equip(rest); return; }
        if (cmd == "lighten") { Lighten(rest); return; }
        if (cmd == "items") { ListItems(rest); return; }
        if (cmd == "magics") { ListMagics(rest); return; }
        if (cmd == "basestat") { ListBaseStats(rest); return; }

        Usage();
    }

    static void Usage()
    {
        Console.WriteLine("用法:");
        Console.WriteLine("  CharacterEditor list  <db-root> [账号邮箱] [角色名]");
        Console.WriteLine("  CharacterEditor boost <db-root> --char <角色名> [--level N] [--gold N] [--class 职业]");
        Console.WriteLine("                        [--magic 名字] [--weapon 名字] [--amulet-count N] [--no-items] [--no-magics]");
        Console.WriteLine("  CharacterEditor equip <db-root> --char <角色名> --slot <槽名> --item <物品名子串>");
        Console.WriteLine("  CharacterEditor lighten <db-root> --char <角色名>  (轻装并缩减背包重物堆叠)");
        Console.WriteLine("        给指定装备槽放物品(槽名: Weapon/Armour/Helmet/Torch/Necklace/BraceletL/RingL/...), 背包有同款则移动, 否则新建");
        Console.WriteLine("  CharacterEditor items <db-root> [--type Weapon] [--class Warrior] [--min 1] [--max 60]");
    }

    static void Open()
    {
        Session = new Session(SessionMode.Users, Root);
        Session.Initialize(
            typeof(ItemInfo).Assembly,         // LibraryCore (SystemModels)
            typeof(AccountInfo).Assembly);     // ServerLibrary (DBModels)
        Console.WriteLine($"数据库: {Session.SystemPath}  /  {Session.UsersPath}");
    }

    static string ClassZh(MirClass c) => c switch
    {
        MirClass.Warrior => "战士", MirClass.Wizard => "法师", MirClass.Taoist => "道士", MirClass.Assassin => "刺客",
        _ => c.ToString(),
    };
    static string ReqClassZh(RequiredClass c) => c switch
    {
        RequiredClass.Warrior => "战士", RequiredClass.Wizard => "法师", RequiredClass.Taoist => "道士",
        RequiredClass.Assassin => "刺客", RequiredClass.WarWizTao => "战法道", RequiredClass.WizTao => "法道",
        RequiredClass.AssWar => "刺战", RequiredClass.All => "全职业", _ => "无限制",
    };
    static string ItemTypeZh(ItemType t) => t switch
    {
        ItemType.Weapon => "武器", ItemType.Armour => "护甲", ItemType.Helmet => "头盔", ItemType.Necklace => "项链",
        ItemType.Bracelet => "手镯", ItemType.Ring => "戒指", ItemType.Shoes => "鞋子", ItemType.Amulet => "护身符",
        ItemType.Consumable => "消耗品", ItemType.Book => "技能书", ItemType.Meat => "肉类",
        ItemType.Poison => "毒药", ItemType.Ore => "矿石", ItemType.Scroll => "卷轴", ItemType.Torch => "火把",
        _ => t.ToString(),
    };

    static string EquipSlotZh(int slot)
    {
        if (slot < 0 || slot > 21) return $"Slot{slot}";
        return ((EquipmentSlot)slot).ToString();
    }

    // ---------- list ----------

    static void List(string[] args)
    {
        string mailFilter = args.Length > 0 ? args[0] : null;
        string charFilter = args.Length > 1 ? args[1] : null;
        Open();

        var accounts = Session.GetCollection<AccountInfo>().Binding;
        Console.WriteLine($"账号数: {accounts.Count}");
        Console.WriteLine($"[调试] SystemDb存在={Session.SystemDatabaseExists} 版本={Session.SystemDatabaseVersion} 模式={Session.Mode}");
        try
        {
            Console.WriteLine($"[调试] 物品模板={Session.GetCollection<ItemInfo>().Binding.Count} 魔法={Session.GetCollection<MagicInfo>().Binding.Count} 货币={Session.GetCollection<CurrencyInfo>().Binding.Count} 角色={Session.GetCollection<CharacterInfo>().Binding.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[调试] 集合访问异常: {ex.Message}\n{ex.StackTrace?.Split('\n').FirstOrDefault()}");
        }
        foreach (var acc in accounts)
        {
            if (mailFilter != null && !acc.EMailAddress.Equals(mailFilter, StringComparison.OrdinalIgnoreCase)) continue;
            var gold = acc.Currencies.FirstOrDefault(x => x.Info.Type == CurrencyType.Gold);
            var gameGold = acc.Currencies.FirstOrDefault(x => x.Info.Type == CurrencyType.GameGold);
            var huntGold = acc.Currencies.FirstOrDefault(x => x.Info.Type == CurrencyType.HuntGold);
            Console.WriteLine($"\n========== 账号: {acc.EMailAddress} (Admin={acc.Admin}, 仓库={acc.StorageSize}, 角色数={acc.Characters.Count}) ==========");
            Console.WriteLine($"  金币={gold?.Amount ?? 0}  商城币={gameGold?.Amount ?? 0}  HuntGold={huntGold?.Amount ?? 0}");

            foreach (var ch in acc.Characters)
            {
                if (charFilter != null && !ch.CharacterName.Equals(charFilter, StringComparison.OrdinalIgnoreCase)) continue;
                Console.WriteLine($"\n  ---- 角色: {ch.CharacterName} | 职业={ClassZh(ch.Class)} 性别={ch.Gender} 等级={ch.Level} " +
                                  $"HP={ch.CurrentHP} MP={ch.CurrentMP} 创建={ch.CreationDate:yyyy-MM-dd}");
                Console.WriteLine($"      魔法书 ({ch.Magics.Count}):");
                foreach (var m in ch.Magics.OrderBy(m => m.Info.Magic))
                    Console.WriteLine($"        - {m.Info.Name} (Magic={m.Info.Magic}, Level={m.Level}, Exp={m.Experience})");
                Console.WriteLine($"      物品 ({ch.Items.Count}):");
                foreach (var item in ch.Items.OrderBy(i => i.Slot))
                {
                    string where = item.Slot >= 1000
                        ? $"装备[{EquipSlotZh(item.Slot - 1000)}]"
                        : $"背包[{item.Slot}]";
                    Console.WriteLine($"        - {where}  {item.Info.ItemName}  x{item.Count}  " +
                                      $"重量 {item.Weight}  耐久 {item.CurrentDurability}/{item.MaxDurability}  Level={item.Level}");
                }
            }
        }
    }

    // ---------- equip (给指定装备槽放物品) ----------

    static ItemType[] SlotItemTypes(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Weapon => new[] { ItemType.Weapon },
        EquipmentSlot.Armour => new[] { ItemType.Armour },
        EquipmentSlot.Helmet => new[] { ItemType.Helmet },
        EquipmentSlot.Torch => new[] { ItemType.Torch },
        EquipmentSlot.Necklace => new[] { ItemType.Necklace },
        EquipmentSlot.BraceletL or EquipmentSlot.BraceletR => new[] { ItemType.Bracelet },
        EquipmentSlot.RingL or EquipmentSlot.RingR => new[] { ItemType.Ring },
        EquipmentSlot.Shoes => new[] { ItemType.Shoes },
        EquipmentSlot.Poison => new[] { ItemType.Poison },
        EquipmentSlot.Amulet => new[] { ItemType.Amulet },
        EquipmentSlot.Flower => new[] { ItemType.Flower },
        EquipmentSlot.HorseArmour => new[] { ItemType.HorseArmour },
        EquipmentSlot.Emblem => new[] { ItemType.Emblem },
        EquipmentSlot.Shield => new[] { ItemType.Shield },
        EquipmentSlot.Costume => new[] { ItemType.Costume },
        EquipmentSlot.Hook => new[] { ItemType.Hook },
        EquipmentSlot.Float => new[] { ItemType.Float },
        EquipmentSlot.Bait => new[] { ItemType.Bait },
        EquipmentSlot.Finder => new[] { ItemType.Finder },
        EquipmentSlot.Reel => new[] { ItemType.Reel },
        _ => Array.Empty<ItemType>(),
    };

    static void Equip(string[] args)
    {
        string charName = null, slotName = null, itemName = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--char": charName = args[++i]; break;
                case "--slot": slotName = args[++i]; break;
                case "--item": itemName = args[++i]; break;
            }
        }
        if (charName == null || slotName == null || itemName == null)
        { Console.WriteLine("需要 --char <角色名> --slot <槽名> --item <物品名子串>"); return; }
        if (!Enum.TryParse<EquipmentSlot>(slotName, true, out var slot))
        { Console.WriteLine($"未知槽位: {slotName}, 可用: {string.Join("/", Enum.GetNames<EquipmentSlot>())}"); return; }

        Open();

        var acc = Session.GetCollection<AccountInfo>().Binding
            .FirstOrDefault(a => a.Characters.Any(c => c.CharacterName.Equals(charName, StringComparison.OrdinalIgnoreCase)));
        if (acc == null) { Console.WriteLine($"找不到角色: {charName}"); return; }
        var ch = acc.Characters.First(c => c.CharacterName.Equals(charName, StringComparison.OrdinalIgnoreCase));

        int slotIdx = 1000 + (int)slot;
        var allowed = SlotItemTypes(slot);
        var pick = Session.GetCollection<ItemInfo>().Binding
            .FirstOrDefault(x => x.ItemName.Contains(itemName, StringComparison.OrdinalIgnoreCase) && allowed.Contains(x.ItemType));
        if (pick == null)
        {
            Console.WriteLine($"找不到名字含 [{itemName}] 且类型匹配槽 {slot} 的物品模板 ({string.Join("/", allowed)})");
            return;
        }

        var existing = ch.Items.FirstOrDefault(x => x.Slot == slotIdx);
        if (existing != null)
        {
            Console.WriteLine($"  装备[{slot}] {existing.Info.ItemName} -> {pick.ItemName}");
            existing.Info = pick; existing.Count = 1;
            if (pick.Durability > 0) { existing.CurrentDurability = pick.Durability; existing.MaxDurability = pick.Durability; }
        }
        else
        {
            // 优先移动背包中同款物品(如 Candle), 否则新建
            var bagItem = ch.Items.FirstOrDefault(x => x.Slot >= 0 && x.Slot < 48 && x.Info.Index == pick.Index);
            if (bagItem != null)
            {
                int oldSlot = bagItem.Slot;
                bagItem.Slot = slotIdx; bagItem.Count = 1;
                Console.WriteLine($"  装备[{slot}] 背包[{oldSlot}] {pick.ItemName} -> 装备槽 {slotIdx}");
            }
            else
            {
                var item = Session.GetCollection<UserItem>().CreateNewObject();
                item.Info = pick; item.Slot = slotIdx; item.Count = 1;
                if (pick.Durability > 0) { item.CurrentDurability = pick.Durability; item.MaxDurability = pick.Durability; }
                ch.Items.Add(item);
                Console.WriteLine($"  装备[{slot}] += {pick.ItemName}");
            }
        }
        Console.WriteLine("  保存中...");
        Session.Save(true);
        Console.WriteLine("  完成。重启服务端后生效。");
    }

    static void Lighten(string[] args)
    {
        string charName = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--char" && i + 1 < args.Length) charName = args[++i];
        if (charName == null) { Console.WriteLine("需要 --char <角色名>"); return; }

        Open();
        var ch = Session.GetCollection<AccountInfo>().Binding
            .SelectMany(a => a.Characters)
            .FirstOrDefault(c => c.CharacterName.Equals(charName, StringComparison.OrdinalIgnoreCase));
        if (ch == null) { Console.WriteLine($"找不到角色: {charName}"); return; }

        var lightGear = new Dictionary<EquipmentSlot, string>
        {
            [EquipmentSlot.Weapon] = "Wood Sword",
            [EquipmentSlot.Armour] = "Commoner Outfit (M)",
            [EquipmentSlot.Helmet] = "Bronze Helmet",
            [EquipmentSlot.Necklace] = "Gold Necklace",
            [EquipmentSlot.BraceletL] = "Silver Bracelet",
            [EquipmentSlot.BraceletR] = "Silver Bracelet",
            [EquipmentSlot.RingL] = "Plain Ring",
            [EquipmentSlot.RingR] = "Plain Ring",
            [EquipmentSlot.Shoes] = "Straw Sandles",
        };
        var templates = Session.GetCollection<ItemInfo>().Binding;
        foreach (var pair in lightGear)
        {
            int slot = 1000 + (int)pair.Key;
            var template = templates.FirstOrDefault(x => x.ItemName.Equals(pair.Value, StringComparison.OrdinalIgnoreCase));
            var equipped = ch.Items.FirstOrDefault(x => x.Slot == slot);
            if (template == null || equipped == null) continue;
            Console.WriteLine($"  装备[{pair.Key}] {equipped.Info.ItemName} -> {template.ItemName} (Weight={template.Weight})");
            equipped.Info = template;
            equipped.Count = 1;
            if (template.Durability > 0)
            {
                equipped.CurrentDurability = template.Durability;
                equipped.MaxDurability = template.Durability;
            }
        }

        foreach (var item in ch.Items.Where(x => x.Slot >= 0 && x.Slot < 48 && x.Count > 1))
        {
            Console.WriteLine($"  背包[{item.Slot}] {item.Info.ItemName} x{item.Count} -> x1");
            item.Count = 1;
        }

        Console.WriteLine("  保存中...");
        Session.Save(true);
        Console.WriteLine("  完成。重启服务端后生效。");
    }

    // ---------- items (查物品模板) ----------

    static void ListItems(string[] args)
    {
        string type = null, cls = null;
        int min = 0, max = 60;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--type": type = args[++i]; break;
                case "--class": cls = args[++i]; break;
                case "--min": min = int.Parse(args[++i]); break;
                case "--max": max = int.Parse(args[++i]); break;
            }
        }
        Open();

        var items = Session.GetCollection<ItemInfo>().Binding.ToList();

        RequiredClass? req = cls switch
        {
            "Warrior" => RequiredClass.Warrior,
            "Wizard" => RequiredClass.Wizard,
            "Taoist" => RequiredClass.Taoist,
            "Assassin" => RequiredClass.Assassin,
            null => (RequiredClass?)null,
            _ => (RequiredClass?)Enum.Parse<RequiredClass>(cls),
        };

        foreach (var it in items)
        {
            if (type != null && !it.ItemType.ToString().Equals(type, StringComparison.OrdinalIgnoreCase)) continue;
            if (req != null && (it.RequiredClass & req.Value) == 0 && it.RequiredClass != RequiredClass.None && it.RequiredClass != RequiredClass.All) continue;
            int lvl = it.RequiredType == RequiredType.Level ? it.RequiredAmount : 0;
            if (lvl < min || lvl > max) continue;
            string stats = StatSummary(it);
            Console.WriteLine($"{it.ItemName,-40} {ItemTypeZh(it.ItemType),-6} 需求等级={lvl,-3} 职业={ReqClassZh(it.RequiredClass),-4} " +
                              $"价={it.Price,-8} Shape={it.Shape,-4} {stats} Index={it.Index}");
        }
    }

    static string StatSummary(ItemInfo it)
    {
        var parts = new List<string>();
        int ac = 0, mr = 0, dc = 0, mc = 0, sc = 0;
        foreach (var s in it.ItemStats)
        {
            switch (s.Stat)
            {
                case Stat.MinAC or Stat.MaxAC: ac += s.Amount; break;
                case Stat.MinMR or Stat.MaxMR: mr += s.Amount; break;
                case Stat.MinDC or Stat.MaxDC: dc += s.Amount; break;
                case Stat.MinMC or Stat.MaxMC: mc += s.Amount; break;
                case Stat.MinSC or Stat.MaxSC: sc += s.Amount; break;
                case Stat.Health: parts.Add($"HP+{s.Amount}"); break;
                case Stat.Mana: parts.Add($"MP+{s.Amount}"); break;
                case Stat.Accuracy: parts.Add($"命中+{s.Amount}"); break;
                case Stat.Agility: parts.Add($"敏捷+{s.Amount}"); break;
                case Stat.AttackSpeed: parts.Add($"攻速+{s.Amount}"); break;
                case Stat.Luck: parts.Add($"幸运+{s.Amount}"); break;
                case Stat.Strength: parts.Add($"力量+{s.Amount}"); break;
                default:
                    if (s.Amount != 0) parts.Add($"{s.Stat}+{s.Amount}");
                    break;
            }
        }
        var sb = new List<string>();
        if (ac != 0) sb.Add($"防{ac}");
        if (mr != 0) sb.Add($"魔防{mr}");
        if (dc != 0) sb.Add($"物攻{dc}");
        if (mc != 0) sb.Add($"魔攻{mc}");
        if (sc != 0) sb.Add($"道术{sc}");
        sb.AddRange(parts);
        return sb.Count > 0 ? string.Join(" ", sb) : "(无属性)";
    }

    static void ListMagics(string[] args)
    {
        string cls = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--class") cls = args[++i];
        Open();

        MirClass? mc = cls switch
        {
            "Warrior" => MirClass.Warrior,
            "Wizard" => MirClass.Wizard,
            "Taoist" => MirClass.Taoist,
            "Assassin" => MirClass.Assassin,
            null => (MirClass?)null,
            _ => (MirClass?)Enum.Parse<MirClass>(cls),
        };

        foreach (var m in Session.GetCollection<MagicInfo>().Binding
                     .Where(x => mc == null || x.Class == mc)
                     .OrderBy(x => x.NeedLevel1).ThenBy(x => x.Magic))
        {
            Console.WriteLine($"{m.Name,-28} Magic={m.Magic,-22} 职业={m.Class,-8} 需求等级={m.NeedLevel1}/{m.NeedLevel2}/{m.NeedLevel3} " +
                              $"耗蓝={m.BaseCost}+{m.LevelCost}/级 延迟={m.Delay}ms 经验={m.Experience1}/{m.Experience2}/{m.Experience3}");
        }
    }

    static void ListBaseStats(string[] args)
    {
        string cls = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--class") cls = args[++i];
        Open();

        MirClass? mc = cls switch
        {
            "Warrior" => MirClass.Warrior,
            "Wizard" => MirClass.Wizard,
            "Taoist" => MirClass.Taoist,
            "Assassin" => MirClass.Assassin,
            null => (MirClass?)null,
            _ => (MirClass?)Enum.Parse<MirClass>(cls),
        };

        var stats = Session.GetCollection<BaseStat>().Binding
            .Where(x => mc == null || x.Class == mc)
            .OrderBy(x => x.Level)
            .ToList();
        Console.WriteLine($"BaseStat 记录数(职业={mc?.ToString() ?? "全部"}): {stats.Count}, 等级范围: {stats.Min(x => x.Level)}-{stats.Max(x => x.Level)}");
        foreach (var b in stats)
            Console.WriteLine($"  Lv.{b.Level,-4} HP={b.Health,-6} MP={b.Mana}");
    }

    // ---------- boost ----------

    static void Boost(string[] args)
    {
        string charName = null;
        int? level = null;
        long? gold = null;
        bool skipItems = false, skipMagics = false;
        string className = null;
        var magicNames = new List<string>();
        bool allMagics = false;
        int? amuletCount = null;
        string weaponName = null;
        var binds = new List<(string name, string fkey)>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--char": charName = args[++i]; break;
                case "--level": level = int.Parse(args[++i]); break;
                case "--gold": gold = long.Parse(args[++i]); break;
                case "--no-items": skipItems = true; break;
                case "--no-magics": skipMagics = true; break;
                case "--magic": magicNames.Add(args[++i]); break;
                case "--all-magics": allMagics = true; break;
                case "--amulet-count": amuletCount = int.Parse(args[++i]); break;
                case "--bind":
                    {
                        var sp = args[++i].Split(':', 2);
                        if (sp.Length == 2) binds.Add((sp[0], sp[1]));
                    }
                    break;
            }
        }
        if (charName == null) { Console.WriteLine("需要 --char <角色名>"); return; }

        Open();

        var acc = Session.GetCollection<AccountInfo>().Binding
            .FirstOrDefault(a => a.Characters.Any(c => c.CharacterName.Equals(charName, StringComparison.OrdinalIgnoreCase)));
        if (acc == null) { Console.WriteLine($"找不到角色: {charName}"); return; }
        var ch = acc.Characters.First(c => c.CharacterName.Equals(charName, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"目标: {acc.EMailAddress} / {ch.CharacterName} ({ClassZh(ch.Class)}, Lv.{ch.Level})");

        // 0. 职业 (改职业时 HP/MP 上限要按新职业 BaseStat 重算, 无论是否同时改等级)
        if (className != null)
        {
            var mc = className switch
            {
                "Warrior" => MirClass.Warrior,
                "Wizard" => MirClass.Wizard,
                "Taoist" => MirClass.Taoist,
                "Assassin" => MirClass.Assassin,
                _ => (MirClass)Enum.Parse<MirClass>(className),
            };
            int newLevel = level ?? ch.Level;
            var bs = Session.GetCollection<BaseStat>().Binding
                .FirstOrDefault(b => b.Class == mc && b.Level == newLevel);
            Console.WriteLine($"  职业 {ClassZh(ch.Class)} -> {ClassZh(mc)} (Lv.{newLevel} BaseStat: HP={(bs?.Health ?? ch.CurrentHP)}, MP={(bs?.Mana ?? ch.CurrentMP)})");
            ch.Class = mc;
            if (bs != null) { ch.CurrentHP = bs.Health; ch.CurrentMP = bs.Mana; }
        }

        // 1. 等级 (+ 同步 HP/MP 到新等级满值)
        if (level != null)
        {
            var baseStat = Session.GetCollection<BaseStat>().Binding
                .FirstOrDefault(b => b.Class == ch.Class && b.Level == level.Value);
            if (baseStat != null)
            {
                Console.WriteLine($"  等级 {ch.Level} -> {level} (HP {ch.CurrentHP}->{baseStat.Health}, MP {ch.CurrentMP}->{baseStat.Mana})");
                ch.CurrentHP = baseStat.Health;
                ch.CurrentMP = baseStat.Mana;
            }
            else
            {
                Console.WriteLine($"  等级 {ch.Level} -> {level} (无 BaseStat 记录, HP/MP 保持原值)");
            }
            ch.Level = level.Value;
        }

        // 2. 金币
        if (gold != null)
        {
            var goldCur = acc.Currencies.FirstOrDefault(x => x.Info.Type == CurrencyType.Gold);
            if (goldCur == null)
            {
                var goldInfo = Session.GetCollection<CurrencyInfo>().Binding.First(x => x.Type == CurrencyType.Gold);
                goldCur = Session.GetCollection<UserCurrency>().CreateNewObject();
                goldCur.Info = goldInfo;
                goldCur.Account = acc;
                goldCur.Amount = gold.Value;
                acc.Currencies.Add(goldCur);
                Console.WriteLine($"  金币 新建 -> {gold}");
            }
            else
            {
                Console.WriteLine($"  金币 {goldCur.Amount} -> {gold}");
                goldCur.Amount = gold.Value;
            }
        }

        // 3. 装备 + 背包
        if (weaponName != null) SetWeapon(ch, weaponName);
        if (amuletCount != null) SetAmulet(ch, amuletCount.Value);
        if (!skipItems) BoostItems(ch);

        // 4. 技能
        if (!skipMagics)
            BoostMagics(ch, allMagics ? null : magicNames, allMagics: allMagics);
        // 5. 快捷键绑定 (绑到 Set1Key, 客户端 MagicBarSpellSet 默认 1)
        if (binds.Count > 0) BindMagics(ch, binds);

        Console.WriteLine("  保存中...");
        Session.Save(true);
        Console.WriteLine("  完成。重启服务端后生效。");

    // 把指定技能绑到 Set1Key 的 F1-F8 槽 (客户端 UseMagicKey: F1->Spell01..F8->Spell08)
    static void BindMagics(CharacterInfo ch, List<(string name, string fkey)> binds)
    {
        foreach (var (name, fkey) in binds)
        {
            int fk = fkey.ToUpperInvariant() switch
            {
                "F1" => 1, "F2" => 2, "F3" => 3, "F4" => 4,
                "F5" => 5, "F6" => 6, "F7" => 7, "F8" => 8,
                _ => -1,
            };
            if (fk < 0) { Console.WriteLine($"  绑定: 未知键位 {fkey}, 跳过"); continue; }
            var spell = (SpellKey)fk;  // Spell01=1..Spell08=8
            var um = ch.Magics.FirstOrDefault(m => m.Info.Name.Contains(name, StringComparison.OrdinalIgnoreCase)
                                                || m.Info.Magic.ToString().Contains(name, StringComparison.OrdinalIgnoreCase));
            if (um == null) { Console.WriteLine($"  绑定: 找不到技能 [{name}], 跳过"); continue; }
            Console.WriteLine($"  绑定 {um.Info.Name} -> Set1Key={spell} ({fkey})");
            um.Set1Key = spell;
        }
    }

    static RequiredClass ClassMask(MirClass c) => (RequiredClass)(1 << (int)c);

    // 强制替换武器槽(1000+0)为指定物品(名字子串, 不区分大小写)
    static void SetWeapon(CharacterInfo ch, string name)
    {
        var pick = Session.GetCollection<ItemInfo>().Binding
            .FirstOrDefault(x => x.ItemType == ItemType.Weapon &&
                                 x.ItemName.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (pick == null) { Console.WriteLine($"  武器: 找不到名字含 [{name}] 的武器"); return; }
        int slotIdx = 1000 + (int)EquipmentSlot.Weapon;
        var old = ch.Items.FirstOrDefault(x => x.Slot == slotIdx);
        if (old != null)
        {
            old.Info = pick; old.Count = 1;
            if (pick.Durability > 0) { old.CurrentDurability = pick.Durability; old.MaxDurability = pick.Durability; }
            Console.WriteLine($"  武器[{EquipmentSlot.Weapon}] {old.Info.ItemName} -> {pick.ItemName}");
        }
        else
        {
            var item = Session.GetCollection<UserItem>().CreateNewObject();
            item.Info = pick; item.Slot = slotIdx; item.Count = 1;
            if (pick.Durability > 0) { item.CurrentDurability = pick.Durability; item.MaxDurability = pick.Durability; }
            ch.Items.Add(item);
            Console.WriteLine($"  武器[{EquipmentSlot.Weapon}] += {pick.ItemName}");
        }
    }

    // Amulet 槽放普通护身符(Shape=0, 召唤魔法消耗要求)并设数量
    static void SetAmulet(CharacterInfo ch, int count)
    {
        var pick = Session.GetCollection<ItemInfo>().Binding
            .FirstOrDefault(x => x.ItemType == ItemType.Amulet && x.Shape == 0 &&
                                 (x.RequiredClass == RequiredClass.None || x.RequiredClass == RequiredClass.All || x.RequiredClass == RequiredClass.WarWizTao));
        if (pick == null) { Console.WriteLine($"  护身符: 找不到 Shape=0 的普通护身符"); return; }
        int slotIdx = 1000 + (int)EquipmentSlot.Amulet;
        var old = ch.Items.FirstOrDefault(x => x.Slot == slotIdx);
        if (old != null)
        {
            old.Info = pick; old.Count = count;
            Console.WriteLine($"  护身符[{EquipmentSlot.Amulet}] {old.Info.ItemName} -> {pick.ItemName} x{count} (Shape={pick.Shape})");
        }
        else
        {
            var item = Session.GetCollection<UserItem>().CreateNewObject();
            item.Info = pick; item.Slot = slotIdx; item.Count = count;
            ch.Items.Add(item);
            Console.WriteLine($"  护身符[{EquipmentSlot.Amulet}] += {pick.ItemName} x{count} (Shape={pick.Shape})");
        }
    }

    static void BoostItems(CharacterInfo ch)
    {
        var all = Session.GetCollection<ItemInfo>().Binding.ToList();
        var ownedSlots = ch.Items.Select(x => x.Slot).ToHashSet();
        int nextBag = Enumerable.Range(0, 48).FirstOrDefault(s => !ownedSlots.Contains(s));
        var mask = ClassMask(ch.Class);

        // 装备: 覆盖全部 17 个 EquipmentSlot (0..16)
        var equipPlan = new (ItemType type, EquipmentSlot slot)[]
        {
            (ItemType.Weapon, EquipmentSlot.Weapon),
            (ItemType.Armour, EquipmentSlot.Armour),
            (ItemType.Helmet, EquipmentSlot.Helmet),
            (ItemType.Torch, EquipmentSlot.Torch),
            (ItemType.Necklace, EquipmentSlot.Necklace),
            (ItemType.Bracelet, EquipmentSlot.BraceletL),
            (ItemType.Bracelet, EquipmentSlot.BraceletR),
            (ItemType.Ring, EquipmentSlot.RingL),
            (ItemType.Ring, EquipmentSlot.RingR),
            (ItemType.Shoes, EquipmentSlot.Shoes),
            (ItemType.Poison, EquipmentSlot.Poison),
            (ItemType.Amulet, EquipmentSlot.Amulet),
            (ItemType.Flower, EquipmentSlot.Flower),
            (ItemType.HorseArmour, EquipmentSlot.HorseArmour),
            (ItemType.Emblem, EquipmentSlot.Emblem),
            (ItemType.Shield, EquipmentSlot.Shield),
            (ItemType.Costume, EquipmentSlot.Costume),
        };

        foreach (var (type, slot) in equipPlan)
        {
            int slotIdx = 1000 + (int)slot;
            var existing = ch.Items.FirstOrDefault(x => x.Slot == slotIdx);

            var pool = all.Where(x => x.ItemType == type)
                          .Where(x => x.RequiredClass == RequiredClass.None || x.RequiredClass == RequiredClass.All || (x.RequiredClass & mask) != 0)
                          .Where(x => x.RequiredType != RequiredType.Level || x.RequiredAmount <= ch.Level)
                          .OrderByDescending(x => x.Price)
                          .ToList();
            if (pool.Count == 0)
                pool = all.Where(x => x.ItemType == type).OrderByDescending(x => x.Price).ToList();
            if (pool.Count == 0)
                pool = all.Where(x => x.ItemType == ItemType.Consumable || x.ItemType == ItemType.Book).OrderByDescending(x => x.Price).ToList();

            if (pool.Count == 0) continue;
            var pick = pool.First();
            if (existing != null)
            {
                existing.Info = pick;
                existing.Count = (type == ItemType.Amulet || type == ItemType.Poison) ? 200 : 1;
                if (pick.Durability > 0) { existing.CurrentDurability = pick.Durability; existing.MaxDurability = pick.Durability; }
                Console.WriteLine($"  装备[{EquipSlotZh((int)slot)}] {existing.Info.ItemName} -> {pick.ItemName}");
            }
            else
            {
                var item = Session.GetCollection<UserItem>().CreateNewObject();
                item.Info = pick;
                item.Slot = slotIdx;
                item.Count = (type == ItemType.Amulet || type == ItemType.Poison) ? 200 : 1;
                if (pick.Durability > 0) { item.CurrentDurability = pick.Durability; item.MaxDurability = pick.Durability; }
                ch.Items.Add(item);
                ownedSlots.Add(slotIdx);
                Console.WriteLine($"  装备[{EquipSlotZh((int)slot)}] += {pick.ItemName} (价={pick.Price}, {ReqClassZh(pick.RequiredClass)})");
            }
        }

        // 背包: 填满全部 48 个格子 (0..47)
        var sampleItems = all.Where(x => x.ItemType == ItemType.Consumable || x.ItemType == ItemType.Scroll || x.ItemType == ItemType.Book || x.ItemType == ItemType.Ore || x.ItemType == ItemType.Ring)
                             .Take(48).ToList();
        if (sampleItems.Count == 0) sampleItems = all.Take(48).ToList();

        for (int bagSlot = 0; bagSlot < 48; bagSlot++)
        {
            var existing = ch.Items.FirstOrDefault(x => x.Slot == bagSlot);
            if (existing != null) continue;
            var pick = sampleItems[bagSlot % sampleItems.Count];
            var item = Session.GetCollection<UserItem>().CreateNewObject();
            item.Info = pick;
            item.Slot = bagSlot;
            item.Count = pick.ItemType == ItemType.Consumable || pick.ItemType == ItemType.Scroll ? 100 : 1;
            if (pick.Durability > 0) { item.CurrentDurability = pick.Durability; item.MaxDurability = pick.Durability; }
            ch.Items.Add(item);
            Console.WriteLine($"  背包[{bagSlot}] += {pick.ItemName}");
        }
    }
    static void BoostMagics(CharacterInfo ch, List<string> names = null, bool allMagics = false)
    {
        var magics = Session.GetCollection<MagicInfo>().Binding;
        // --all-magics: 加全部 174 个; 给了 --magic 就只加指定的; 否则按职业全套
        List<MagicInfo> classMagics;
        if (allMagics)
            classMagics = magics.ToList();
        else if (names != null && names.Count > 0)
            classMagics = magics.Where(x => names.Any(n => x.Name.Contains(n, StringComparison.OrdinalIgnoreCase)
                                            || x.Magic.ToString().Contains(n, StringComparison.OrdinalIgnoreCase))).ToList();
        else
            classMagics = magics.Where(x => x.Class == ch.Class).ToList();

        var have = ch.Magics.Select(x => x.Info.Magic).ToHashSet();
        foreach (var m in classMagics.OrderBy(x => x.NeedLevel1))
        {
            if (have.Contains(m.Magic)) { Console.WriteLine($"  技能 {m.Name} 已有, 跳过"); continue; }
            var um = Session.GetCollection<UserMagic>().CreateNewObject();
            um.Info = m; um.Level = 3; um.Experience = 1000000;
            ch.Magics.Add(um);
            Console.WriteLine($"  技能 += {m.Name} (Magic={m.Magic}, Lv.3)");
            have.Add(m.Magic);
        }
    }
}
}
