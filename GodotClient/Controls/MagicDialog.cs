using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using ZirconClient.Formats;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 技能列表窗口 (移植自 Client/Scenes/Views/MagicDialog.cs 精简版)。
/// 列出已学技能 (GameScene.UserMagics), 每行: 图标 + 名称 + 等级 + 当前栏组键位。
/// 暂不做: 职业分 Tab、拖拽设键位、经验条细节、技能升级。
/// 打开: Q 键 (KeyBindAction.MagicWindow=E)。
/// </summary>
public partial class MagicDialog : DXWindow
{
    private readonly List<MagicCellView> _cells = new();

    public MagicDialog()
    {
        HasTitle = true;
        Text = "技能";
        Size = new Vector2I(400, 360);

        var bg = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 164,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddControl(bg);

        var close = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)ClientArea.Size.X - 30, 3),
        };
        close.MouseClick += (o, e) => Visible = false;
        AddControl(close);

        Visible = false;
    }

    /// <summary>从 GameScene.UserMagics 刷新技能列表。</summary>
    public void Refresh()
    {
        var game = GameScene.Game;
        if (game == null) return;

        // 清旧
        foreach (var c in _cells) c.QueueFree();
        _cells.Clear();

        var magics = game.UserMagics.Values.Where(m => m != null).ToList();
        int y = 30;
        foreach (var m in magics)
        {
            var cell = new MagicCellView(m, game.MagicBarSpellSet);
            cell.Position = new Vector2(15, y);
            AddChild(cell);
            _cells.Add(cell);
            y += 58;
        }

        // 窗口高度随技能数
        int h = System.Math.Max(360, (int)y + 30);
        Size = new Vector2I((int)Size.X, h);
    }
}

/// <summary>单个技能行 (移植自 MagicCell, 精简)。</summary>
public partial class MagicCellView : Control
{
    private readonly ClientUserMagic _magic;
    private ZlLibrary _iconLib;

    public MagicCellView(ClientUserMagic magic, int spellSet)
    {
        _magic = magic;
        MouseFilter = MouseFilterEnum.Pass;
        Size = new Vector2(369, 54);
    }

    public override void _Ready()
    {
        _iconLib = LibraryCache.Get(LibraryFile.MagicIcon);
    }

    public override void _Draw()
    {
        if (_magic?.Info == null) return;
        // 行背景
        DrawRect(new Rect2(0, 0, 369, 54), new Color(0, 0, 0, 0.4f), true);

        // 图标
        var tex = _iconLib?.GetImageTexture(_magic.Info.Icon);
        if (tex != null)
            DrawTextureRect(tex, new Rect2(9, 9, 36, 36), false);

        // 名称
        DrawString(MirSkin.GetFont(), new Vector2(54, 18), _magic.Info.Name ?? "", fontSize: 13);

        // 等级
        DrawString(MirSkin.GetFont(), new Vector2(54, 36), $"Lv.{_magic.Level}", fontSize: 11,
            modulate: new Color(0.8f, 0.8f, 0.8f));

        // 当前栏组键位
        var game = GameScene.Game;
        if (game != null)
        {
            var key = game.MagicBarSpellSet switch
            {
                1 => _magic.Set1Key,
                2 => _magic.Set2Key,
                3 => _magic.Set3Key,
                4 => _magic.Set4Key,
                _ => Library.SpellKey.None,
            };
            if (key != Library.SpellKey.None)
            {
                int slot = (int)key;  // Spell01=1 ... Spell08=8
                DrawString(MirSkin.GetFont(), new Vector2(330, 18), $"F{slot}", fontSize: 13,
                    modulate: new Color(1f, 0.85f, 0.3f));
            }
        }
    }
}
