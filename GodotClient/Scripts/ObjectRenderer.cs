using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.Network;
using Library.SystemModels;
using S = Library.Network.ServerPackets;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

// 周围物体渲染 (怪物/NPC/地面物品), 移植自 Client/Models/MonsterObject.cs + NPCObject.cs + ItemObject.cs
// 帧号公式: DrawFrame = FrameIndex + Start + OffSet*dir; 形状偏移: BodyFrame = DrawFrame + BodyShape * BodyOffSet
public partial class ObjectRenderer : MapObjectNode
{
    public enum Kind { Monster, NPC, Item, Player }

    public Kind Type;
    public ZlLibrary BodyLibrary;
    public int BodyShape;   // 怪物: MonsterLookup 形状; NPC: NPCInfo.Image; 物品: 0
    public int BodyOffSet;  // 怪物: 1000; NPC: 100; 物品: 0
    public int DrawImage;   // 物品专用: Ground 图库帧号 (无方向/形状)
    public string DisplayName;
    public string GuildName;
    public string PetOwner;
    public Stats Stats;
    public int CharacterIndex;
    public int Level;
    public MonsterImage MonsterImage;
    public MonsterInfo MonsterInfo;
    public bool MonsterExtra;
    public bool Skeleton;
    public Color NameColour = Colors.White;
    public Color DrawColour = Colors.White;
    public PoisonType Poison;
    public bool Focused;
    public int Light;
    public string ChatText;
    private double _chatUntil;

    private Dictionary<MirAnimation, Frame> _frameTable = new(FrameSet.DefaultMonster);
    public override Dictionary<MirAnimation, Frame> FrameTable => _frameTable;

    // ---- 工厂方法 ----
    public static ObjectRenderer CreateMonster(S.ObjectMonster p)
    {
        if (p.Dead) return null; // 死亡状态不渲染 (服务端随后发 ObjectRemove)

        var mi = Globals.MonsterInfoList?.Binding.FirstOrDefault(x => x.Index == p.MonsterIndex);
        if (mi == null)
        {
            GD.PrintErr($"[Object] 找不到怪物信息: MonsterIndex={p.MonsterIndex}");
            return null;
        }
        if (!MonsterLookup.Map.TryGetValue(mi.Image, out var lookup))
        {
            GD.PrintErr($"[Object] 怪物无图库映射: {mi.MonsterName} ({mi.Image})");
            return null;
        }

        string monsterName = string.IsNullOrWhiteSpace(p.CustomName) ? mi.MonsterName : p.CustomName;
        if (string.IsNullOrWhiteSpace(monsterName)) monsterName = $"Monster {p.MonsterIndex}";

        var r = new ObjectRenderer
        {
            Type = Kind.Monster,
            MonsterImage = mi.Image,
            MonsterInfo = mi,
            MonsterExtra = p.Extra,
            Skeleton = p.Skeleton,
            DisplayName = monsterName,
            PetOwner = p.PetOwner,
            Stats = mi.Stats,
            Level = mi.Level,
            NameColour = p.NameColour.A <= 0 ? Colors.White : ToGodot(p.NameColour),
            DrawColour = p.Colour.A <= 0 ? Colors.White : ToGodot(p.Colour),
            Poison = p.Poison,
            BodyLibrary = LibraryCache.Get(lookup.File),
            BodyShape = lookup.Shape,
            BodyOffSet = 1000,
        };
        if (r.BodyLibrary == null)
        {
            GD.PrintErr($"[Object] 怪物图库加载失败: {lookup.File}");
            return null;
        }
        r.ApplyPacket(p.Direction, p.Location);
        return r;
    }

    public static ObjectRenderer CreateNPC(S.ObjectNPC p)
    {
        var ni = Globals.NPCInfoList?.Binding.FirstOrDefault(x => x.Index == p.NPCIndex);
        if (ni == null)
        {
            GD.PrintErr($"[Object] 找不到 NPC 信息: NPCIndex={p.NPCIndex}");
            return null;
        }

        var r = new ObjectRenderer
        {
            Type = Kind.NPC,
            DisplayName = ni.NPCName,
            NameColour = new Color(0.4f, 1f, 0.4f),
            BodyLibrary = LibraryCache.Get(LibraryFile.NPC),
            BodyShape = ni.Image,
            BodyOffSet = 100,
            // 原版 NPCObject 构造时固定带 Light = 10（城镇灯笼、火把类
            // NPC 依靠这个属性参与夜间光照层）。
            Light = 10,
        };
        r._frameTable = BuildNpcFrames(ni.Image);
        if (r.BodyLibrary == null)
        {
            GD.PrintErr("[Object] NPC 图库加载失败: NPC.Zl");
            return null;
        }
        r.ApplyPacket(p.Direction, p.CurrentLocation);
        return r;
    }

