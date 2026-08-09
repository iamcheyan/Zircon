using System.Drawing;
using Library;
using S = Library.Network.ServerPackets;

namespace Zircon.BotRunner;

public sealed class BotWorld
{
    public int MapIndex { get; private set; }
    public int SpawnMapIndex { get; private set; }
    public int InstanceIndex { get; private set; }
    public Point Location { get; private set; }
    public Point SpawnLocation { get; private set; }
    public MirDirection Direction { get; private set; }
    public uint SelfObjectId { get; private set; }
    public int Level { get; private set; }
    public MirClass Class { get; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    public bool Dead { get; private set; }
    public bool InSafeZone { get; private set; }
    public HorseType Horse { get; private set; }
    public long Gold { get; private set; }

    public Dictionary<uint, S.ObjectMonster> Monsters { get; } = new();
    public Dictionary<uint, S.ObjectPlayer> Players { get; } = new();
    public Dictionary<uint, S.ObjectNPC> Npcs { get; } = new();
    public Dictionary<uint, S.ObjectItem> Items { get; } = new();
    public List<ClientUserItem> Inventory { get; } = new();
    public List<ClientUserMagic> Magics { get; } = new();
    public HashSet<string> GroupMembers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<uint, S.DataObjectHealthMana> PlayerVitals { get; } = new();
    public Dictionary<uint, S.DataObjectMaxHealthMana> PlayerMaxVitals { get; } = new();
    public ClientUserItem EquippedTorch => Inventory.FirstOrDefault(x => x.Info?.ItemType == ItemType.Torch && x.Slot == Globals.EquipmentOffSet + (int)EquipmentSlot.Torch);
    public ClientUserItem SpareTorch => Inventory.FirstOrDefault(x => x.Info?.ItemType == ItemType.Torch && x.Slot != Globals.EquipmentOffSet + (int)EquipmentSlot.Torch && x.Count > 0);

    public void Apply(S.StartGame p)
    {
        if (p.StartInformation == null) return;
        var s = p.StartInformation;
        // A reconnect creates a new server-side object set. Never let stale
        // monsters, players, loot or vitals from the previous session drive
        // the first actions after login.
        Monsters.Clear();
        Players.Clear();
        Npcs.Clear();
        Items.Clear();
        PlayerVitals.Clear();
        PlayerMaxVitals.Clear();
        SelfObjectId = s.ObjectID;
        MapIndex = s.MapIndex;
        SpawnMapIndex = s.MapIndex;
        InstanceIndex = s.InstanceIndex;
        Location = s.Location;
        SpawnLocation = s.Location;
        Direction = s.Direction;
        Level = s.Level;
        Class = s.Class;
        CurrentHealth = s.CurrentHP;
        MaxHealth = s.CurrentHP;
        CurrentMana = s.CurrentMP;
        MaxMana = s.CurrentMP;
        Dead = s.CurrentHP <= 0;
        InSafeZone = s.InSafeZone;
        Horse = s.Horse;
        Inventory.Clear();
        if (s.Items != null) Inventory.AddRange(s.Items.Where(x => x != null));
        Magics.Clear();
        if (s.Magics != null) Magics.AddRange(s.Magics.Where(x => x?.Info != null));
        var gold = Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.Type == CurrencyType.Gold);
        Gold = s.Currencies?.FirstOrDefault(x => x.CurrencyIndex == gold?.Index)?.Amount ?? 0;
        GroupMembers.Clear();
    }

