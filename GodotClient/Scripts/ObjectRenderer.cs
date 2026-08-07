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
    public enum Kind { Monster, NPC, Item }

    public Kind Type;
    public ZlLibrary BodyLibrary;
    public int BodyShape;   // 怪物: MonsterLookup 形状; NPC: NPCInfo.Image; 物品: 0
    public int BodyOffSet;  // 怪物: 1000; NPC: 100; 物品: 0
    public int DrawImage;   // 物品专用: Ground 图库帧号 (无方向/形状)
    public string DisplayName;
    public Color NameColour = Colors.White;
    public PoisonType Poison;
    public bool Focused;
    public int Light;

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

        var r = new ObjectRenderer
        {
            Type = Kind.Monster,
            DisplayName = mi.MonsterName,
            NameColour = p.NameColour == System.Drawing.Color.Empty ? Colors.White : ToGodot(p.NameColour),
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
        SetAnimation(MirAnimation.Standing);
    }

    private bool _debugLogged;
    private bool _decodeErrorLogged;

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
            RenderPrimitives.DrawGroundShadow(this, 20f, 6f, 0f, 2f, 0.32f);
            DrawItemImage(DrawImage, 0, 0);
        }
        else
        {
            if (!DrawResourceShadow(BodyFrame))
                RenderPrimitives.DrawGroundShadow(this, Type == Kind.NPC ? 21f : 28f,
                    Type == Kind.NPC ? 7f : 10f, 0f, 2f, 0.44f);
            DrawLayer(BodyFrame, 0, 0);
        }
        DrawName();
        DrawHealthBar();
    }

    public int BodyFrame => DrawFrame + BodyShape * BodyOffSet;

    private bool DrawResourceShadow(int frame)
    {
        if (frame < 0 || frame >= BodyLibrary.Images.Length) return false;
        var img = BodyLibrary.Images[frame];
        if (img == null || img.ShadowWidth <= 0 || img.ShadowHeight <= 0) return false;
        var texture = BodyLibrary.GetShadowTexture(frame);
        if (texture == null) return false;
        var dest = new Rect2(img.ShadowOffSetX, img.ShadowOffSetY,
            img.ShadowWidth, img.ShadowHeight);
        DrawTextureRectRegion(texture, dest, new Rect2(0, 0, img.ShadowWidth, img.ShadowHeight),
            new Color(0f, 0f, 0f, 0.52f));
        return true;
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

    private void DrawLayer(int frame, float px, float py)
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
        DrawTextureRectRegion(texture, new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height),
                              new Rect2(0, 0, img.Width, img.Height));
    }

    private void DrawName()
    {
        if (Type == Kind.Item && !Focused) return;
        float y = Type == Kind.Item ? -18f : -64f;
        RenderPrimitives.DrawLabel(this, DisplayName, new Vector2(0f, y), NameColour, 9f);
        if (Poison != PoisonType.None)
            DrawCircle(new Vector2(0f, y - 7f), 3f, new Color(0.35f, 1f, 0.35f, 0.85f));
    }
}
