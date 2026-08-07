using System;
using System.Collections.Generic;
using Godot;
using Library;
using Library.Network;
using Library.SystemModels;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

// 玩家外观渲染 (移植自 Client/Models/PlayerObject.cs DrawBody + UpdateLibraries)
// 分层顺序: 背武器/盾 -> 身体(ArmourFrame) -> 头(盔/发) -> 前武器
// 帧号公式 (4.3 节):
//   ArmourFrame = DrawFrame + (Costume>=0 ? Costume%10 : ArmourShape%11) * ArmourShapeOffSet + ArmourShift
//   HairFrame   = DrawFrame + (HairType-1) * 5000
//   WeaponFrame = DrawFrame + (WeaponShape%10) * 5000
//   DrawFrame   = FrameIndex + StartIndex + OffSet * (int)Direction   (OffSet=10 每方向)
public partial class PlayerRenderer : Node2D
{
    // ---- 外观状态 (与 ObjectPlayer/StartInformation 字段对应) ----
    public MirClass Class;
    public MirGender Gender;
    public int HairType = 1;
    public Godot.Color HairColour = Colors.Black;
    public int ArmourShape;
    public Godot.Color ArmourColour = Colors.White;
    public int CostumeShape = -1;
    public int HelmetShape = 0;
    public int ShieldShape = -1;
    public int LibraryWeaponShape;
    public bool HideHead;
    public HorseType Horse = HorseType.None;
    public int HorseShape;
    public bool Dead;

    // ---- M5 战斗: 玩家血量 (DataObjectHealthMana/MaxHealthMana) ----
    public int Health;
    public int MaxHealth;
    public int MaxMana;
    public bool ShowHealthBar;
    public string DisplayName;
    public Color NameColour = Colors.White;
    public int Light;

    // ---- 动画状态 ----
    public MirDirection Direction;
    public MirAnimation Animation = MirAnimation.Standing;
    public int FrameIndex;
    public double FrameStartMs;   // 本帧序列开始时间
    private Frame _currentFrame;

    // 一次性动作动画 (Combat/Struck): 播完回 Standing
    private MirAnimation _oneShotAnim = MirAnimation.Standing;

    // 移动插值 (格子坐标 -> 屏幕偏移)
    public int CellX, CellY;          // 服务端权威格子坐标
    public float OffsetX, OffsetY;    // 平滑移动的像素偏移

    private const int CellWidth = 48;
    private const int CellHeight = 32;

    private ZlLibrary _bodyLib, _hairLib, _helmetLib, _weaponLib1, _weaponLib2, _shieldLib;
    private ZlLibrary _horseLib, _horseEffectLib;

    public void UpdateAppearance(StartInformation info)
    {
        Class = info.Class;
        Gender = info.Gender;
        HairType = info.HairType;
        HairColour = ToGodot(info.HairColour);
        ArmourShape = info.Armour;
        ArmourColour = ToGodot(info.ArmourColour);
        CostumeShape = info.Costume;
        HelmetShape = info.HelmetShape;
        ShieldShape = info.Shield;
        LibraryWeaponShape = info.Weapon;
        HideHead = info.HideHead;
        Horse = info.Horse;
        HorseShape = info.HorseShape;
        Direction = info.Direction;
        Dead = info.Dead;
        RefreshLibraries();
        SetAnimation(Animation);
        QueueRedraw();
    }

    public void UpdateAppearance(Library.Network.ServerPackets.ObjectPlayer info)
    {
        Class = info.Class;
        Gender = info.Gender;
        HairType = info.HairType;
        HairColour = ToGodot(info.HairColour);
        ArmourShape = info.Armour;
        ArmourColour = ToGodot(info.ArmourColour);
        CostumeShape = info.Costume;
        HelmetShape = info.Helmet;
        ShieldShape = info.Shield;
        LibraryWeaponShape = info.Weapon;
        HideHead = info.HideHead;
        Horse = info.Horse;
        HorseShape = info.HorseShape;
        Direction = info.Direction;
        Dead = info.Dead;
        RefreshLibraries();
        SetAnimation(Animation);
        QueueRedraw();
    }

