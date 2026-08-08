using System.Drawing;
using Library;
using Library.MirDB;
using Library.SystemModels;
using MirDB;
using Server.DBModels;
using Server.Envir;

// Creates persistent, ordinary server accounts/characters for BotRunner.
// The tool is intentionally idempotent: existing bot accounts are left intact.
static class Program
{
    static Session Session;
    static string Root;
    static string Prefix = "bot";
    static string Password = "bot123456";
    static string ReferenceName;
    static int Count = 20;
    static bool DryRun;
    static bool ResetPositions;
    static bool ResourceReport;
    static bool SeedReference;
    static bool FindShuriken;

    static int Main(string[] args)
    {
        if (args.Length == 0) { Usage(); return 2; }
        Root = Path.GetFullPath(args[0]);
        if (!Root.EndsWith(Path.DirectorySeparatorChar)) Root += Path.DirectorySeparatorChar;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--prefix": Prefix = args[++i]; break;
                case "--password": Password = args[++i]; break;
                case "--count": Count = Math.Clamp(int.Parse(args[++i]), 1, 20); break;
                case "--reference": ReferenceName = args[++i]; break;
                case "--dry-run": DryRun = true; break;
                case "--reset-positions": ResetPositions = true; break;
                case "--seed-reference": SeedReference = true; break;
                case "--resource-report": ResourceReport = true; break;
                case "--find-shuriken": FindShuriken = true; break;
                case "--help": Usage(); return 0;
                default: Console.Error.WriteLine($"未知参数: {args[i]}"); return 2;
            }
        }

        Session = new Session(SessionMode.Users, Root);
        Session.Initialize(typeof(ItemInfo).Assembly, typeof(AccountInfo).Assembly);
        // AccountInfo.OnCreated creates its default hunt-gold buff and currencies
        // through SEnvir's runtime collection handles. The server normally wires
        // these in LoadDatabase(); this standalone provisioning tool must do the
        // small subset explicitly before creating new accounts.
        SEnvir.Session = Session;
        SEnvir.CurrencyInfoList = Session.GetCollection<CurrencyInfo>();
        SEnvir.UserCurrencyList = Session.GetCollection<UserCurrency>();
        SEnvir.BuffInfoList = Session.GetCollection<BuffInfo>();
        // 行会重置需要 SEnvir 集合句柄 (服务端 LoadDatabase 486-487 同样接线)
        SEnvir.GuildInfoList = Session.GetCollection<GuildInfo>();
        SEnvir.GuildMemberInfoList = Session.GetCollection<GuildMemberInfo>();

        if (ResourceReport)
        {
            var items = Session.GetCollection<ItemInfo>().Binding;
            Console.WriteLine("资源工具:");
            foreach (var item in items.Where(x => x.ItemEffect is ItemEffect.PickAxe or ItemEffect.FishingRod or ItemEffect.FishingRobe)
                .OrderBy(x => x.ItemEffect).ThenBy(x => x.ItemName))
                Console.WriteLine($"  {item.ItemEffect}: {item.ItemName} index={item.Index} type={item.ItemType} durability={item.Durability}");

            Console.WriteLine("可采矿地图:");
            foreach (var map in Session.GetCollection<MapInfo>().Binding.Where(x => x.CanMine))
                Console.WriteLine($"  map={map.Index} {map.Description} file={map.FileName} mining={map.Mining?.Count ?? 0} points={map.Mining?.Sum(x => x.Region?.PointRegion?.Length ?? 0) ?? 0}");
            foreach (var map in Session.GetCollection<MapInfo>().Binding.Where(x => x.CanMine))
                foreach (var mine in map.Mining)
                    Console.WriteLine($"    map={map.Index} mine={mine.Item?.ItemName} region={mine.Region?.Description} type={mine.Region?.RegionType} size={mine.Region?.Size} bits={mine.Region?.BitRegion?.Length ?? 0}");

            Console.WriteLine("钓鱼区域:");
            foreach (var fishing in Session.GetCollection<FishingInfo>().Binding)
                Console.WriteLine($"  {fishing.Name}: map={fishing.Region?.Map?.Index} region={fishing.Region?.Description} drops={fishing.Drops?.Count ?? 0}");

            Console.WriteLine("采矿地图 NPC:");
            foreach (var npc in Session.GetCollection<NPCInfo>().Binding
                .Where(x => x.Region?.Map?.CanMine == true)
                .OrderBy(x => x.Region.Map.Index).ThenBy(x => x.Index))
                Console.WriteLine($"  map={npc.Region.Map.Index} npc={npc.Index} {npc.NPCName}");

            Console.WriteLine("NPC 商店页面:");
            foreach (var npc in Session.GetCollection<NPCInfo>().Binding
                .Where(x => x.Region?.Map?.Index == 1 && x.EntryPage != null)
                .OrderBy(x => x.NPCName))
            {
                var pages = new[] { npc.EntryPage }
                    .Concat(npc.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>())
                    .Distinct();
                foreach (var page in pages)
                    Console.WriteLine($"  {npc.NPCName}: page={page.DialogType} goods={page.Goods?.Count ?? 0} types={page.Types?.Count ?? 0} goodsIndex={npc.GoodsIndex} items={string.Join(",", page.Goods?.Where(g => g.Item != null).Select(g => g.Item.ItemName) ?? Enumerable.Empty<string>())}");
            }

            var safeZones = Session.GetCollection<SafeZoneInfo>().Binding.ToList();
            Console.WriteLine($"安全区区域 (SafeZoneInfo) 总数={safeZones.Count}:");
            foreach (var sz in safeZones.OrderBy(x => x.Region?.Map?.Index ?? 9999).ThenBy(x => x.Region?.Description))
            {
                string regionDesc = sz.Region == null ? "(null)" : $"{sz.Region.Description ?? ""} map={sz.Region.Map?.Index} bit={sz.Region.BitRegion?.Length} pts={sz.Region.PointRegion?.Length}";
                Console.WriteLine($"  region={regionDesc} bind={sz.BindRegion?.Description} red={sz.RedZone} border={sz.Border}");
                if (sz.Region?.PointRegion != null)
                    foreach (var p in sz.Region.PointRegion.Take(3))
                        Console.WriteLine($"    point=({p.X},{p.Y})");
            }

            Console.WriteLine("伙伴食物 (ItemType.CompanionFood):");
            foreach (var food in Session.GetCollection<ItemInfo>().Binding
                .Where(x => x.ItemType == ItemType.CompanionFood)
                .OrderBy(x => x.ItemName))
                Console.WriteLine($"  {food.ItemName} index={food.Index} hungerStat={food.Stats[Stat.CompanionHunger]} stack={food.StackSize} dur={food.Durability} shape={food.Shape}");

            Console.WriteLine("伙伴等级配置:");
            foreach (var lv in Session.GetCollection<CompanionLevelInfo>().Binding.OrderBy(x => x.Level))
                Console.WriteLine($"  Lv{lv.Level} inventory={lv.InventorySpace} weight={lv.InventoryWeight} maxHunger={lv.MaxHunger}");

            Console.WriteLine($"宠物配置: companions={Session.GetCollection<CompanionInfo>().Binding.Count}, unlocks={Session.GetCollection<UserCompanionUnlock>().Binding.Count}, users={Session.GetCollection<UserCompanion>().Binding.Count}");
            Console.WriteLine($"副本配置: instances={Session.GetCollection<InstanceInfo>().Binding.Count}, instanceMaps={Session.GetCollection<InstanceMapInfo>().Binding.Count}");
            Console.WriteLine($"礼包配置: bundles={Session.GetCollection<BundleInfo>().Binding.Count}, lootBoxes={Session.GetCollection<LootBoxInfo>().Binding.Count}");
            Console.WriteLine($"市场记录: auctions={Session.GetCollection<AuctionInfo>().Binding.Count}");
            return 0;
        }
        if (FindShuriken)
        {
            var items = Session.GetCollection<ItemInfo>().Binding;
            Console.WriteLine("形状 33 武器 (Shuriken 类):");
            foreach (var item in items.Where(x => x.ItemType == ItemType.Weapon && x.Shape == 33)
                .OrderBy(x => x.Index))
                Console.WriteLine($"  index={item.Index} {item.ItemName} class={item.RequiredClass} level={item.RequiredAmount}");
            if (!items.Any(x => x.ItemType == ItemType.Weapon && x.Shape == 33))
                Console.WriteLine("  (无)");
            Console.WriteLine("武器形状分布:");
            foreach (var g in items.Where(x => x.ItemType == ItemType.Weapon).GroupBy(x => x.Shape).OrderBy(x => x.Key))
                Console.WriteLine($"  shape={g.Key} count={g.Count()} 例: {string.Join(", ", g.Take(3).Select(x => x.ItemName))}");
            Console.WriteLine("地图 1 怪物刷新点 (TestHero 在 122,256):");
            foreach (var spawn in Session.GetCollection<Library.SystemModels.RespawnInfo>().Binding.Where(x => x.Region?.Map?.Index == 1))
                Console.WriteLine($"  {spawn.Monster?.MonsterName} index={spawn.Monster?.Index} region={spawn.Region?.Description} size={spawn.Region?.Size} count={spawn.Count}");
            Console.WriteLine("地图 1 怪物属性:");
            foreach (var m in Session.GetCollection<MonsterInfo>().Binding.Where(x => x.Respawns?.Any(r => r.Region?.Map?.Index == 1) == true).OrderBy(x => x.Index))
            {
                var st = m.MonsterInfoStats.ToDictionary(x => x.Stat, x => x.Amount);
                st.TryGetValue(Stat.Health, out int hp);
                st.TryGetValue(Stat.MinDC, out int minDc);
                st.TryGetValue(Stat.MaxDC, out int maxDc);
                Console.WriteLine($"  {m.MonsterName} index={m.Index} Lv={m.Level} HP={hp} DC={minDc}-{maxDc} ai={m.AI}");
            }
            var hero = Session.GetCollection<CharacterInfo>().Binding.FirstOrDefault(x => x.CharacterName == ReferenceName);
            if (hero != null)
                Console.WriteLine($"TestHero: map={hero.CurrentMap?.Index} loc={hero.CurrentLocation} class={hero.Class} level={hero.Level}");
            return 0;
        }
        var accounts = Session.GetCollection<AccountInfo>().Binding;
        var characters = Session.GetCollection<CharacterInfo>().Binding;
        var accountCollection = Session.GetCollection<AccountInfo>();
        var characterCollection = Session.GetCollection<CharacterInfo>();
        var reference = FindReference(accounts);
        if (reference == null)
        {
            if (DryRun)
            {
                Console.WriteLine($"账号数={accounts.Count}, 角色数={characters.Count}");
                foreach (var account in accounts) Console.WriteLine($"账号 {account.EMailAddress}, 角色: {string.Join(", ", account.Characters.Select(x => x.CharacterName))}");
                return 0;
            }
            Console.Error.WriteLine("找不到参考角色。请使用 --reference 角色名，或先创建测试角色。");
            return 1;
        }

        Console.WriteLine($"数据库: {Session.UsersPath}");
        Console.WriteLine($"参考角色: {reference.CharacterName}, 地图={reference.CurrentMap?.Index}, 坐标={reference.CurrentLocation}");
        if (SeedReference)
        {
            // 给参考角色 (TestHero) 施加与机器人相同的固定种子装备/消耗品/魔法，
            // 保证实服操作审计每次都能拿到确定的戒指/手镯/自动药水。
            // 规范重置: 先删除种子管理的物品/魔法再重新播种, 避免旧装备腾挪进背包
            // 累积、重复槽位 (服务端按 Slot 绑定, 同槽双物品会歧义)。
            int wiped = 0;
            foreach (var item in reference.Items.ToList())
            {
                if (item.Info?.ItemType == ItemType.Torch) { reference.Items.Remove(item); item.Delete(); wiped++; }
                else if (item.Slot >= Globals.EquipmentOffSet) { reference.Items.Remove(item); item.Delete(); wiped++; }
                else if (item.Info != null && item.Info.CanAutoPot && item.Info.ItemType == ItemType.Consumable) { reference.Items.Remove(item); item.Delete(); wiped++; }
                // 伙伴食物 (CanAutoPot=false) 也要清, 否则每次播种新发的食物会与残留堆叠槽位歧义
                else if (item.Info?.ItemType == ItemType.CompanionFood) { reference.Items.Remove(item); item.Delete(); wiped++; }
            }
            int wipedMagics = reference.Magics.Count;
            foreach (var magic in reference.Magics.ToList()) magic.Delete();
            reference.Magics.Clear();
            // 清空邮箱: 审计 S13-S15 依赖确定的邮件基线 (残留邮件会污染 mailCountBefore)
            int wipedMail = reference.Account.Mail.Count;
            foreach (var mail in reference.Account.Mail.ToList())
            {
                foreach (var item in mail.Items.ToList())
                {
                    mail.Items.Remove(item);
                    item.Delete();
                }
                reference.Account.Mail.Remove(mail);
                mail.Delete();
            }
            // 建会费 7.5M (Globals.GuildCreationCost); 审计前金币基线须足够
            SeedCurrency(reference.Account, 50_000_000);
            SeedEquipment(reference, reference.Class, reference.Level, 17);
            EnsureTorch(reference);
            EnsureClassSupplies(reference);
            SeedConsumables(reference, 1);
            SeedMagics(reference, reference.Class, reference.Level);
            // C6/E3 实服端到端种子: 专属伙伴 (Lv2 才有 1 格背包) + 伙伴食物 x10 +
            // 行会重置 + 安全区出生点 (每次强制, 防 S16 走位把存点移出安全区)
            bool companionSeeded = SeedTestHeroCompanion(reference);
            int foodCount = SeedCompanionFood(reference);
            bool guildReset = ResetGuild(reference.Account);
            ResetTestHeroPosition(reference);
            var equipmentSlots = new[] { EquipmentSlot.Weapon, EquipmentSlot.Armour, EquipmentSlot.Helmet,
                EquipmentSlot.Necklace, EquipmentSlot.BraceletL, EquipmentSlot.BraceletR,
                EquipmentSlot.RingL, EquipmentSlot.RingR, EquipmentSlot.Shoes };
            var equipped = string.Join(", ", equipmentSlots
                .Select(s => (Slot: s, Item: reference.Items.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + (int)s)))
                .Where(x => x.Item?.Info != null)
                .Select(x => $"{x.Slot}={x.Item.Info.ItemName}"));
            var potions = reference.Items.Where(x => x.Info?.CanAutoPot == true).Select(x => $"{x.Info.ItemName}x{x.Count}").ToList();
            Session.Save(true);
            Console.WriteLine($"[seed] {reference.CharacterName} {reference.Class}/{reference.Gender} Lv.{reference.Level} 清理 {wiped} 物品/{wipedMagics} 魔法/{wipedMail} 邮件");
            Console.WriteLine($"[seed] 装备: {equipped}");
            Console.WriteLine($"[seed] 自动药水: {string.Join(", ", potions)}");
            Console.WriteLine($"[seed] 伙伴: {(companionSeeded ? $"{reference.Companion?.Name ?? "?"} Lv.{reference.Companion?.Level} Hunger={reference.Companion?.Hunger}" : "未播种")}");
            Console.WriteLine($"[seed] 伙伴食物: {foodCount} 个");
            Console.WriteLine($"[seed] 行会重置: {(guildReset ? "已删除旧行会成员/行会" : "无旧行会")}");
            Console.WriteLine($"[seed] 出生点: map={reference.CurrentMap?.Index} loc={reference.CurrentLocation}");
            Console.WriteLine("完成: 参考角色播种完毕。");
            return 0;
        }
        if (DryRun)
        {
            Console.WriteLine($"账号数={accounts.Count}, 角色数={characters.Count}, 物品模板={Session.GetCollection<ItemInfo>().Binding.Count}, 魔法模板={Session.GetCollection<MagicInfo>().Binding.Count}");
            var bots = accounts.Where(x => x.EMailAddress?.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true)
                .SelectMany(x => x.Characters.Select(c => (Account: x, Character: c))).ToList();
            Console.WriteLine($"机器人账号={bots.Select(x => x.Account).Distinct().Count()}, 机器人角色={bots.Count}");
            foreach (var (_, character) in bots.OrderBy(x => x.Character.CharacterName))
            {
                var torch = character.Items.FirstOrDefault(x => x.Info?.ItemType == ItemType.Torch);
                Console.WriteLine($"  {character.CharacterName}: {character.Class}/{character.Gender} Lv.{character.Level} horse={character.Account?.Horse} map={character.CurrentMap?.Index} loc={character.CurrentLocation} items={character.Items.Count} magics={character.Magics.Count} torch={(torch == null ? "无" : $"{torch.Info.ItemName}@{torch.Slot}")}");
            }
            return 0;
        }
        var made = 0;
        var updated = 0;
        for (int i = 1; i <= Count; i++)
        {
            string email = $"{Prefix}{i:00}@bot.local";
            var existingAccount = accounts.FirstOrDefault(x => string.Equals(x.EMailAddress, email, StringComparison.OrdinalIgnoreCase));
            if (existingAccount != null)
            {
                var existingCharacter = existingAccount.Characters.FirstOrDefault();
                if (existingCharacter != null)
                {
                    if (ResetPositions)
                    {
                        existingCharacter.CurrentMap = reference.CurrentMap;
                        existingCharacter.CurrentInstance = reference.CurrentInstance;
                        existingCharacter.BindPoint = reference.BindPoint;
                        existingCharacter.CurrentLocation = Offset(reference.CurrentLocation, i);
                        existingCharacter.Direction = (MirDirection)(i % 8);
                    }
                    if (EnsureTorch(existingCharacter)) updated++;
                    if (EnsureClassSupplies(existingCharacter)) updated++;
                    if (EnsureClassMagics(existingCharacter)) updated++;
                    if (EnsureEquipmentMatches(existingCharacter)) updated++;
                    if (EnsureCurrency(existingAccount, i)) updated++;
                    if (i % 5 == 0 && EnsureMiningTool(existingCharacter)) updated++;
                    if (EnsureHorse(existingAccount, i)) updated++;
                    if (EnsureCompanion(existingAccount, existingCharacter, i)) updated++;
                }
                Console.WriteLine($"[skip] {email} 已存在");
                continue;
            }
            var account = accountCollection.CreateNewObject();
            account.EMailAddress = email;
            account.Password = SEnvir.CreateHash(Password);
            account.RealName = $"Zircon Bot {i:00}";
            account.BirthDate = new DateTime(1990 + i % 15, 1 + i % 12, 1 + i % 27);
            account.CreationIP = "127.0.0.1";
            account.CreationDate = DateTime.UtcNow;
            account.Activated = true;
            account.AllowGroup = true;
            account.AllowTrade = true;
            account.AllowGuild = true;

            var cls = (MirClass)((i - 1) % 4);
            var gender = i % 2 == 0 ? MirGender.Female : MirGender.Male;
            int level = new[] { 18, 25, 32, 40, 48, 55, 62, 70 }[(i - 1) % 8];
            var character = characterCollection.CreateNewObject();
            character.Account = account;
            character.CharacterName = $"Bot{i:00}";
            character.Class = cls;
            character.Gender = gender;
            character.Level = level;
            character.CreationIP = "127.0.0.1";
            character.CreationDate = DateTime.UtcNow;
            character.HairType = gender == MirGender.Male ? 1 + i % 10 : 1 + i % 11;
            character.HairColour = HairColours[i % HairColours.Length];
            character.ArmourColour = ArmourColours[i % ArmourColours.Length];
            character.CurrentMap = reference.CurrentMap;
            character.CurrentInstance = reference.CurrentInstance;
            character.BindPoint = reference.BindPoint;
            character.CurrentLocation = Offset(reference.CurrentLocation, i);
            character.Direction = (MirDirection)(i % 8);
            var baseStat = Session.GetCollection<BaseStat>().Binding.FirstOrDefault(x => x.Class == cls && x.Level == level);
            character.CurrentHP = baseStat?.Health ?? 100;
            character.CurrentMP = baseStat?.Mana ?? 50;
            character.Experience = 0;
            SeedCurrency(account, 1_000_000 + i * 100_000);
            SeedEquipment(character, cls, level, i);
            EnsureTorch(character);
            EnsureClassSupplies(character);
            if (i % 5 == 0) EnsureMiningTool(character);
            EnsureHorse(account, i);
            EnsureCompanion(account, character, i);
            SeedConsumables(character, i);
            SeedMagics(character, cls, level);
            made++;
            Console.WriteLine($"[make] {email} / {character.CharacterName} {cls} {gender} Lv.{level} map={character.CurrentMap?.Index}");
        }

        Session.Save(true);
        Console.WriteLine($"完成: 新建 {made} 个机器人账号，补充/装备照明物品 {updated} 个。");
        return 0;
    }

    static CharacterInfo FindReference(IEnumerable<AccountInfo> accounts)
    {
        if (!string.IsNullOrWhiteSpace(ReferenceName))
            return accounts.SelectMany(x => x.Characters).FirstOrDefault(x => x.CharacterName.Equals(ReferenceName, StringComparison.OrdinalIgnoreCase));
        return accounts.SelectMany(x => x.Characters).FirstOrDefault(x => x.CharacterName.Equals("TestHero", StringComparison.OrdinalIgnoreCase))
            ?? accounts.SelectMany(x => x.Characters).FirstOrDefault();
    }

    static void SeedCurrency(AccountInfo account, long amount)
    {
        var info = Session.GetCollection<CurrencyInfo>().Binding.FirstOrDefault(x => x.Type == CurrencyType.Gold);
        if (info == null) return;
        var currency = account.Currencies.FirstOrDefault(x => x.Info == info) ?? Session.GetCollection<UserCurrency>().CreateNewObject();
        currency.Info = info; currency.Account = account; currency.Amount = amount;
        if (!account.Currencies.Contains(currency)) account.Currencies.Add(currency);
    }

    static bool EnsureCurrency(AccountInfo account, int index)
    {
        var info = Session.GetCollection<CurrencyInfo>().Binding.FirstOrDefault(x => x.Type == CurrencyType.Gold);
        if (info == null) return false;
        var currency = account.Currencies.FirstOrDefault(x => x.Info == info) ?? Session.GetCollection<UserCurrency>().CreateNewObject();
        currency.Info = info;
        currency.Account = account;
        long minimum = 250_000 + index * 25_000L;
        if (currency.Amount >= minimum)
        {
            if (!account.Currencies.Contains(currency)) account.Currencies.Add(currency);
            return false;
        }
        currency.Amount = minimum;
        if (!account.Currencies.Contains(currency)) account.Currencies.Add(currency);
        return true;
    }

    static void SeedEquipment(CharacterInfo character, MirClass cls, int level, int salt)
    {
        var genderFlag = character.Gender == MirGender.Male ? RequiredGender.Male : RequiredGender.Female;
        var types = new[] { (ItemType.Weapon, EquipmentSlot.Weapon), (ItemType.Armour, EquipmentSlot.Armour),
            (ItemType.Helmet, EquipmentSlot.Helmet), (ItemType.Necklace, EquipmentSlot.Necklace),
            (ItemType.Bracelet, EquipmentSlot.BraceletL), (ItemType.Bracelet, EquipmentSlot.BraceletR),
            (ItemType.Ring, EquipmentSlot.RingL), (ItemType.Ring, EquipmentSlot.RingR),
            (ItemType.Shoes, EquipmentSlot.Shoes) };
        foreach (var (type, slot) in types)
        {
            // 性别必须匹配：衣服类装备在数据库中严格区分男女款(RequiredGender 非 None)，
            // 服务端 CanUseItem/CanStartWith 会拒绝性别不符的穿戴，种子若随机到异性款
            // 角色就会穿着异性衣服进游戏。
            var all = Session.GetCollection<ItemInfo>().Binding.Where(x => x.ItemType == type && (x.RequiredType != RequiredType.Level || x.RequiredAmount <= level))
                .Where(x => Compatible(x.RequiredClass, cls))
                .Where(x => x.RequiredGender == RequiredGender.None || (x.RequiredGender & genderFlag) != 0)
                .OrderBy(x => x.RequiredType == RequiredType.Level ? x.RequiredAmount : 0).ToList();
            if (all.Count == 0) continue;
            // 武器/衣服优先职业专属款（RequiredClass 精确等于本职业），没有等级可用的
            // 专属款才退回 WarWizTao/All 等通用款，保证机器人拿的是本职业特色装备。
            var classFlag = (RequiredClass)(1 << (int)cls);
            var exact = all.Where(x => x.RequiredClass == classFlag).ToList();
            var candidates = exact.Count > 0 ? exact : all;
            var info = candidates[Math.Min(candidates.Count - 1, salt % Math.Max(1, candidates.Count))];
            AddItem(character, info, 1000 + (int)slot, 1);
        }
    }

    static void SeedConsumables(CharacterInfo character, int salt)
    {
        var items = Session.GetCollection<ItemInfo>().Binding
            .Where(x => x.ItemType == ItemType.Consumable && x.CanAutoPot)
            .Take(3).ToList();
        if (items.Count == 0)
            items = Session.GetCollection<ItemInfo>().Binding.Where(x => x.ItemType == ItemType.Consumable).Take(3).ToList();
        for (int i = 0; i < items.Count; i++) AddItem(character, items[(i + salt) % items.Count], i, 30 + salt * 2);
    }

    static bool EnsureTorch(CharacterInfo character)
    {
        var torchInfo = Session.GetCollection<ItemInfo>().Binding
            .Where(x => x.ItemType == ItemType.Torch)
            .OrderBy(x => x.RequiredType == RequiredType.Level ? x.RequiredAmount : 0)
            .ThenBy(x => x.ItemName)
            .FirstOrDefault();
        if (torchInfo == null)
        {
            Console.WriteLine($"[warn] 找不到 ItemType.Torch，无法给 {character.CharacterName} 配置照明物品");
            return false;
        }

        int equipmentSlot = Globals.EquipmentOffSet + (int)EquipmentSlot.Torch;
        var torch = character.Items.FirstOrDefault(x => x.Info?.ItemType == ItemType.Torch);
        if (torch == null)
        {
            torch = Session.GetCollection<UserItem>().CreateNewObject();
            torch.Info = torchInfo;
            torch.Count = 1;
            torch.CurrentDurability = torchInfo.Durability;
            torch.MaxDurability = torchInfo.Durability;
            character.Items.Add(torch);
        }
        else if (torch.Info != torchInfo)
        {
            torch.Info = torchInfo;
            torch.CurrentDurability = torchInfo.Durability;
            torch.MaxDurability = torchInfo.Durability;
        }

        var occupant = character.Items.FirstOrDefault(x => x != torch && x.Slot == equipmentSlot);
        if (occupant != null)
            occupant.Slot = character.Items.Where(x => x != torch).Select(x => x.Slot).DefaultIfEmpty(0).Max() + 1;
        torch.Slot = equipmentSlot;

        // 火炬会按服务器规则消耗耐久，准备两个备用照明物品，保证机器人
        // 长时间挂机不会因为火炬耗尽而失去夜间亮度。
        while (character.Items.Count(x => x.Info?.ItemType == ItemType.Torch) < 3)
        {
            int slot = Enumerable.Range(0, Globals.InventorySize)
                .FirstOrDefault(x => character.Items.All(item => item.Slot != x));
            if (character.Items.Any(x => x.Slot == slot)) break;
            AddItem(character, torchInfo, slot, 1);
        }
        return true;
    }

    static bool EnsureMiningTool(CharacterInfo character)
    {
        var pickaxe = Session.GetCollection<ItemInfo>().Binding
            .Where(x => x.ItemEffect == ItemEffect.PickAxe)
            .OrderBy(x => x.RequiredType == RequiredType.Level ? x.RequiredAmount : 0)
            .FirstOrDefault();
        if (pickaxe == null) return false;
        if (character.Items.Any(x => x.Info?.ItemEffect == ItemEffect.PickAxe)) return false;

        int slot = Enumerable.Range(0, Globals.InventorySize)
            .FirstOrDefault(x => character.Items.All(item => item.Slot != x));
        if (character.Items.Any(x => x.Slot == slot)) return false;
        AddItem(character, pickaxe, slot, 1);
        return true;
    }

    static bool EnsureHorse(AccountInfo account, int index)
    {
        // Give every fourth bot a different persistent mount. Existing horse
        // ownership is preserved so this migration remains idempotent.
        if (index % 4 != 0 || account.Horse != HorseType.None) return false;
        account.Horse = (HorseType)(((index / 4 - 1) % 4) + 1);
        return true;
    }

    static bool EnsureCompanion(AccountInfo account, CharacterInfo character, int index)
    {
        // A subset of bots owns a persistent companion. The server will spawn
        // it from CharacterInfo.Companion during normal StartGame handling.
        if (index % 4 != 3 || account.Companions.Any()) return false;
        var info = Session.GetCollection<CompanionInfo>().Binding
            .Where(x => x.MonsterInfo != null)
            .OrderBy(x => x.Index)
            .ElementAtOrDefault((index / 4) % Math.Max(1, Session.GetCollection<CompanionInfo>().Binding.Count));
        if (info == null) return false;

        var companion = Session.GetCollection<UserCompanion>().CreateNewObject();
        companion.Account = account;
        companion.Character = character;
        companion.Info = info;
        companion.Name = $"{character.CharacterName}伙伴";
        companion.Level = 1;
        companion.Hunger = 100;
        character.Companion = companion;
        return true;
    }

    // C6: TestHero 专属伙伴。Lv2 才有 1 格背包 (服务端 Companion.Stats[Stat.CompanionInventory]
    // = InventorySpace, Lv1 为 0 会拒收物品), Hunger=50 (低于 Lv2 MaxHunger=100,
    // 服务端 7054-7060 饥饿满拒用)。先删旧伙伴再建, 保证幂等。
    static bool SeedTestHeroCompanion(CharacterInfo character)
    {
        var old = character.Companion;
        if (old != null)
        {
            character.Companion = null;
            old.Character = null;
            old.Account = null;
            old.Delete();
        }
        foreach (var stale in character.Account.Companions.ToList())
        {
            if (stale == old) continue;
            stale.Character = null;
            stale.Account = null;
            stale.Delete();
        }

        var info = Session.GetCollection<CompanionInfo>().Binding
            .Where(x => x.MonsterInfo != null)
            .OrderBy(x => x.Index)
            .FirstOrDefault();
        if (info == null)
        {
            Console.WriteLine("[warn] 无 CompanionInfo (MonsterInfo != null), 跳过伙伴播种");
            return false;
        }

        var companion = Session.GetCollection<UserCompanion>().CreateNewObject();
        companion.Account = character.Account;
        companion.Character = character;
        companion.Info = info;
        companion.Name = $"{character.CharacterName}伙伴";
        companion.Level = 2;
        companion.Hunger = 50;
        character.Companion = companion;
        return true;
    }

    // C6: 背包放 10 个伙伴食物 (首个 CompanionHunger > 0 的 CompanionFood), 找空闲槽
    static int SeedCompanionFood(CharacterInfo character)
    {
        var food = Session.GetCollection<ItemInfo>().Binding
            .Where(x => x.ItemType == ItemType.CompanionFood && x.Stats[Stat.CompanionHunger] > 0)
            .OrderBy(x => x.Index)
            .FirstOrDefault();
        if (food == null)
        {
            Console.WriteLine("[warn] 无 CompanionFood 物品, 跳过食物播种");
            return 0;
        }
        int slot = Enumerable.Range(0, Globals.InventorySize)
            .FirstOrDefault(x => character.Items.All(item => item.Slot != x));
        if (character.Items.Any(x => x.Slot == slot)) return 0;
        AddItem(character, food, slot, 10);
        Console.WriteLine($"[seed] 伙伴食物: {food.ItemName} index={food.Index} slot={slot} count=10 hungerStat={food.Stats[Stat.CompanionHunger]}");
        return 10;
    }

    // E3: 行会重置 (幂等)。镜像服务端踢人 (PlayerObject.cs:4771-4773 info.Guild=null;
    // info.Account=null; info.Delete()) 移除 TestHero 成员资格; 若行会已空则清仓库
    // 物品并删会 (行会名 E3AuditGuild 可被下次建会复用)。
    static bool ResetGuild(AccountInfo account)
    {
        var member = account.GuildMember;
        if (member == null) return false;
        var guild = member.Guild;
        member.Guild = null;
        member.Account = null;
        member.Delete();
        if (guild == null) return true;
        if (guild.Members.Count > 0) return true;
        foreach (var item in guild.Items.ToList())
        {
            guild.Items.Remove(item);
            item.Delete();
        }
        foreach (var m in guild.Members.ToList())
        {
            m.Guild = null;
            m.Account = null;
            m.Delete();
        }
        guild.Delete();
        return true;
    }

    // E3 闸门依赖安全区: 每次播种强制把 TestHero 放回 map1 "Player Spawns" 安全区
    // (158,229)。只动参考角色, 机器人保留各自已存坐标。
    static void ResetTestHeroPosition(CharacterInfo character)
    {
        var map1 = Session.GetCollection<MapInfo>().Binding.FirstOrDefault(x => x.Index == 1);
        if (map1 == null)
        {
            Console.WriteLine("[warn] 无 map1, 跳过出生点重置");
            return;
        }
        character.CurrentMap = map1;
        character.CurrentInstance = null;
        character.CurrentLocation = new Point(158, 229);
    }

    static void SeedMagics(CharacterInfo character, MirClass cls, int level)
    {
        var available = Session.GetCollection<MagicInfo>().Binding.Where(x => x.Class == cls && x.NeedLevel1 <= level).ToList();
        var priority = cls switch
        {
            MirClass.Wizard => new[] { MagicType.MagicShield, MagicType.SuperiorMagicShield, MagicType.FireBall, MagicType.LightningBall, MagicType.IceBolt },
            MirClass.Taoist => new[] { MagicType.Heal, MagicType.SummonSkeleton, MagicType.SummonShinsu, MagicType.SummonJinSkeleton, MagicType.SummonDemonicCreature, MagicType.MagicShield },
            MirClass.Warrior => new[] { MagicType.Slaying, MagicType.Thrusting, MagicType.HalfMoon, MagicType.DragonRise, MagicType.BladeStorm },
            MirClass.Assassin => new[] { MagicType.Shuriken, MagicType.HundredFist, MagicType.Hemorrhage, MagicType.FlamingDaggers, MagicType.Shredding },
            _ => Array.Empty<MagicType>()
        };
        foreach (var info in available.OrderBy(x => Array.IndexOf(priority, x.Magic) < 0 ? 999 : Array.IndexOf(priority, x.Magic)).ThenBy(x => x.NeedLevel1).Take(12))
        {
            var magic = Session.GetCollection<UserMagic>().CreateNewObject();
            magic.Info = info; magic.Level = Math.Min(3, Math.Max(1, level / 25)); magic.Experience = 0;
            character.Magics.Add(magic);
        }
    }

    // 修复历史建号缺陷：SeedEquipment 曾不按性别筛选，随机选中的衣服若与角色性别
    // 相反，会作为装备槽物品直接写入数据库（不经过服务端 CanUseItem 穿戴校验），
    // 导致男角色穿女款、女角色穿男款。此函数把性别/职业不符的装备槽物品替换为
    // 同类型、同职业、性别匹配且等级可用的替代品；另外武器/衣服把 WarWizTao/All
    // 等通用款升级为本职业专属款（若等级可用）。已匹配的槽位保持不变。
    // 挖矿镐(PickAxe)占武器槽是 BotRunner 运行时的换装状态，不属于种子缺陷，跳过。
    static bool EnsureEquipmentMatches(CharacterInfo character)
    {
        bool changed = false;
        var genderFlag = character.Gender == MirGender.Male ? RequiredGender.Male : RequiredGender.Female;
        var classFlag = (RequiredClass)(1 << (int)character.Class);
        foreach (var item in character.Items.Where(x => x.Slot >= Globals.EquipmentOffSet).ToList())
        {
            if (item.Info == null) continue;
            if (item.Info.ItemEffect == ItemEffect.PickAxe) continue;
            bool genderOk = item.Info.RequiredGender == RequiredGender.None || (item.Info.RequiredGender & genderFlag) != 0;
            bool classOk = Compatible(item.Info.RequiredClass, character.Class);
            bool wantsExclusive = (item.Info.ItemType == ItemType.Weapon || item.Info.ItemType == ItemType.Armour) &&
                                  item.Info.RequiredClass != classFlag;
            if (genderOk && classOk && !wantsExclusive) continue;

            var all = Session.GetCollection<ItemInfo>().Binding
                .Where(x => x.ItemType == item.Info.ItemType)
                .Where(x => x.RequiredType != RequiredType.Level || x.RequiredAmount <= character.Level)
                .Where(x => Compatible(x.RequiredClass, character.Class))
                .Where(x => x.RequiredGender == RequiredGender.None || (x.RequiredGender & genderFlag) != 0)
                .ToList();
            // 武器/衣服优先职业专属款，没有等级可用的专属款才退回通用款。
            var exact = all.Where(x => x.RequiredClass == classFlag).ToList();
            var replacement = (exact.Count > 0 ? exact : all)
                .OrderByDescending(x => x.RequiredType == RequiredType.Level ? x.RequiredAmount : 0)
                .FirstOrDefault();
            if (replacement == null || replacement == item.Info)
            {
                Console.WriteLine($"[warn] {character.CharacterName} 装备槽 {item.Info.ItemName} 无更匹配替代品，保留原状");
                continue;
            }

            string slotName = Enum.GetName(typeof(EquipmentSlot), item.Slot - Globals.EquipmentOffSet) ?? "?";
            Console.WriteLine($"[fix] {character.CharacterName} {slotName}: {item.Info.ItemName} -> {replacement.ItemName} (gender={replacement.RequiredGender} class={replacement.RequiredClass})");
            item.Info = replacement;
            item.CurrentDurability = replacement.Durability;
            item.MaxDurability = replacement.Durability;
            changed = true;
        }
        return changed;
    }

    static bool EnsureClassSupplies(CharacterInfo character)
    {
        if (character.Class != MirClass.Taoist) return false;
        int slot = Globals.EquipmentOffSet + (int)EquipmentSlot.Amulet;
        var amulet = character.Items.FirstOrDefault(x => x.Slot == slot && x.Info?.ItemType == ItemType.Amulet && x.Info.Shape == 0);
        if (amulet != null)
        {
            if (amulet.Count < 500) { amulet.Count = 500; return true; }
            return false;
        }

        var genderFlag = character.Gender == MirGender.Male ? RequiredGender.Male : RequiredGender.Female;
        var info = Session.GetCollection<ItemInfo>().Binding
            .Where(x => x.ItemType == ItemType.Amulet && x.Shape == 0)
            .Where(x => x.RequiredType != RequiredType.Level || x.RequiredAmount <= character.Level)
            .Where(x => Compatible(x.RequiredClass, character.Class))
            .Where(x => x.RequiredGender == RequiredGender.None || (x.RequiredGender & genderFlag) != 0)
            .OrderBy(x => x.RequiredType == RequiredType.Level ? x.RequiredAmount : 0)
            .FirstOrDefault();
        if (info == null) return false;
        var occupant = character.Items.FirstOrDefault(x => x.Slot == slot);
        if (occupant != null) occupant.Slot = Enumerable.Range(0, Globals.InventorySize).FirstOrDefault(x => character.Items.All(item => item.Slot != x), 0);
        AddItem(character, info, slot, 500);
        return true;
    }

    static bool EnsureClassMagics(CharacterInfo character)
    {
        var priority = character.Class switch
        {
            MirClass.Wizard => new[] { MagicType.MagicShield, MagicType.SuperiorMagicShield },
            MirClass.Taoist => new[] { MagicType.Heal, MagicType.SummonSkeleton, MagicType.SummonShinsu, MagicType.SummonJinSkeleton, MagicType.SummonDemonicCreature },
            MirClass.Warrior => new[] { MagicType.Slaying, MagicType.Thrusting, MagicType.HalfMoon, MagicType.DragonRise, MagicType.BladeStorm },
            MirClass.Assassin => new[] { MagicType.Shuriken, MagicType.HundredFist, MagicType.Hemorrhage, MagicType.FlamingDaggers },
            _ => Array.Empty<MagicType>()
        };
        bool changed = false;
        foreach (var type in priority)
        {
            var info = Session.GetCollection<MagicInfo>().Binding.FirstOrDefault(x => x.Magic == type && x.Class == character.Class && x.NeedLevel1 <= character.Level);
            if (info == null || character.Magics.Any(x => x.Info == info)) continue;
            var magic = Session.GetCollection<UserMagic>().CreateNewObject();
            magic.Info = info; magic.Level = Math.Min(3, Math.Max(1, character.Level / 25));
            character.Magics.Add(magic);
            changed = true;
        }
        return changed;
    }

    static void AddItem(CharacterInfo character, ItemInfo info, int slot, long count)
    {
        var item = Session.GetCollection<UserItem>().CreateNewObject();
        item.Info = info; item.Slot = slot; item.Count = count;
        item.CurrentDurability = info.Durability; item.MaxDurability = info.Durability;
        character.Items.Add(item);
    }

    static bool Compatible(RequiredClass required, MirClass cls) => required == RequiredClass.None || required == RequiredClass.All || required.HasFlag((RequiredClass)(1 << (int)cls));
    static Point Offset(Point origin, int n) => new(origin.X + (n % 5) - 2, origin.Y + (n / 5) - 2);
    static readonly Color[] HairColours = { Color.Black, Color.Brown, Color.DarkBlue, Color.DarkRed, Color.Gray };
    static readonly Color[] ArmourColours = { Color.White, Color.LightBlue, Color.LightGreen, Color.LightPink, Color.LightYellow };
    static void Usage() => Console.WriteLine("用法: dotnet run --project Tools/BotProvisioner -- <Users.db所在目录> [--reference TestHero] [--count 20] [--prefix bot] [--password bot123456] [--dry-run] [--reset-positions] [--seed-reference] [--resource-report]");
}
