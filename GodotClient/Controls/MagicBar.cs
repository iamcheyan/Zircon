using System.Collections.Generic;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;
using ZirconClient.Formats;

/// <summary>
/// 魔法快捷栏 (移植自 Client/Scenes/Views/MagicBarDialog.cs 精简版)。
/// 显示当前栏组 (SpellSet 1~4) 的 8 个槽 (Spell01~Spell08 <- F1~F8),
/// 每槽画 MagicInfo.Icon (MIcon.Zl 图库) + 键名。
/// GameScene 持有 UserMagics (MagicInfo -> ClientUserMagic), 本控件只读渲染。
/// </summary>
public partial class MagicBar : Control
{
    private const int SlotSize = 34;
    private const int SlotGap = 4;
    private readonly GameScene _game;
    private readonly ZlLibrary _iconLib;

    public MagicBar(GameScene game)
    {
        _game = game;
        _iconLib = LibraryCache.Get(LibraryFile.MagicIcon);
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(10, 0);  // GameScene.LayoutHud 会重新定位
        Size = new Vector2((SlotSize + SlotGap) * 8 + SlotGap, SlotSize + 18);
    }

    public override void _Draw()
    {
        if (_game == null) return;
        // 取当前栏组的 8 个槽 (SpellKey.Spell01~08 对应当前 SpellSet 的 SetXKey)
        var set = _game.MagicBarSpellSet;
        var slots = GetSlotsForSet(set);
        for (int i = 0; i < 8; i++)
        {
            float x = SlotGap + i * (SlotSize + SlotGap);
            // 槽背景
            DrawRect(new Rect2(x, 0, SlotSize, SlotSize), new Color(0, 0, 0, 0.6f), filled: true);
            DrawRect(new Rect2(x, 0, SlotSize, SlotSize), new Color(0.4f, 0.35f, 0.2f, 0.8f), filled: false, width: 1);

            var magic = slots[i];
            if (magic == null) continue;
            // 图标
            var img = _iconLib?.GetImageTexture(magic.Info.Icon);
            if (img != null)
            {
                int iw = img.GetWidth();
                int ih = img.GetHeight();
                float ix = x + (SlotSize - iw) / 2f;
                float iy = (SlotSize - ih) / 2f;
                DrawTextureRect(img, new Rect2(ix, iy, iw, ih), false);
            }
        }
    }

    /// <summary>当前栏组下 8 个槽对应的 ClientUserMagic (null=空槽)。</summary>
    private List<ClientUserMagic> GetSlotsForSet(int set)
    {
        var result = new List<ClientUserMagic>(8);
        // Spell01~08 对应 SpellKey.Spell01..Spell08
        for (int i = 0; i < 8; i++)
        {
            var key = (Library.SpellKey)(i + 1);  // SpellKey.Spell01 = 1
            ClientUserMagic found = null;
            foreach (var kv in _game.UserMagics)
            {
                var m = kv.Value;
                if (m == null) continue;
                if (set == 1 && m.Set1Key == key) found = m;
                else if (set == 2 && m.Set2Key == key) found = m;
                else if (set == 3 && m.Set3Key == key) found = m;
                else if (set == 4 && m.Set4Key == key) found = m;
                if (found != null) break;
            }
            result.Add(found);
        }
        return result;
    }

    public void Refresh() => QueueRedraw();
}