    private static Godot.Color ToGodot(System.Drawing.Color c)
    {
        return new Godot.Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    }

    // 切换动画帧表 (Start/Count/OffSet), 参考 FrameSet.Players
    public void SetAnimation(MirAnimation anim)
    {
        Animation = anim;
        _currentFrame = GetFrameTable(anim);
        FrameStartMs = Godot.Time.GetTicksMsec(); // 从当前时刻起播, 保证从第 0 帧开始
        FrameIndex = 0;
        // 一次性动作: 播完回 Standing (Die 保持最后一帧)
        _oneShotAnim = anim is MirAnimation.Combat1 or MirAnimation.Combat2 or MirAnimation.Combat3
            or MirAnimation.Combat4 or MirAnimation.Combat5 or MirAnimation.Combat6 or MirAnimation.Combat7
            or MirAnimation.Combat8 or MirAnimation.Combat9 or MirAnimation.Combat10 or MirAnimation.Combat11
            or MirAnimation.Combat12 or MirAnimation.Combat13 or MirAnimation.Combat14 or MirAnimation.Combat15
            or MirAnimation.Struck or MirAnimation.Pushed or MirAnimation.Harvest ? anim : MirAnimation.Standing;
        QueueRedraw();
    }

    // M5 战斗: 玩家攻击/受击/死亡动画
    public void PlayCombat(MagicType magic)
    {
        SetAnimation(CombatForMagic(magic));
    }

    public void PlayStruck() => SetAnimation(MirAnimation.Struck);

    public void PlayDie()
    {
        SetAnimation(MirAnimation.Die);
        Dead = true;
    }

    // 普通攻击 = Combat3 (战士默认), 技能按原客户端 GetAttackAnimation 简化
    private static MirAnimation CombatForMagic(MagicType magic) => MirAnimation.Combat3;

    private Frame GetFrameTable(MirAnimation anim)
    {
        // 行走/站立用 Players 表; 马/其他用 DefaultMonster 兜底
        switch (anim)
        {
            case MirAnimation.Standing:
            case MirAnimation.Walking:
            case MirAnimation.Running:
            case MirAnimation.Struck:
            case MirAnimation.Die:
            case MirAnimation.Dead:
            case MirAnimation.Pushed:
            case MirAnimation.Combat1:
            case MirAnimation.Combat2:
            case MirAnimation.Combat3:
            case MirAnimation.Combat4:
            case MirAnimation.Combat5:
            case MirAnimation.Combat6:
            case MirAnimation.Combat7:
            case MirAnimation.Combat8:
            case MirAnimation.Combat9:
            case MirAnimation.Combat10:
            case MirAnimation.Combat11:
            case MirAnimation.Combat12:
            case MirAnimation.Combat13:
            case MirAnimation.Combat14:
            case MirAnimation.Combat15:
            case MirAnimation.Stance:
            case MirAnimation.Harvest:
                if (FrameSet.Players.TryGetValue(anim, out var pf)) return pf;
                break;
            case MirAnimation.HorseStanding:
            case MirAnimation.HorseWalking:
            case MirAnimation.HorseRunning:
            case MirAnimation.HorseStruck:
                if (FrameSet.Players.TryGetValue(anim, out var hf)) return hf;
                break;
        }
        return FrameSet.DefaultMonster[MirAnimation.Standing];
    }

    // 由 FrameSet.Frame 结构: Start/Count/OffSet/Delays
    private int GetFrameIndex(double nowMs, bool loop)
    {
        if (_currentFrame == null) return 0;
        if (_currentFrame.FrameCount <= 1) return 0;

        double sum = _currentFrame.Sum;
        double elapsed = nowMs - FrameStartMs;
        int frame = 0;
        double acc = 0;
        for (int i = 0; i < _currentFrame.FrameCount; i++)
        {
            acc += _currentFrame.Delays[i].TotalMilliseconds;
            if (elapsed < acc) { frame = i; break; }
            frame = i;
        }
        if (elapsed >= sum)
        {
            if (loop) frame = (int)((elapsed - (elapsed % sum)) / sum) % _currentFrame.FrameCount; // 简单循环
            else frame = _currentFrame.FrameCount - 1;
        }
        return frame;
    }