    public static ObjectRenderer CreateItem(S.ObjectItem p)
    {
        if (p.Item?.Info == null)
        {
            GD.PrintErr($"[Object] 物品信息缺失: ObjectID={p.ObjectID}");
            return null;
        }

        ItemInfo info = p.Item.Info;
        if (info.ItemEffect == ItemEffect.ItemPart && p.Item.AddedStats != null &&
            p.Item.AddedStats[Stat.ItemIndex] > 0)
        {
            int partIndex = p.Item.AddedStats[Stat.ItemIndex];
            var partInfo = Globals.ItemInfoList?.Binding.FirstOrDefault(x => x.Index == partIndex);
            if (partInfo != null) info = partInfo;
        }

        int drawIndex;
        if (IsCurrencyItem(info))
            drawIndex = CurrencyImage(info, p.Item.Count);
        else
            drawIndex = info.Image;

        var r = new ObjectRenderer
        {
            Type = Kind.Item,
            DisplayName = info.ItemName ?? "Item",
            NameColour = Colors.White,
            BodyLibrary = LibraryCache.Get(LibraryFile.Ground),
            BodyShape = 0,
            BodyOffSet = 0,
            DrawImage = drawIndex,
        };
        if (r.BodyLibrary == null)
        {
            GD.PrintErr("[Object] 地面物品图库加载失败: Ground.Zl");
            return null;
        }
        if (drawIndex < 0 || drawIndex >= r.BodyLibrary.Images.Length)
        {
            GD.PrintErr($"[Object] 物品帧越界: {info.ItemName} Image={drawIndex} 图库={r.BodyLibrary.Images.Length}");
            return null;
        }
        r.ApplyPacket(MirDirection.Up, p.Location);
        return r;
    }

    /// <summary>
    /// 怪物施法动作。原版怪物的普通 Spell 动作是 Combat3，不能复用
    /// 玩家按 MagicType 选择的 Combat1/Combat2；DragonRepulse 才有专用
    /// 起始动作，而且只有当前怪物帧表提供时才使用。
    /// </summary>
    public void PlaySpell(MagicType magic)
    {
        var animation = MirAnimation.Combat3;
        if (magic == MagicType.DragonRepulse)
            animation = MirAnimation.DragonRepulseStart;
        animation = magic switch
        {
            MagicType.DoomClawRightPinch => MirAnimation.Combat1,
            MagicType.DoomClawRightSwipe => MirAnimation.Combat2,
            MagicType.DoomClawSpit => MirAnimation.Combat7,
            MagicType.DoomClawWave => MirAnimation.Combat6,
            MagicType.DoomClawLeftPinch => MirAnimation.Combat4,
            MagicType.DoomClawLeftSwipe => MirAnimation.Combat5,
            _ => animation,
        };
        SetAnimation(animation);
    }

    public void PlayRangeAttack() => SetAnimation(MirAnimation.Combat2);