    public void Apply(S.MapChanged p) { MapIndex = p.MapIndex; InstanceIndex = p.InstanceIndex; }
    public void Apply(S.SafeZoneChanged p) { InSafeZone = p.InSafeZone; }
    public void Apply(S.CurrencyChanged p)
    {
        var gold = Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.Type == CurrencyType.Gold);
        if (gold?.Index == p.CurrencyIndex) Gold = p.Amount;
    }
    public void Apply(S.UserLocation p) { Direction = p.Direction; Location = p.Location; }
    public void Apply(S.ObjectMove p)
    {
        if (p.ObjectID == SelfObjectId) { Direction = p.Direction; Location = p.Location; }
        if (Monsters.TryGetValue(p.ObjectID, out var monster)) { monster.Direction = p.Direction; monster.Location = p.Location; }
        if (Players.TryGetValue(p.ObjectID, out var player)) { player.Direction = p.Direction; player.Location = p.Location; }
    }
    public void Apply(S.ObjectTurn p)
    {
        if (p.ObjectID == SelfObjectId) { Direction = p.Direction; Location = p.Location; }
        if (Monsters.TryGetValue(p.ObjectID, out var monster)) { monster.Direction = p.Direction; monster.Location = p.Location; }
        if (Players.TryGetValue(p.ObjectID, out var player)) { player.Direction = p.Direction; player.Location = p.Location; }
    }
    public void Apply(S.ObjectMount p)
    {
        if (p.ObjectID == SelfObjectId) Horse = p.Horse;
        if (Players.TryGetValue(p.ObjectID, out var player)) player.Horse = p.Horse;
    }
    public void Apply(S.ObjectMonster p) { Monsters[p.ObjectID] = p; }
    public void Apply(S.ObjectPlayer p) { Players[p.ObjectID] = p; }
    public void Apply(S.DataObjectHealthMana p) { if (p.ObjectID != SelfObjectId) PlayerVitals[p.ObjectID] = p; }
    public void Apply(S.DataObjectMaxHealthMana p) { if (p.ObjectID != SelfObjectId) PlayerMaxVitals[p.ObjectID] = p; }
    public void Apply(S.ObjectNPC p) { Npcs[p.ObjectID] = p; }
    public void Apply(S.ObjectItem p) { Items[p.ObjectID] = p; }
    public void Apply(S.ObjectRemove p) { Monsters.Remove(p.ObjectID); Players.Remove(p.ObjectID); Npcs.Remove(p.ObjectID); Items.Remove(p.ObjectID); }
    public void Apply(S.HealthChanged p)
    {
        if (p.ObjectID != SelfObjectId) return;
        CurrentHealth = Math.Max(0, CurrentHealth + p.Change);
        Dead = CurrentHealth <= 0;
    }
    public void Apply(S.ManaChanged p)
    {
        if (p.ObjectID != SelfObjectId) return;
        CurrentMana = Math.Max(0, CurrentMana + p.Change);
    }
    public void Apply(S.StatsUpdate p)
    {
        if (p.Stats == null) return;
        MaxHealth = Math.Max(MaxHealth, p.Stats[Stat.Health]);
        MaxMana = Math.Max(MaxMana, p.Stats[Stat.Mana]);
        if (CurrentHealth <= 0) CurrentHealth = p.Stats[Stat.Health];
        if (CurrentMana <= 0) CurrentMana = p.Stats[Stat.Mana];
    }
    public void Apply(S.ItemsGained p)
    {
        if (p.Items == null) return;
        foreach (var item in p.Items.Where(x => x != null))
        {
            Inventory.RemoveAll(x => x.Index == item.Index || (x.Slot == item.Slot && x.InfoIndex == item.InfoIndex));
            Inventory.Add(item);
        }
    }
    public void Apply(S.ItemChanged p)
    {
        if (p.Link == null) return;
        var item = Inventory.FirstOrDefault(x => x.Slot == AbsoluteSlot(p.Link.GridType, p.Link.Slot));
        if (item == null) return;
        if (p.Success && p.Link.Count <= 0) Inventory.Remove(item);
        // 服务器发的是剩余总量(PlayerObject 消耗后 result.Link.Count = item.Count),
        // 不是变化量; 直接覆盖, 否则会越减越少。
        else if (p.Link.Count > 0) item.Count = p.Link.Count;
    }
    public void Apply(S.ItemMove p)
    {
        if (!p.Success) return;
        var item = Inventory.FirstOrDefault(x => x.Slot == AbsoluteSlot(p.FromGrid, p.FromSlot));
        if (item == null) return;
        item.Slot = AbsoluteSlot(p.ToGrid, p.ToSlot);
    }
    public void Apply(S.ItemSort p)
    {
        if (!p.Success || p.Grid != GridType.Inventory || p.Items == null) return;
        // ClientUserItem does not carry GridType. Preserve equipment entries
        // while replacing the inventory portion returned by the server.
        var equipment = Inventory
            .Where(x => x.Info?.ItemType is ItemType.Weapon or ItemType.Armour or ItemType.Torch or ItemType.Helmet
                or ItemType.Necklace or ItemType.Bracelet or ItemType.Ring or ItemType.Shoes or ItemType.Poison
                or ItemType.Amulet or ItemType.Flower or ItemType.HorseArmour or ItemType.Emblem or ItemType.Shield
                or ItemType.Costume or ItemType.Hook or ItemType.Float or ItemType.Bait or ItemType.Finder or ItemType.Reel)
            .ToList();
        var sorted = p.Items.Where(x => x != null).ToList();
        Inventory.Clear();
        Inventory.AddRange(equipment);
        Inventory.AddRange(sorted.Where(x => equipment.All(e => e.Index != x.Index)));
    }
    public void Apply(S.ItemDelete p)
    {
        if (!p.Success) return;
        Inventory.RemoveAll(x => x.Slot == AbsoluteSlot(p.Grid, p.Slot));
    }
    public void Apply(S.ItemDurability p)
    {
        var item = Inventory.FirstOrDefault(x => x.Slot == AbsoluteSlot(p.GridType, p.Slot));
        if (item != null) item.CurrentDurability = Math.Max(0, p.CurrentDurability);
    }
    public void Apply(S.GroupMember p)
    {
        if (!string.IsNullOrWhiteSpace(p.Name)) GroupMembers.Add(p.Name);
    }
    public void Apply(S.GroupRemove p)
    {
        if (p.ObjectID == SelfObjectId) GroupMembers.Clear();
    }
    // Client packets use grid-local slots. StartGame and the persistent
    // database use the shared absolute equipment range (1000+slot).
    private static int AbsoluteSlot(GridType grid, int slot)
        => grid is GridType.Equipment or GridType.CompanionEquipment
            ? Globals.EquipmentOffSet + slot
            : slot;
    public void Apply(S.ObjectDied p)
    {
        if (p.ObjectID == SelfObjectId) Dead = true;
        if (Players.TryGetValue(p.ObjectID, out var player)) player.Dead = true;
        if (Monsters.TryGetValue(p.ObjectID, out var monster)) monster.Dead = true;
    }
    public void Apply(S.ObjectRevive p)
    {
        if (p.ObjectID == SelfObjectId) { Location = p.Location; Dead = false; }
        if (Players.TryGetValue(p.ObjectID, out var player)) { player.Dead = false; player.Location = p.Location; }
        if (Monsters.TryGetValue(p.ObjectID, out var monster)) { monster.Dead = false; monster.Location = p.Location; }
    }
    public void Apply(S.LevelChanged p) { Level = p.Level; }
}