    public override void _Process(double delta)
    {
        double nowMs = Godot.Time.GetTicksMsec();
        int frame = GetFrameIndex(nowMs, _oneShotAnim == MirAnimation.Standing);
        if (frame != FrameIndex)
        {
            FrameIndex = frame;
            QueueRedraw();
        }

        // 一次性动作播完回 Standing (死亡保持 Die 帧)
        if (_oneShotAnim != MirAnimation.Standing && !Dead)
        {
            var f = _currentFrame;
            if (f != null && nowMs - FrameStartMs >= f.Sum)
            {
                _oneShotAnim = MirAnimation.Standing;
                SetAnimation(MirAnimation.Standing);
            }
        }
    }

    // 帧号计算 (4.3 节)
    private int DrawFrame => FrameIndex + _currentFrame.StartIndex + _currentFrame.OffSet * (int)Direction;
    private int ArmourShapeOffSet => Class == MirClass.Assassin ? 3000 : 5000;
    private int ArmourShift => 0; // 刺客专用, 暂不实现
    private int ArmourFrame => DrawFrame + (CostumeShape >= 0 ? (CostumeShape % 10) : (ArmourShape % 11)) * ArmourShapeOffSet + ArmourShift;
    private int HairFrame => DrawFrame + (HairType - 1) * 5000;
    private int HelmetFrame => DrawFrame + ((HelmetShape - 1) % 10) * ArmourShapeOffSet + ArmourShift;
    private int WeaponFrame => DrawFrame + (WeaponShape % 10) * 5000;
    private int ShieldFrame => DrawFrame + (ShieldShape % 10) * ArmourShapeOffSet + ArmourShift;
    private int WeaponShape => LibraryWeaponShape >= 1000 ? LibraryWeaponShape - 1000 : LibraryWeaponShape;

    private static readonly HashSet<int> CostumeShapeHideWeapon = new() { 6, 7, 8, 9, 10, 11, 12, 13, 16, 17, 18 };

    private bool _debugLogged;

    public override void _Draw()
    {
        if (_bodyLib == null) return;
        if (!_debugLogged)
        {
            _debugLogged = true;
            GD.Print($"[PlayerView] 首帧诊断: body={_bodyLib.FileName} hair={_hairLib?.FileName ?? "null"} " +
                     $"helmet={_helmetLib?.FileName ?? "null"} weapon1={_weaponLib1?.FileName ?? "null"} " +
                     $"dir={Direction} frame={FrameIndex} anim={Animation} " +
                     $"DrawFrame={DrawFrame} ArmourFrame={ArmourFrame} HairFrame={HairFrame} " +
                     $"Cell=({CellX},{CellY}) Pos={Position}");
        }
        DrawShadow();
        DrawPlayerAt(0, 0);
        if (!string.IsNullOrWhiteSpace(DisplayName))
            RenderPrimitives.DrawLabel(this, DisplayName, new Vector2(0f, -76f), NameColour, 9f);

        // 玩家头顶血条
        if (ShowHealthBar && !Dead && MaxHealth > 0)
        {
            float percent = Math.Clamp(Health / (float)MaxHealth, 0f, 1f);
            if (percent > 0f)
            {
                const float w = 48, h = 6;
                float x = -w / 2, y = -70;
                DrawRect(new Rect2(x - 1, y - 1, w + 2, h + 2), new Color(0f, 0f, 0f, 0.75f));
                var col = percent > 0.5f ? new Color(0f, 0.8f, 0.29f)
                        : percent > 0.25f ? new Color(0.9f, 0.8f, 0.1f)
                        : new Color(0.9f, 0.2f, 0.1f);
                DrawRect(new Rect2(x, y, w * percent, h), col);
            }
        }
    }