    private static bool IsCurrencyItem(ItemInfo info)
    {
        return Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.DropItem == info) != null;
    }

    private static int CurrencyImage(ItemInfo info, long count)
    {
        var currency = Globals.CurrencyInfoList?.Binding.FirstOrDefault(x => x.DropItem == info);
        if (currency == null) return 0;

        var image = currency.Images.OrderByDescending(x => x.Amount).FirstOrDefault(x => x.Amount <= count);
        return image?.Image ?? currency.DropItem.Image;
    }

    // NPC 帧表特例 (移植自 NPCObject 构造函数)
    private static Dictionary<MirAnimation, Frame> BuildNpcFrames(int image)
    {
        switch (image)
        {
            case 64 or 65 or 91 or 92 or 93 or 157 or 158 or 160 or 165 or 166 or 168
                 or 208 or 209 or 210 or 211 or 212 or 213 or 214 or 231 or 234:
                return new Dictionary<MirAnimation, Frame>
                {
                    [MirAnimation.Standing] = new Frame(0, 1, 0, TimeSpan.FromHours(1)),
                };
            case 56 or 57:
                return new Dictionary<MirAnimation, Frame>
                {
                    [MirAnimation.Standing] = new Frame(0, 12, 0, TimeSpan.FromMilliseconds(200)),
                };
            case 156:
                return new Dictionary<MirAnimation, Frame>
                {
                    [MirAnimation.Standing] = new Frame(0, 16, 0, TimeSpan.FromMilliseconds(200)),
                };
            default:
                return new Dictionary<MirAnimation, Frame>(FrameSet.DefaultNPC);
        }
    }

    private void ApplyPacket(MirDirection dir, System.Drawing.Point loc)
    {
        Direction = dir;
        CellX = loc.X;
        CellY = loc.Y;
        SetAnimation(MonsterImage is MonsterImage.ZumaGuardian or MonsterImage.ZumaFanatic or MonsterImage.ZumaKing
            ? (MonsterExtra ? MirAnimation.Standing : MirAnimation.StoneStanding)
            : MirAnimation.Standing);
    }

    private bool _debugLogged;
    private bool _decodeErrorLogged;
    // 旧端 MonsterObject 的附加外观使用 ImageType.Image；这里不能使用
    // 技能特效的黑色透明键，否则黑色铠甲/旗帜/怪物细节会被抠除。
    private readonly List<BlendImageLayerNode> _blendLayers = new();
    private BlendImageLayerNode _bodyBlendLayer;

    private static Color ToGodot(System.Drawing.Color c) =>
        new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    public override void _Draw()
    {
        if (BodyLibrary == null) return;
        if (!_debugLogged)
        {
            _debugLogged = true;
            GD.Print($"[ObjectView] 首帧诊断: type={Type} name={DisplayName} lib={System.IO.Path.GetFileName(BodyLibrary.FileName)} " +
                     $"shape={BodyShape} drawImage={DrawImage} dir={Direction} frame={FrameIndex} anim={Animation} " +
                     $"DrawFrame={DrawFrame} BodyFrame={BodyFrame} Cell=({CellX},{CellY}) Pos=({Position.X},{Position.Y}) viewport=({GetViewport().GetVisibleRect().Size.X},{GetViewport().GetVisibleRect().Size.Y})");
        }

        if (Type == Kind.Item)
        {
            if (!DrawResourceShadow(DrawImage, 0, 0))
                RenderPrimitives.DrawGroundShadow(this, 20f, 6f, 24f, 16f, 0.32f);
            DrawItemImage(DrawImage, 0, 0);
        }
        else
        {
            int bodyY = MonsterImage switch
            {
                MonsterImage.ChestnutTree => -32,
                MonsterImage.NewMob10 => -128,
                _ => 0,
            };
            // 原版 MonsterObject.DrawShadow：普通怪物优先使用 ZL 当前帧的
            // Shadow 通道；只有资源没有可用 Shadow 时才用主体轮廓兜底。
            // 若反过来优先投影主体，会把树、巨型怪物压成一大片黑影。
            if (!DrawMonsterShadow(bodyY))
                RenderPrimitives.DrawGroundShadow(this, Type == Kind.NPC ? 21f : 28f,
                    Type == Kind.NPC ? 7f : 10f, 24f, 16f, 0.44f);
            if (MonsterImage is MonsterImage.DustDevil or MonsterImage.Tornado)
            {
                if (_bodyBlendLayer == null)
                {
                    _bodyBlendLayer = new BlendImageLayerNode();
                    AddChild(_bodyBlendLayer);
                }
                _bodyBlendLayer.Position = new Vector2(0, bodyY);
                _bodyBlendLayer.Configure(BodyLibrary, BodyFrame, DrawColour, 1);
            }
            else
            {
                if (_bodyBlendLayer != null) _bodyBlendLayer.Visible = false;
                DrawLayer(BodyFrame, 0, bodyY, DrawColour);
            }
            if (MonsterImage == MonsterImage.CastleFlag)
                DrawOverlay(BodyFrame, 0, bodyY, DrawColour);
            if (MonsterImage == MonsterImage.LobsterLord)
            {
                DrawLayer(BodyFrame + 1000, 0, bodyY, DrawColour);
                DrawLayer(BodyFrame + 2000, 0, bodyY, DrawColour);
            }
            DrawSpecialMonsterEffect(bodyY);
        }
        DrawName();
        DrawHealthBar();
    }

    public int BodyFrame => DrawFrame + BodyShape * BodyOffSet;

    private bool DrawMonsterShadow(float bodyY)
    {
        // 与原版一致：这些对象的主体/特效本身就是阴影或门体，不能再
        // 额外投影一个普通怪物阴影。
        switch (MonsterImage)
        {
            case MonsterImage.DustDevil:
            case MonsterImage.Tornado:
            case MonsterImage.SabukGateSouth:
            case MonsterImage.SabukGateNorth:
            case MonsterImage.SabukGateEast:
            case MonsterImage.SabukGateWest:
                return true;
        }

        if (MonsterImage == MonsterImage.LobsterLord)
        {
            bool drawn = DrawResourceShadow(BodyFrame, 0, bodyY);
            drawn |= DrawResourceShadow(BodyFrame + 1000, 0, bodyY);
            drawn |= DrawResourceShadow(BodyFrame + 2000, 0, bodyY);
            return drawn || DrawSilhouetteShadow(BodyFrame, bodyY);
        }

        return DrawResourceShadow(BodyFrame, 0, bodyY)
            || DrawSilhouetteShadow(BodyFrame, bodyY);
    }

    private bool DrawResourceShadow(int frame, float px = 0f, float py = 0f)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return false;
        var img = BodyLibrary.Images[frame];
        if (img == null || !RenderPrimitives.IsUsableResourceShadow(img.ShadowWidth, img.ShadowHeight))
            return false;
        var texture = BodyLibrary.GetShadowTexture(frame);
        if (!RenderPrimitives.IsUsableResourceShadow(texture, img.ShadowWidth, img.ShadowHeight))
            return false;
        var dest = new Rect2(px + img.ShadowOffSetX, py + img.ShadowOffSetY,
            img.ShadowWidth, img.ShadowHeight);
        DrawTextureRectRegion(texture, dest, new Rect2(0, 0, img.ShadowWidth, img.ShadowHeight),
            new Color(0f, 0f, 0f, 0.52f));
        return true;
    }

    private bool DrawSilhouetteShadow(int frame, float py)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return false;
        var img = BodyLibrary.Images[frame];
        var texture = BodyLibrary.GetImageTexture(frame);
        return RenderPrimitives.DrawSilhouetteShadow(this, texture, img, 0.46f,
            new Vector2(12f, 16f + py));
    }

    private void DrawSpecialMonsterEffect(float py)
    {
        foreach (var layer in _blendLayers)
            layer.Configure(null, -1, Colors.White, 1);

        ZlLibrary lib = null;
        int frame = DrawFrame;
        switch (MonsterImage)
        {
            case MonsterImage.NewMob1:
                lib = LibraryCache.Get(LibraryFile.MonMagicEx20); frame += 2000; break;
            case MonsterImage.NumaHighMage:
                lib = LibraryCache.Get(LibraryFile.MonMagicEx4); frame += 500; break;
            case MonsterImage.InfernalSoldier:
                lib = LibraryCache.Get(LibraryFile.MonMagicEx8); break;
            case MonsterImage.JinamStoneGate:
                lib = LibraryCache.Get(LibraryFile.MonMagicEx6); frame = (int)(Godot.Time.GetTicksMsec() / 100 % 30) + 1400; break;
            default: return;
        }
        if (lib == null) return;
        AddBlendLayer(lib, frame, py);
        if (MonsterImage == MonsterImage.InfernalSoldier)
            AddBlendLayer(lib, frame + 1000, py);
    }

    private void AddBlendLayer(ZlLibrary library, int frame, float py)
    {
        BlendImageLayerNode layer;
        if (_blendLayers.Count == 0 || _blendLayers[^1].Visible)
        {
            layer = new BlendImageLayerNode();
            _blendLayers.Add(layer);
            AddChild(layer);
        }
        else
            layer = _blendLayers[^1];

        layer.Configure(library, frame, new Color(1f, 1f, 1f, 0.82f), 1, 0, py);
    }

    private void DrawLayer(ZlLibrary lib, int frame, float px, float py, Color tint, bool effectTexture = false)
    {
        if (lib == null || frame < 0 || frame >= lib.Images.Length) return;
        var img = lib.Images[frame];
        var texture = effectTexture ? lib.GetEffectTexture(frame) : lib.GetImageTexture(frame);
        if (img == null || texture == null) return;
        DrawTextureRectRegion(texture, new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height),
            new Rect2(0, 0, img.Width, img.Height), tint);
    }

    // 地面物品: 居中绘制 (物品图标是平铺地面的, 无锚点)
    private void DrawItemImage(int frame, float px, float py)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return;
        var img = BodyLibrary.Images[frame];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;

        var texture = BodyLibrary.GetImageTexture(frame);
        if (texture == null) return;

        float ox = px + (48 - img.Width) / 2f;
        float oy = py + (32 - img.Height) / 2f;
        DrawTextureRectRegion(texture, new Rect2(ox, oy, img.Width, img.Height), new Rect2(0, 0, img.Width, img.Height));
    }

    private void DrawLayer(int frame, float px, float py, Color? tint = null)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return;
        if (BodyLibrary.Images[frame] == null) return;

        Texture2D texture;
        try
        {
            texture = BodyLibrary.GetImageTexture(frame);
        }
        catch (Exception ex)
        {
            if (!_decodeErrorLogged)
            {
                _decodeErrorLogged = true;
                GD.PrintErr($"[ObjectView] 图片解码失败: lib={System.IO.Path.GetFileName(BodyLibrary.FileName)} frame={frame} err={ex.GetType().Name}: {ex.Message}");
            }
            return;
        }
        if (texture == null) return;

        var img = BodyLibrary.Images[frame];
        Color drawColour = tint ?? Colors.White;
        if (drawColour.A <= 0f) drawColour = Colors.White;
        DrawTextureRectRegion(texture, new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height),
                              new Rect2(0, 0, img.Width, img.Height), drawColour);
    }

    private void DrawOverlay(int frame, float px, float py, Color tint)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return;
        var img = BodyLibrary.Images[frame];
        if (img == null || img.OverlayWidth <= 0 || img.OverlayHeight <= 0) return;
        var texture = BodyLibrary.GetOverlayTexture(frame);
        if (texture == null) return;
        DrawTextureRectRegion(texture,
            new Rect2(px + img.OffSetX, py + img.OffSetY, img.OverlayWidth, img.OverlayHeight),
            new Rect2(0, 0, img.OverlayWidth, img.OverlayHeight), tint);
    }

    private void DrawName()
    {
        if (Type == Kind.Item && !Focused) return;
        // 对象节点的坐标是脚底基线。原版按当前帧的真实图像顶部放置
        // 名称；固定 -64 会让大型怪物名字压进身体，小型 NPC 又漂得过高。
        float y;
        if (Type == Kind.Item)
            y = -18f;
        else if (BodyLibrary?.Images != null && BodyFrame >= 0 && BodyFrame < BodyLibrary.Images.Length
            && BodyLibrary.Images[BodyFrame] != null)
        {
            var image = BodyLibrary.Images[BodyFrame];
            y = Math.Min(-52f, image.OffSetY - 5f);
        }
        else
            y = -64f;
        if (string.IsNullOrWhiteSpace(DisplayName)) return;
        RenderPrimitives.DrawLabel(this, DisplayName, new Vector2(0f, y), NameColour, 9f);
        if (!string.IsNullOrWhiteSpace(GuildName))
            RenderPrimitives.DrawLabel(this, GuildName, new Vector2(0f, y - 12f), new Color(0.8f, 0.8f, 0.4f), 8f);
        if (!string.IsNullOrWhiteSpace(PetOwner))
            RenderPrimitives.DrawLabel(this, $"({PetOwner})", new Vector2(0f, y + 12f), new Color(0.7f, 0.9f, 0.7f), 8f);
        if (Poison != PoisonType.None)
            DrawCircle(new Vector2(0f, y - 7f), 3f, new Color(0.35f, 1f, 0.35f, 0.85f));
        if (!string.IsNullOrWhiteSpace(ChatText) && Godot.Time.GetTicksMsec() < _chatUntil)
            RenderPrimitives.DrawLabel(this, ChatText, new Vector2(0f, y - 18f), Colors.White, 9f);
    }

    public void SetChat(string text)
    {
        ChatText = text;
        _chatUntil = Godot.Time.GetTicksMsec() + 5000;
        QueueRedraw();
    }
}
