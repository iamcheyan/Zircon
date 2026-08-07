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

        if (cmd == "list") { List(rest); return; }
        if (cmd == "boost") { Boost(rest); return; }
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
                                      $"耐久 {item.CurrentDurability}/{item.MaxDurability}  Level={item.Level}");
                }
            }
        }
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
        int? amuletCount = null;
        string weaponName = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--char": charName = args[++i]; break;
                case "--level": level = int.Parse(args[++i]); break;
                case "--gold": gold = long.Parse(args[++i]); break;
                case "--no-items": skipItems = true; break;
                case "--no-magics": skipMagics = true; break;
                case "--class": className = args[++i]; break;
                case "--magic": magicNames.Add(args[++i]); break;
                case "--amulet-count": amuletCount = int.Parse(args[++i]); break;
                case "--weapon": weaponName = args[++i]; break;
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
        if (!skipMagics) BoostMagics(ch, magicNames);

        Console.WriteLine("  保存中...");
        Session.Save(true);
        Console.WriteLine("  完成。重启服务端后生效。");
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

        // 装备: 按价格挑"最强"的(无等级概念时价格即强度), 放装备槽
        var equipPlan = new (ItemType type, EquipmentSlot slot)[]
        {
            (ItemType.Weapon, EquipmentSlot.Weapon),
            (ItemType.Armour, EquipmentSlot.Armour),
            (ItemType.Helmet, EquipmentSlot.Helmet),
            (ItemType.Necklace, EquipmentSlot.Necklace),
            (ItemType.Bracelet, EquipmentSlot.BraceletL),
            (ItemType.Bracelet, EquipmentSlot.BraceletR),
            (ItemType.Ring, EquipmentSlot.RingL),
            (ItemType.Ring, EquipmentSlot.RingR),
            (ItemType.Shoes, EquipmentSlot.Shoes),
            (ItemType.Amulet, EquipmentSlot.Amulet),
        };

        foreach (var (type, slot) in equipPlan)
        {
            int slotIdx = 1000 + (int)slot;
            if (ownedSlots.Contains(slotIdx)) { Console.WriteLine($"  装备[{slot}] 已有物品, 跳过"); continue; }

            var pool = all.Where(x => x.ItemType == type)
                          .Where(x => x.RequiredClass == RequiredClass.None || x.RequiredClass == RequiredClass.All || (x.RequiredClass & mask) != 0)
                          .Where(x => x.RequiredType != RequiredType.Level || x.RequiredAmount <= ch.Level)
                          .OrderByDescending(x => x.Price)
                          .ToList();
            if (pool.Count == 0) { Console.WriteLine($"  装备[{slot}] 无匹配物品, 跳过"); continue; }
            var pick = pool.First();
            var item = Session.GetCollection<UserItem>().CreateNewObject();
            item.Info = pick;
            item.Slot = slotIdx;
            item.Count = 1;
            if (pick.Durability > 0) { item.CurrentDurability = pick.Durability; item.MaxDurability = pick.Durability; }
            ch.Items.Add(item);
            ownedSlots.Add(slotIdx);
            Console.WriteLine($"  装备[{EquipSlotZh((int)slot)}] += {pick.ItemName} (价={pick.Price}, {ReqClassZh(pick.RequiredClass)})");
        }

        // 背包: 大血瓶 + 大蓝瓶 + 回城卷
        var heal = all.Where(x => x.ItemType == ItemType.Consumable && x.ItemStats.Any(s => s.Stat == Stat.Health) && x.Price > 1000)
                      .OrderByDescending(x => x.Price).FirstOrDefault();
        if (heal == null) heal = all.FirstOrDefault(x => x.ItemType == ItemType.Consumable && x.ItemStats.Any(s => s.Stat == Stat.Health));
        if (heal != null && !ownedSlots.Contains(nextBag))
        {
            var healItem = Session.GetCollection<UserItem>().CreateNewObject();
            healItem.Info = heal; healItem.Slot = nextBag; healItem.Count = 100;
            ch.Items.Add(healItem);
            Console.WriteLine($"  背包[{nextBag}] += {heal.ItemName} x100");
            ownedSlots.Add(nextBag);
            nextBag = Enumerable.Range(0, 48).FirstOrDefault(s => !ownedSlots.Contains(s));
        }
        var mana = all.Where(x => x.ItemType == ItemType.Consumable && x.ItemStats.Any(s => s.Stat == Stat.Mana) && x.Price > 1000)
                      .OrderByDescending(x => x.Price).FirstOrDefault();
        if (mana == null) mana = all.FirstOrDefault(x => x.ItemType == ItemType.Consumable && x.ItemStats.Any(s => s.Stat == Stat.Mana));
        if (mana != null && !ownedSlots.Contains(nextBag))
        {
            var manaItem = Session.GetCollection<UserItem>().CreateNewObject();
            manaItem.Info = mana; manaItem.Slot = nextBag; manaItem.Count = 100;
            ch.Items.Add(manaItem);
            Console.WriteLine($"  背包[{nextBag}] += {mana.ItemName} x100");
            ownedSlots.Add(nextBag);
            nextBag = Enumerable.Range(0, 48).FirstOrDefault(s => !ownedSlots.Contains(s));
        }
        var town = all.FirstOrDefault(x => x.ItemName == "Scroll Of Town Portal");
        if (town != null && !ownedSlots.Contains(nextBag))
        {
            var townItem = Session.GetCollection<UserItem>().CreateNewObject();
            townItem.Info = town; townItem.Slot = nextBag; townItem.Count = 20;
            ch.Items.Add(townItem);
            Console.WriteLine($"  背包[{nextBag}] += {town.ItemName} x20");
        }
    }

    static void BoostMagics(CharacterInfo ch, List<string> names = null)
    {
        var magics = Session.GetCollection<MagicInfo>().Binding;
        // 给了 --magic 就只加指定的; 否则按职业全套
        var classMagics = names != null && names.Count > 0
            ? magics.Where(x => names.Any(n => x.Name.Contains(n, StringComparison.OrdinalIgnoreCase)
                                            || x.Magic.ToString().Contains(n, StringComparison.OrdinalIgnoreCase))).ToList()
            : magics.Where(x => x.Class == ch.Class).ToList();

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