    // 供 GameScene 调用: 计算本节点屏幕位置
    public void ComputeScreenPos(int camCenterX, int camCenterY, int viewRangeX, int viewRangeY, float screenOffsetX, float screenOffsetY)
    {
        Position = new Vector2(
            (CellX - camCenterX + viewRangeX) * CellWidth + screenOffsetX + OffsetX,
            (CellY - camCenterY + viewRangeY + 1) * CellHeight + screenOffsetY - 34 + OffsetY
        );
    }

    private void DrawPlayerAt(float px, float py)
    {
        bool hideWeapon = CostumeShapeHideWeapon.Contains(CostumeShape);

        // 坐骑必须位于人物所有装备层之前，阴影单独绘制在人物和坐骑的共同基线。
        if (Animation is MirAnimation.HorseStanding or MirAnimation.HorseWalking
            or MirAnimation.HorseRunning or MirAnimation.HorseStruck)
            DrawHorse(px, py);

        // 1. 背武器 (Up/DownLeft/Left/UpLeft 方向)
        if (!hideWeapon)
        {
            if (Direction is MirDirection.Up or MirDirection.DownLeft or MirDirection.Left or MirDirection.UpLeft)
                DrawLayer(_weaponLib1, WeaponFrame, px, py);

            // 2. 背盾 (UpRight/Right/DownRight)
            if (ShieldShape >= 0 && Direction is MirDirection.UpRight or MirDirection.Right or MirDirection.DownRight)
                DrawLayer(_shieldLib, ShieldFrame, px, py);
        }

        // 3. 身体
        DrawLayer(_bodyLib, ArmourFrame, px, py);
        if (ArmourColour != Colors.White)
            DrawOverlay(_bodyLib, ArmourFrame, px, py, ArmourColour);

        // 4. 头 (盔优先, 否则发)
        if (!HideHead)
        {
            if (HelmetShape > 0)
                DrawLayer(_helmetLib, HelmetFrame, px, py);
            else if (HairType > 0)
                DrawLayer(_hairLib, HairFrame, px, py, HairColour);
        }

        // 5. 前武器 (UpRight/Right/DownRight/Down)
        if (!hideWeapon)
        {
            if (Direction is MirDirection.UpRight or MirDirection.Right or MirDirection.DownRight or MirDirection.Down)
                DrawLayer(_weaponLib1, WeaponFrame, px, py);
        }
    }

    private void DrawShadow()
    {
        bool horse = Animation is MirAnimation.HorseStanding or MirAnimation.HorseWalking
            or MirAnimation.HorseRunning or MirAnimation.HorseStruck;
        bool resourceShadow = false;
        if (horse && _horseLib != null)
            resourceShadow = DrawResourceShadow(_horseLib, DrawFrame + ((int)Horse - 1) * 5000);
        if (!resourceShadow)
            resourceShadow = DrawResourceShadow(_bodyLib, ArmourFrame);
        if (!resourceShadow)
            RenderPrimitives.DrawGroundShadow(this, horse ? 42f : 27f, horse ? 12f : 9f,
                0f, horse ? 7f : 2f, 0.46f);
    }

    private bool DrawResourceShadow(ZlLibrary lib, int frame)
    {
        if (lib == null || frame < 0 || frame >= lib.Images.Length) return false;
        var img = lib.Images[frame];
        if (img == null || img.ShadowWidth <= 0 || img.ShadowHeight <= 0) return false;
        var texture = lib.GetShadowTexture(frame);
        if (texture == null) return false;
        DrawTextureRectRegion(texture,
            new Rect2(img.ShadowOffSetX, img.ShadowOffSetY, img.ShadowWidth, img.ShadowHeight),
            new Rect2(0, 0, img.ShadowWidth, img.ShadowHeight),
            new Color(0f, 0f, 0f, 0.5f));
        return true;
    }

