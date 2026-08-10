using System.Collections.Concurrent;
using System.Drawing;
using System.Net.Sockets;
using Library;
using Library.Network;
using C = Library.Network.ClientPackets;
using G = Library.Network.GeneralPackets;
using S = Library.Network.ServerPackets;

namespace Zircon.BotRunner;

public sealed class BotConnection : BaseConnection
{
    protected override TimeSpan TimeOutDelay => TimeSpan.FromSeconds(30);
    public event Action<BotConnection, Packet> PacketReceived;
    public event Action<BotConnection, Exception> ConnectionError;

    private readonly byte[] _clientHash;

    public BotConnection(TcpClient client, byte[] clientHash = null, bool verboseNetworkLogging = false) : base(client)
    {
        _clientHash = clientHash ?? Array.Empty<byte>();
        AdditionalLogging = verboseNetworkLogging;
        OnException = (sender, exception) => ConnectionError?.Invoke(this, exception);
        UpdateTimeOut();
        BeginReceive();
    }

    public override void TryDisconnect() => Disconnect();
    public override void TrySendDisconnect(Packet p) => SendDisconnect(p);

    protected override void ProcessUnhandledPacket(Packet p) => PacketReceived?.Invoke(this, p);

    public void Process(G.Connected p)
    {
        Enqueue(new G.Connected());
        PacketReceived?.Invoke(this, p);
    }

    public void Process(G.CheckVersion p)
    {
        Enqueue(new G.Version { ClientHash = _clientHash });
        PacketReceived?.Invoke(this, p);
    }

    public void Process(G.GoodVersion p) => PacketReceived?.Invoke(this, p);
    public void Process(G.Disconnect p)
    {
        PacketReceived?.Invoke(this, p);
        Connected = false;
    }

    public void Process(G.Ping p)
    {
        Enqueue(new G.Ping());
        PacketReceived?.Invoke(this, p);
    }

    public void Process(S.Login p) => PacketReceived?.Invoke(this, p);
    public void Process(S.NewAccount p) => PacketReceived?.Invoke(this, p);
    public void Process(S.StartGame p) => PacketReceived?.Invoke(this, p);
    public void Process(S.MapChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.UserLocation p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectPlayer p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectMonster p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectNPC p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectItem p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectRemove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectMove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectTurn p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectDied p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectRevive p) => PacketReceived?.Invoke(this, p);
    public void Process(S.HealthChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.DataObjectHealthMana p) => PacketReceived?.Invoke(this, p);
    public void Process(S.DataObjectMaxHealthMana p) => PacketReceived?.Invoke(this, p);
    public void Process(S.StatsUpdate p) => PacketReceived?.Invoke(this, p);
    public void Process(S.LevelChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.GainedExperience p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemsGained p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemMove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemSort p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemDelete p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ItemDurability p) => PacketReceived?.Invoke(this, p);
    public void Process(S.CurrencyChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.SafeZoneChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.Chat p) => PacketReceived?.Invoke(this, p);
    public void Process(S.NPCResponse p) => PacketReceived?.Invoke(this, p);
    public void Process(S.NPCClose p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectAttack p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectRangeAttack p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectMagic p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectStruck p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectEffect p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectBuffAdd p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectBuffRemove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectPoison p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ManaChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.FocusChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BuffAdd p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BuffRemove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BuffChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BuffTime p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BuffPaused p) => PacketReceived?.Invoke(this, p);
    public void Process(S.MagicCooldown p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectMining p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectHarvest p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectHarvested p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectMount p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectFishing p) => PacketReceived?.Invoke(this, p);
    public void Process(S.FishingStats p) => PacketReceived?.Invoke(this, p);
    public void Process(S.SetTimer p) => PacketReceived?.Invoke(this, p);
    public void Process(S.ObjectPetOwnerChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.GroupInvite p) => PacketReceived?.Invoke(this, p);
    public void Process(S.GroupRequest p) => PacketReceived?.Invoke(this, p);
    public void Process(S.GroupMember p) => PacketReceived?.Invoke(this, p);
    public void Process(S.GroupRemove p) => PacketReceived?.Invoke(this, p);
    public void Process(S.AutoPathChanged p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeRequest p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeOpen p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeClose p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeItemAdded p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeGoldAdded p) => PacketReceived?.Invoke(this, p);
    public void Process(S.TradeUnlock p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BundleOpen p) => PacketReceived?.Invoke(this, p);
    public void Process(S.BundleClose p) => PacketReceived?.Invoke(this, p);
    public void Process(S.JoinInstance p) => PacketReceived?.Invoke(this, p);
}
