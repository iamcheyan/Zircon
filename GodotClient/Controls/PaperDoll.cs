using System.Linq;
using Godot;
using Library;
using ZirconClient.Scripts;
using ZirconClient.Formats;

namespace ZirconClient.Controls;

/// <summary>
/// 角色面板人物纸娃娃 (移植自 Client/Scenes/Views/CharacterDialog.cs
/// CharacterTab_BeforeChildrenDraw, 行 2555-2700)。
///
/// 逐层叠加绘制 (从底到顶):
///   1. ProgUse[1160]  刺客女特殊发型 (Class=Assassin & Female & HairType=1 & 无头盔)
///   2. ProgUse[0男/1女] 裸身肤色
///   3. Equip[costume.Image] 时装 / Equip[armour.Image](+overlay 染色) 衣服
///   4. Equip[weapon.Image](+overlay) 武器
///   5. Equip[shield.Image](+overlay) 盾
///   6. Equip[helmet.Image](+overlay) 头盔
/// 坐标 (130, 270) 相对窗口 (原版 CharacterTab 内坐标)。
///
/// 数据来源: GameScene.StartInfo (Gender/Class/HairType/HairColour) +
///           GameScene.Equipment (装备数组) + HideBody/HideWeapon (骑马时)。
/// </summary>
public partial class PaperDoll : Control
{
    private const int DollX = 130;
    private const int DollY = 270;

    private ZlLibrary _progUse;
    private ZlLibrary _equip;

    public PaperDoll()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(DollX, DollY);
        // 给个尺寸避免被裁; 实际靠贴图 OffSet 定位
        Size = new Vector2(180, 220);
    }

    public override void _Ready()
    {
        _progUse = LibraryCache.Get(LibraryFile.ProgUse);
        _equip = LibraryCache.Get(LibraryFile.Equip);
    }

    public override void _Draw()
    {
        var game = GameScene.Game;
        if (game == null) return;
        var info = game.StartInfo;
        if (info == null) return;
        var eq = game.Equipment;
        if (eq == null) return;

        bool hideBody = info.Horse != Library.HorseType.None;   // 骑马时只露头
        bool hideWeapon = hideBody;
        bool hideHead = false;

        var weapon = eq[(int)EquipmentSlot.Weapon];
        var armour = eq[(int)EquipmentSlot.Armour];
        var helmet = eq[(int)EquipmentSlot.Helmet];
        var shield = eq[(int)EquipmentSlot.Shield];
        var costume = eq[(int)EquipmentSlot.Costume];

        // 1. 刺客女特殊发型
        if (!hideBody && info.Class == MirClass.Assassin && info.Gender == MirGender.Female
            && info.HairType == 1 && helmet == null)
        {
            DrawImage(_progUse, 1160, ToGodot(info.HairColour));
        }

        // 2. 裸身 (男0/女1)
        if (!hideBody)
        {
            int bodyIndex = info.Gender == MirGender.Male ? 0 : 1;
            DrawImage(_progUse, bodyIndex, Colors.White);
        }

        // 3. 衣服 / 时装
        if (_equip != null)
        {
            if (costume != null)
            {
                DrawImage(_equip, costume.Info.Image, Colors.White);
            }
            else if (armour != null)
            {
                DrawImage(_equip, armour.Info.Image, Colors.White);
                DrawImageOverlay(_equip, armour.Info.Image, ToGodot(armour.Colour));
            }

            // 4. 武器
            if (!hideWeapon && weapon != null)
            {
                DrawImage(_equip, weapon.Info.Image, Colors.White);
                DrawImageOverlay(_equip, weapon.Info.Image, ToGodot(weapon.Colour));
            }

            // 5. 盾
            if (!hideWeapon && shield != null)
            {
                DrawImage(_equip, shield.Info.Image, Colors.White);
                DrawImageOverlay(_equip, shield.Info.Image, ToGodot(shield.Colour));
            }

            // 6. 头盔
            if (!hideHead && helmet != null)
            {
                DrawImage(_equip, helmet.Info.Image, Colors.White);
                DrawImageOverlay(_equip, helmet.Info.Image, ToGodot(helmet.Colour));
            }
        }
    }

    /// <summary>画一帧普通图 (用贴图自带 OffSet 定位)。</summary>
    private void DrawImage(ZlLibrary lib, int index, Color colour)
    {
        if (lib == null || index < 0) return;
        var tex = lib.GetImageTexture(index);
        if (tex == null) return;
        var img = lib.Images[index];
        // 贴图 OffSet 是相对"锚点"的; 我们锚点在 Position (130,270)
        Vector2 pos = new(img.OffSetX, img.OffSetY);
        // Godot DrawTextureRect 不支持染色; 用 DrawTexture (单色乘) 替代
        if (colour == Colors.White)
            DrawTextureRect(tex, new Rect2(pos, img.Width, img.Height), false);
        else
            DrawTextureRect(tex, new Rect2(pos, img.Width, img.Height), false, colour);
    }

    /// <summary>画 overlay 层 (染色叠加)。原版 ImageType.Overlay 是第二张图。</summary>
    private void DrawImageOverlay(ZlLibrary lib, int index, Color colour)
    {
        if (lib == null || index < 0) return;
        var img = lib.Images[index];
        // overlay 图在 Zl 里通常是同索引的 Overlay 帧; ZlReader 暂未分离 Overlay,
        // 用半透明染色重画同一张图近似 (色调由 colour 决定)。
        var tex = lib.GetImageTexture(index);
        if (tex == null) return;
        Vector2 pos = new(img.OffSetX, img.OffSetY);
        DrawTextureRect(tex, new Rect2(pos, img.Width, img.Height), false, colour);
    }

    private static Color ToGodot(System.Drawing.Color c)
        => new Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
}