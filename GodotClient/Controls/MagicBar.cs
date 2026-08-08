using System;
using System.Collections.Generic;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Formats;
using ZirconClient.Controls;
using ZirconClient.Scripts;

/// <summary>原版 MagicBarDialog 的 Godot 绘制：12 列、最多 24 槽、按学校显示边框。</summary>
public partial class MagicBar : Control
{
    private const int IconSize = 36;
    private const int IconsPerRow = 12;
    private const int GroupSpacing = 5;
    private readonly GameScene _game;
    private readonly ZlLibrary _iconLib;
    private readonly DXButton _upButton;
    private readonly DXButton _downButton;
    private readonly DXLabel _setLabel;
    private bool _dragging;

    /// <summary>旧版 MagicBarDialog 是可移动窗口；布局刷新不能覆盖用户拖动后的位置。</summary>
    public bool UserMoved { get; private set; }

    public MagicBar(GameScene game)
    {
        _game = game;
        _iconLib = LibraryCache.Get(LibraryFile.MagicIcon);
        // 技能槽本身需要接收点击；上下翻栏按钮仍由子控件处理。
        MouseFilter = MouseFilterEnum.Stop;
        // 原版 MagicBarDialog.Opacity = 0.6，图标本身另有 0.6 透明度。
        // 保留父级透明度，避免快捷栏比原版过亮。
        Modulate = new Color(1f, 1f, 1f, 0.6f);
        Position = new Vector2(10, 0);
        // Client/Scenes/Views/MagicBarDialog.cs: frame on uses 49/46,
        // frame off uses 37/36. The extra 20px is reserved for set controls.
        Size = new Vector2(BarWidth(), BarHeight(1));
        if (ClientSettings.MagicBarPosition.X >= 0 && ClientSettings.MagicBarPosition.Y >= 0)
        {
            Position = ClientSettings.MagicBarPosition;
            UserMoved = true;
        }

        _upButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 44,
            Location = new Vector2I((int)Size.X - 17, 0),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _upButton.MouseClick += (o, e) =>
        {
            _game.MagicBarSpellSet = Mathf.Max(1, _game.MagicBarSpellSet - 1);
            Refresh();
        };
        AddChild(_upButton);

        _downButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 46,
            Location = new Vector2I((int)Size.X - 17, 30),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _downButton.MouseClick += (o, e) =>
        {
            _game.MagicBarSpellSet = Mathf.Min(4, _game.MagicBarSpellSet + 1);
            Refresh();
        };
        AddChild(_downButton);

        _setLabel = new DXLabel
        {
            Text = "1",
            FontSize = 10,
            TextColour = Colors.White,
            IsControl = false,
            Size = new Vector2I(18, 20),
            Align = HorizontalAlignment.Center,
            Location = new Vector2I((int)Size.X - 18, 14),
        };
        AddChild(_setLabel);
    }