    private void DrawHorse(float px, float py)
    {
        if (_horseLib == null) return;
        int frame = DrawFrame + ((int)Horse - 1) * 5000;
        if (HorseShape is >= 4 and <= 6) frame = DrawFrame;
        DrawLayer(_horseLib, frame, px, py);
        if (_horseEffectLib != null && HorseShape is 5 or 6)
            DrawLayer(_horseEffectLib, DrawFrame, px, py);
    }

    private void DrawLayer(ZlLibrary lib, int frame, float px, float py, Color? tint = null)
    {
        if (lib == null) return;
        if (frame < 0 || frame >= lib.Images.Length) return;
        if (lib.Images[frame] == null) return;

        var texture = lib.GetImageTexture(frame);
        if (texture == null) return;

        var img = lib.Images[frame];
        var dest = new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height);
        if (tint.HasValue)
            DrawTextureRectRegion(texture, dest, new Rect2(0, 0, img.Width, img.Height), tint.Value);
        else
            DrawTextureRectRegion(texture, dest, new Rect2(0, 0, img.Width, img.Height));
    }

    private void DrawOverlay(ZlLibrary lib, int frame, float px, float py, Color tint)
    {
        if (lib == null || frame < 0 || frame >= lib.Images.Length) return;
        var img = lib.Images[frame];
        if (img == null || img.OverlayWidth <= 0 || img.OverlayHeight <= 0) return;
        var texture = lib.GetOverlayTexture(frame);
        if (texture == null) return;
        DrawTextureRectRegion(texture,
            new Rect2(px + img.OffSetX, py + img.OffSetY, img.OverlayWidth, img.OverlayHeight),
            new Rect2(0, 0, img.OverlayWidth, img.OverlayHeight), tint);
    }

    // 库选择 (UpdateLibraries 移植)
    private void RefreshLibraries()
    {
        _weaponLib2 = null;
        _horseLib = null;
        _horseEffectLib = null;

        bool isFemale = Gender == MirGender.Female;
        bool isAssassin = Class == MirClass.Assassin;
        int femaleOff = isFemale ? 5000 : 0;
        int assassinOff = isAssassin ? 50000 : 0;

        // 身体 (ArmourList: ArmourShape/11)
        LibraryFile bodyFile = LibraryFile.M_Hum;
        if (isAssassin)
            bodyFile = isFemale ? LibraryFile.WM_HumA : LibraryFile.M_HumA;
        else
            bodyFile = isFemale ? LibraryFile.WM_Hum : LibraryFile.M_Hum;

        // 查 ArmourList 字典 (30 项: 键 ArmourShape/11 + 偏移)
        var armourKey = ArmourShape / 11 + femaleOff + (isAssassin ? assassinOff : 0);
        if (TryArmour(armourKey, out var armourFile))
            bodyFile = armourFile;

        // 时装优先
        if (CostumeShape >= 0)
        {
            var costumeKey = CostumeShape / 10 + femaleOff + (isAssassin ? assassinOff : 0);
            if (TryCostume(costumeKey, out var costumeFile))
                bodyFile = costumeFile;
            else
                bodyFile = isAssassin ? (isFemale ? LibraryFile.WM_HumA : LibraryFile.M_HumA)
                                      : (isFemale ? LibraryFile.WM_Hum : LibraryFile.M_Hum);
        }

        _bodyLib = LibraryCache.Get(bodyFile);

        // 发型
        _hairLib = LibraryCache.Get(isAssassin ? (isFemale ? LibraryFile.WM_HairA : LibraryFile.M_HairA)
                                                : (isFemale ? LibraryFile.WM_Hair : LibraryFile.M_Hair));

        // 头盔 (HelmetList: (HelmetShape-1)/10)
        if (HelmetShape > 0)
        {
            var helmetKey = (HelmetShape - 1) / 10 + femaleOff + (isAssassin ? assassinOff : 0);
            if (TryHelmet(helmetKey, out var helmetFile))
                _helmetLib = LibraryCache.Get(helmetFile);
            else
                _helmetLib = null;
        }
        else _helmetLib = null;

        // 武器 (WeaponList: LibraryWeaponShape/10)
        var weaponKey = LibraryWeaponShape / 10 + femaleOff;
        if (TryWeapon(weaponKey, out var weaponFile))
            _weaponLib1 = LibraryCache.Get(weaponFile);
        else
            _weaponLib1 = LibraryCache.Get(LibraryFile.M_Weapon1);

        if (LibraryWeaponShape >= 1200 && LibraryWeaponShape != 1263)
        {
            var rightKey = LibraryWeaponShape / 10 + femaleOff + 50;
            if (TryWeapon(rightKey, out var rightFile))
                _weaponLib2 = LibraryCache.Get(rightFile);
        }

        // 盾 (ShieldList: ShieldShape/10)
        _shieldLib = null;
        if (ShieldShape >= 0)
        {
            var shieldKey = ShieldShape / 10 + femaleOff;
            if (TryShield(shieldKey, out var shieldFile))
                _shieldLib = LibraryCache.Get(shieldFile);
        }

        switch (HorseShape)
        {
            case 1: _horseLib = LibraryCache.Get(LibraryFile.HorseIron); break;
            case 2: _horseLib = LibraryCache.Get(LibraryFile.HorseSilver); break;
            case 3: _horseLib = LibraryCache.Get(LibraryFile.HorseGold); break;
            case 4: _horseLib = LibraryCache.Get(LibraryFile.HorseBlue); break;
            case 5:
                _horseLib = LibraryCache.Get(LibraryFile.HorseDark);
                _horseEffectLib = LibraryCache.Get(LibraryFile.HorseDarkEffect);
                break;
            case 6:
                _horseLib = LibraryCache.Get(LibraryFile.HorseRoyal);
                _horseEffectLib = LibraryCache.Get(LibraryFile.HorseRoyalEffect);
                break;
            case 7:
                _horseLib = LibraryCache.Get(LibraryFile.HorseBlueDragon);
                _horseEffectLib = LibraryCache.Get(LibraryFile.HorseBlueDragonEffect);
                break;
            default: _horseLib = LibraryCache.Get(LibraryFile.Horse); break;
        }
        if (Horse == HorseType.None) _horseLib = null;
    }

    // ---- 装备库字典 (4.2 节) ----
    private static bool TryArmour(int key, out LibraryFile file)
    {
        switch (key)
        {
            case 0: file = LibraryFile.M_Hum; return true;
            case 1: file = LibraryFile.M_HumEx1; return true;
            case 2: file = LibraryFile.M_HumEx2; return true;
            case 3: file = LibraryFile.M_HumEx3; return true;
            case 4: file = LibraryFile.M_HumEx4; return true;
            case 10: file = LibraryFile.M_HumEx10; return true;
            case 11: file = LibraryFile.M_HumEx11; return true;
            case 12: file = LibraryFile.M_HumEx12; return true;
            case 13: file = LibraryFile.M_HumEx13; return true;
            case 20: file = LibraryFile.M_HumCx1; return true;
            case 5000: file = LibraryFile.WM_Hum; return true;
            case 5001: file = LibraryFile.WM_HumEx1; return true;
            case 5002: file = LibraryFile.WM_HumEx2; return true;
            case 5003: file = LibraryFile.WM_HumEx3; return true;
            case 5004: file = LibraryFile.WM_HumEx4; return true;
            case 5010: file = LibraryFile.WM_HumEx10; return true;
            case 5011: file = LibraryFile.WM_HumEx11; return true;
            case 5012: file = LibraryFile.WM_HumEx12; return true;
            case 5013: file = LibraryFile.WM_HumEx13; return true;
            case 5020: file = LibraryFile.WM_HumCx1; return true;
            case 50000: file = LibraryFile.M_HumA; return true;
            case 50001: file = LibraryFile.M_HumAEx1; return true;
            case 50002: file = LibraryFile.M_HumAEx2; return true;
            case 50003: file = LibraryFile.M_HumAEx3; return true;
            case 50020: file = LibraryFile.M_HumACx1; return true;
            case 55000: file = LibraryFile.WM_HumA; return true;
            case 55001: file = LibraryFile.WM_HumAEx1; return true;
            case 55002: file = LibraryFile.WM_HumAEx2; return true;
            case 55003: file = LibraryFile.WM_HumAEx3; return true;
            case 55020: file = LibraryFile.WM_HumACx1; return true;
            default: file = LibraryFile.M_Hum; return false;
        }
    }
    private static bool TryCostume(int key, out LibraryFile file)
    {
        switch (key)
        {
            case 0: file = LibraryFile.M_Costume; return true;
            case 1: file = LibraryFile.M_CostumeEx1; return true;
            case 5000: file = LibraryFile.WM_Costume; return true;
            case 5001: file = LibraryFile.WM_CostumeEx1; return true;
            case 50000: file = LibraryFile.M_CostumeA; return true;
            case 55000: file = LibraryFile.WM_CostumeA; return true;
            default: file = LibraryFile.M_Hum; return false;
        }
    }
    private static bool TryHelmet(int key, out LibraryFile file)
    {
        switch (key)
        {
            case 0: file = LibraryFile.M_Helmet1; return true;
            case 1: file = LibraryFile.M_Helmet2; return true;
            case 2: file = LibraryFile.M_Helmet3; return true;
            case 3: file = LibraryFile.M_Helmet4; return true;
            case 4: file = LibraryFile.M_Helmet5; return true;
            case 10: file = LibraryFile.M_Helmet11; return true;
            case 11: file = LibraryFile.M_Helmet12; return true;
            case 12: file = LibraryFile.M_Helmet13; return true;
            case 13: file = LibraryFile.M_Helmet14; return true;
            case 20: file = LibraryFile.M_HelmetCx1; return true;
            case 5000: file = LibraryFile.WM_Helmet1; return true;
            case 5001: file = LibraryFile.WM_Helmet2; return true;
            case 5002: file = LibraryFile.WM_Helmet3; return true;
            case 5003: file = LibraryFile.WM_Helmet4; return true;
            case 5004: file = LibraryFile.WM_Helmet5; return true;
            case 5010: file = LibraryFile.WM_Helmet11; return true;
            case 5011: file = LibraryFile.WM_Helmet12; return true;
            case 5012: file = LibraryFile.WM_Helmet13; return true;
            case 5013: file = LibraryFile.WM_Helmet14; return true;
            case 5020: file = LibraryFile.WM_HelmetCx1; return true;
            case 50000: file = LibraryFile.M_HelmetA1; return true;
            case 50001: file = LibraryFile.M_HelmetA2; return true;
            case 50002: file = LibraryFile.M_HelmetA3; return true;
            case 50003: file = LibraryFile.M_HelmetA4; return true;
            case 50020: file = LibraryFile.M_HelmetACx1; return true;
            case 55000: file = LibraryFile.WM_HelmetA1; return true;
            case 55001: file = LibraryFile.WM_HelmetA2; return true;
            case 55002: file = LibraryFile.WM_HelmetA3; return true;
            case 55003: file = LibraryFile.WM_HelmetA4; return true;
            case 55020: file = LibraryFile.WM_HelmetACx1; return true;
            default: file = LibraryFile.None; return false;
        }
    }
    private static bool TryWeapon(int key, out LibraryFile file)
    {
        // WeaponList (52 项): 0-15 基础 + 女版 + 右手版
        switch (key)
        {
            case 0: file = LibraryFile.M_Weapon1; return true;
            case 1: file = LibraryFile.M_Weapon2; return true;
            case 2: file = LibraryFile.M_Weapon3; return true;
            case 3: file = LibraryFile.M_Weapon4; return true;
            case 4: file = LibraryFile.M_Weapon5; return true;
            case 5: file = LibraryFile.M_Weapon6; return true;
            case 6: file = LibraryFile.M_Weapon7; return true;
            case 9: file = LibraryFile.M_Weapon10; return true;
            case 10: file = LibraryFile.M_Weapon11; return true;
            case 11: file = LibraryFile.M_Weapon12; return true;
            case 12: file = LibraryFile.M_Weapon13; return true;
            case 13: file = LibraryFile.M_Weapon14; return true;
            case 14: file = LibraryFile.M_Weapon15; return true;
            case 15: file = LibraryFile.M_Weapon16; return true;
            case 110: file = LibraryFile.M_WeaponAOH1; return true;
            case 111: file = LibraryFile.M_WeaponAOH2; return true;
            case 112: file = LibraryFile.M_WeaponAOH3; return true;
            case 113: file = LibraryFile.M_WeaponAOH4; return true;
            case 114: file = LibraryFile.M_WeaponAOH5; return true;
            case 115: file = LibraryFile.M_WeaponAOH6; return true;
            case 120: file = LibraryFile.M_WeaponADL1; return true;
            case 121: file = LibraryFile.M_WeaponADL2; return true;
            case 125: file = LibraryFile.M_WeaponADL6; return true;
            case 170: file = LibraryFile.M_WeaponADR1; return true;
            case 171: file = LibraryFile.M_WeaponADR2; return true;
            case 175: file = LibraryFile.M_WeaponADR6; return true;
            case 5000: file = LibraryFile.WM_Weapon1; return true;
            case 5001: file = LibraryFile.WM_Weapon2; return true;
            case 5002: file = LibraryFile.WM_Weapon3; return true;
            case 5003: file = LibraryFile.WM_Weapon4; return true;
            case 5004: file = LibraryFile.WM_Weapon5; return true;
            case 5005: file = LibraryFile.WM_Weapon6; return true;
            case 5006: file = LibraryFile.WM_Weapon7; return true;
            case 5009: file = LibraryFile.WM_Weapon10; return true;
            case 5010: file = LibraryFile.WM_Weapon11; return true;
            case 5011: file = LibraryFile.WM_Weapon12; return true;
            case 5012: file = LibraryFile.WM_Weapon13; return true;
            case 5013: file = LibraryFile.WM_Weapon14; return true;
            case 5014: file = LibraryFile.WM_Weapon15; return true;
            case 5015: file = LibraryFile.WM_Weapon16; return true;
            case 5110: file = LibraryFile.WM_WeaponAOH1; return true;
            case 5111: file = LibraryFile.WM_WeaponAOH2; return true;
            case 5112: file = LibraryFile.WM_WeaponAOH3; return true;
            case 5113: file = LibraryFile.WM_WeaponAOH4; return true;
            case 5114: file = LibraryFile.WM_WeaponAOH5; return true;
            case 5115: file = LibraryFile.WM_WeaponAOH6; return true;
            case 5120: file = LibraryFile.WM_WeaponADL1; return true;
            case 5121: file = LibraryFile.WM_WeaponADL2; return true;
            case 5125: file = LibraryFile.WM_WeaponADL6; return true;
            case 5170: file = LibraryFile.WM_WeaponADR1; return true;
            case 5171: file = LibraryFile.WM_WeaponADR2; return true;
            case 5175: file = LibraryFile.WM_WeaponADR6; return true;
            default: file = LibraryFile.M_Weapon1; return false;
        }
    }
    private static bool TryShield(int key, out LibraryFile file)
    {
        switch (key)
        {
            case 0: file = LibraryFile.M_Shield1; return true;
            case 1: file = LibraryFile.M_Shield2; return true;
            case 5000: file = LibraryFile.WM_Shield1; return true;
            case 5001: file = LibraryFile.WM_Shield2; return true;
            default: file = LibraryFile.None; return false;
        }
    }
}