    public override void _Draw()
    {
        if (_game == null) return;

        var slots = GetSlotsForSet(_game.MagicBarSpellSet);
        _setLabel.Text = _game.MagicBarSpellSet.ToString();
        // 原版 MagicBarDialog 始终显示第一排 12 个槽位；只有
        // Spell13~Spell24 中存在绑定时才展开第二排。
        int maxSlot = IconsPerRow;
        for (int i = 0; i < slots.Count; i++)
            if (i >= IconsPerRow && slots[i] != null) maxSlot = 24;

        int rows = maxSlot > IconsPerRow ? 2 : 1;
        Size = new Vector2(BarWidth(), BarHeight(rows));
        _setLabel.Position = new Vector2(Size.X - 16, Size.Y / 2f - 9);
        _upButton.Position = new Vector2(Size.X - 15, _setLabel.Position.Y - 9);
        _downButton.Position = new Vector2(Size.X - 15, _setLabel.Position.Y + 15);

        for (int i = 0; i < maxSlot; i++)
        {
            bool frames = _game.ShowMagicBarFrames;
            int slotSize = frames ? 46 : 36;
            int slotSpacing = frames ? 49 : 37;
            int frameWidth = frames ? 48 : 36;
            int column = i % IconsPerRow;
            int row = i / IconsPerRow;
            float x = column * slotSpacing + (column / 4) * GroupSpacing;
            float y = row * (slotSpacing + 5);
            var magic = slots[i];

            if (magic == null)
            {
                var emptyBorder = frames ? MirSkin.GetTexture(LibraryFile.GameInter2, SchoolBorderIndex(MagicSchool.None)) : null;
                if (emptyBorder != null)
                    DrawTextureRect(emptyBorder, new Rect2(x, y, frameWidth, slotSize), false);
                else
                    DrawRect(new Rect2(x, y, frameWidth, frames ? slotSize : frameWidth), new Color(0.45f, 0.35f, 0.2f), false, 1);
                continue;
            }

            var border = frames ? MirSkin.GetTexture(LibraryFile.GameInter2, SchoolBorderIndex(magic.Info.School)) : null;
            if (border != null) DrawTextureRect(border, new Rect2(x, y, frameWidth, slotSize), false);
            else DrawRect(new Rect2(x, y, frameWidth, frames ? slotSize : frameWidth), new Color(0.45f, 0.35f, 0.2f), false, 1);

            var icon = _iconLib?.GetImageTexture(magic.Info.Icon);
            if (icon != null)
            {
                float ix = x + (frames ? 6 : 0) + (IconSize - icon.GetWidth()) / 2f;
                float iy = y + (frames ? 5 : 0) + (IconSize - icon.GetHeight()) / 2f;
                DrawTextureRect(icon, new Rect2(ix, iy, icon.GetWidth(), icon.GetHeight()), false,
                    new Color(1f, 1f, 1f, 0.6f));
            }

            var font = MirSkin.GetFont();
            if (font != null)
                DrawString(font, new Vector2(x + IconSize - 12, y + IconSize - 4),
                    (i + 1).ToString(), HorizontalAlignment.Left, -1, 9, Colors.White);

            // 原版 MagicBarDialog 在技能图标上覆盖冷却层；不能只在施法时
            // 刷一次，否则倒计时会停在旧数字。
            bool toggleSkill = magic.Info.Magic is
                MagicType.Thrusting or MagicType.HalfMoon or MagicType.DestructiveSurge or
                MagicType.FlameSplash or MagicType.FullBloom or MagicType.WhiteLotus or
                MagicType.RedLotus or MagicType.SweetBrier or MagicType.Karma;
            var nextCast = magic.NextCast > _game.ToggleTime || !toggleSkill
                ? magic.NextCast
                : _game.ToggleTime;
            var remaining = nextCast - Library.Time.Now;
            if (remaining > TimeSpan.Zero)
            {
                DrawRect(new Rect2(x + (frames ? 7 : 1), y + (frames ? 6 : 1), IconSize - 2, IconSize - 2), new Color(0.08f, 0.08f, 0.08f, .68f));
                var colour = remaining.TotalSeconds > 5 ? Colors.Gold : Colors.Red;
                if (font != null)
                    DrawString(font, new Vector2(x + (frames ? 18 : 12), y + (frames ? 5 : 0) + IconSize / 2f + 4),
                        $"{Math.Ceiling(remaining.TotalSeconds)}s", HorizontalAlignment.Left, -1, 10, colour);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Visible) QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton dragButton && dragButton.ButtonIndex == MouseButton.Left)
        {
            if (dragButton.Pressed)
            {
                _dragging = true;
            }
            else
            {
                _dragging = false;
                if (UserMoved)
                {
                    ClientSettings.MagicBarPosition = new Vector2I((int)Position.X, (int)Position.Y);
                    ClientSettings.Save();
                }
            }
            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseMotion motion && _dragging)
        {
            Position += motion.Relative;
            UserMoved = true;
            ClampToViewport();
            AcceptEvent();
            return;
        }

        if (@event is not InputEventMouseButton mouse || !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left) return;

        var slots = GetSlotsForSet(_game.MagicBarSpellSet);
        int slotSpacing = _game.ShowMagicBarFrames ? 49 : 37;
        int slotSize = _game.ShowMagicBarFrames ? 46 : 36;
        for (int i = 0; i < slots.Count; i++)
        {
            int column = i % IconsPerRow;
            int row = i / IconsPerRow;
            float x = column * slotSpacing + (column / 4) * GroupSpacing;
            float y = row * (slotSpacing + 5);
            if (slots[i] != null && new Rect2(x, y, slotSize, slotSize).HasPoint(mouse.Position))
            {
                _game.UseMagicSlot(i);
                AcceptEvent();
                return;
            }
        }
    }

    private List<ClientUserMagic> GetSlotsForSet(int set)
    {
        var result = new List<ClientUserMagic>(24);
        for (int i = 0; i < 24; i++)
        {
            var key = (SpellKey)(i + 1);
            ClientUserMagic found = null;
            foreach (var pair in _game.UserMagics)
            {
                var magic = pair.Value;
                if (magic == null) continue;
                if (set == 1 && magic.Set1Key == key || set == 2 && magic.Set2Key == key ||
                    set == 3 && magic.Set3Key == key || set == 4 && magic.Set4Key == key)
                {
                    found = magic;
                    break;
                }
            }
            result.Add(found);
        }
        return result;
    }

    private static int SchoolBorderIndex(MagicSchool school) => school switch
    {
        MagicSchool.Passive => 860,
        MagicSchool.Active => 861,
        MagicSchool.Toggle => 862,
        MagicSchool.Fire => 870,
        MagicSchool.Ice => 871,
        MagicSchool.Lightning => 872,
        MagicSchool.Wind => 873,
        MagicSchool.Phantom => 874,
        MagicSchool.Holy => 880,
        MagicSchool.Dark => 881,
        MagicSchool.Physical => 883,
        MagicSchool.Atrocity => 890,
        MagicSchool.Kill => 891,
        MagicSchool.Assassination => 892,
        MagicSchool.Discipline or MagicSchool.Horse => 815,
        _ => 815,
    };

    public void Refresh() => QueueRedraw();

    private void ClampToViewport()
    {
        Vector2 logicalViewport = GetViewport().GetVisibleRect().Size / GameScene.UiScale;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, Mathf.Max(0, logicalViewport.X - Size.X)),
            Mathf.Clamp(Position.Y, 0, Mathf.Max(0, logicalViewport.Y - Size.Y)));
    }

    private float BarWidth() => (_game?.ShowMagicBarFrames == false ? 37 : 49) * IconsPerRow + 15 + 20;
    private float BarHeight(int rows) => _game?.ShowMagicBarFrames == false
        ? (rows == 2 ? 37 * 2 + 5 : 37)
        : (rows == 2 ? 46 * 2 + 5 + 3 : 46);
}
