using System;
using System.Collections.Generic;
using Godot;
using Library;
using S = Library.Network.ServerPackets;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 FishingDialog(Interface 220)：钓鱼装备五格，与角色装备数组共用。</summary>
public sealed partial class FishingDialog : DXWindow
{
    private readonly List<DXItemCell> _equipmentCells = new();

    public IReadOnlyList<DXItemCell> EquipmentCells => _equipmentCells;

    public FishingDialog()
    {
        HasTitle = false; HasFooter = false; Movable = true; Size = new Vector2I(224, 268);
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 220, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(194, 3) };
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);

        AddEquipmentCell(EquipmentSlot.Hook, 14, 169, 221);
        AddEquipmentCell(EquipmentSlot.Float, 14, 209, 222);
        AddEquipmentCell(EquipmentSlot.Bait, 54, 209, 224);
        AddEquipmentCell(EquipmentSlot.Finder, 94, 209, 223);
        AddEquipmentCell(EquipmentSlot.Reel, 134, 209, 225);
    }

    private void AddEquipmentCell(EquipmentSlot slot, int x, int y, int emptyIndex)
    {
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = emptyIndex, Location = new Vector2I(x, y), IsControl = false, MouseFilter = MouseFilterEnum.Ignore });
        var cell = new DXItemCell { Location = new Vector2I(x, y), GridType = GridType.Equipment, Slot = (int)slot, ItemGrid = GameScene.Game?.Equipment ?? new ClientUserItem[Globals.EquipmentSize] };
        AddControl(cell);
        _equipmentCells.Add(cell);
    }

    public bool AuditLayout(out string details)
    {
        bool cells = _equipmentCells.Count == 5
            && _equipmentCells[0].Location == new Vector2I(14, 169)
            && _equipmentCells[1].Location == new Vector2I(14, 209)
            && _equipmentCells[4].Location == new Vector2I(134, 209);
        details = $"size={Size} cells={_equipmentCells.Count} hook={_equipmentCells[0].Location} reel={_equipmentCells[4].Location}";
        return Size == new Vector2I(224, 268) && cells;
    }
}

/// <summary>原版 FishingCatchDialog(Interface 230)：抛竿/收线状态、动态鱼钩、进度与自动抛竿。</summary>
public sealed partial class FishingCatchDialog : DXWindow
{
    private readonly DXControl _catchBar;
    private readonly DXControl _catchInnerBar;
    private readonly DXImageControl _catchBarTexture;
    private readonly DXControl _progressBar;
    private readonly DXImageControl _movingPointer;
    private readonly DXImageControl _throwDistancePointer;
    private readonly DXImageControl _progressTexture;
    private readonly DXImageControl _fishFoundBase, _fishFoundCircle;
    private readonly DXButton _fishFoundButton;
    private readonly DXCheckButton _autoCastCheckBox;
    private int _pointsCurrent;
    private int _pointsRequired;
    private int _throwQuality;
    private int _movementSpeed;
    private int _requiredAccuracy;
    private int _playerLocation;
    private int _fishLocation;
    private bool _fishingStarted;
    private bool _fishDirectionRight;
    private bool _pressed;
    private double _nextUpdateMs;

    private const int PointerXStart = 10;
    private const int PlayerPointerY = 65;
    private const int ThrowDistancePointerY = 82;
    private const int FishBlockSize = 25;
    private const int FishBlocksTotal = 4;
    private const int FishMaxTotal = FishBlockSize * FishBlocksTotal;

    public bool AutoCast => _autoCastCheckBox.Checked;
    public FishingState State { get; private set; }

    public DXCheckButton AutoCastCheckBox => _autoCastCheckBox;
    public bool CaughtFish => State == FishingState.Cast && Math.Abs(_fishLocation - _playerLocation) <= _requiredAccuracy;

    public bool IsActive => State != FishingState.None && State != FishingState.Cancel;

    public bool AuditLayout(out string details)
    {
        bool bars = _catchBar.Location == new Vector2I(19, 76)
            && _catchBar.Size == new Vector2I(216, 12)
            && _progressBar.Location == new Vector2I(19, 91)
            && _progressTexture.Index == 232
            && _movingPointer.Index == 233
            && _throwDistancePointer.Index == 234;
        bool fish = _fishFoundBase.Index == 4500 && _fishFoundCircle.Index == 4501
            && _fishFoundButton.Index == 4510 && _fishFoundButton.Size == new Vector2I(32, 30);
        details = $"size={Size} catch={_catchBar.Location}/{_catchBar.Size} progress={_progressBar.Location} pointer={_movingPointer.Index}/{_throwDistancePointer.Index} fish={_fishFoundBase.Index}/{_fishFoundButton.Index}";
        return Size == new Vector2I(252, 144) && bars && fish && _autoCastCheckBox.Location == new Vector2I(164, 47);
    }

    public FishingCatchDialog()
    {
        HasTitle = false; HasFooter = false; Size = new Vector2I(252, 144);
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 230, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(222, 3) };
        close.MouseClick += (s, e) => Cancel(); AddControl(close);
        _autoCastCheckBox = new DXCheckButton("自动抛竿") { Location = new Vector2I(164, 47), Size = new Vector2I(18, 18), Checked = true };
        AddControl(_autoCastCheckBox);
        AddControl(new DXLabel { Text = "自动抛竿", FontSize = 9, Location = new Vector2I(184, 48), Size = new Vector2I(60, 18), IsControl = false });

        _catchBar = new DXControl { Location = new Vector2I(19, 76), Size = new Vector2I(216, 12), MouseFilter = MouseFilterEnum.Ignore };
        _catchInnerBar = new DXControl { Location = Vector2I.Zero, Size = new Vector2I(1, 12), Visible = false, MouseFilter = MouseFilterEnum.Ignore };
        _catchBarTexture = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 231, IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        _catchInnerBar.AddControl(_catchBarTexture);
        _catchBar.AddControl(_catchInnerBar);
        AddControl(_catchBar);
        _progressBar = new DXControl { Location = new Vector2I(19, 91), Size = new Vector2I(1, 8), Clip = true, MouseFilter = MouseFilterEnum.Ignore };
        _progressTexture = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 232, IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        _progressBar.AddControl(_progressTexture);
        AddControl(_progressBar);
        _movingPointer = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 233, Location = new Vector2I(PointerXStart, PlayerPointerY), IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        _throwDistancePointer = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 234, Location = new Vector2I(PointerXStart, ThrowDistancePointerY), IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(_movingPointer); AddControl(_throwDistancePointer);

        _fishFoundBase = new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 4500, Location = new Vector2I(105, 102), IsControl = false };
        _fishFoundCircle = new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 4501, Location = new Vector2I(105, 102), IsControl = false };
        _fishFoundButton = new DXButton { LibraryFile = LibraryFile.GameInter, Index = 4510, HoverIndex = 4511, Location = new Vector2I(111, 108), Size = new Vector2I(32, 30), Visible = false };
        _fishFoundButton.MouseClick += (s, e) => Reel();
        _fishFoundButton.MouseDown += (s, e) => _pressed = true;
        _fishFoundButton.MouseUp += (s, e) => _pressed = false;
        _fishFoundButton.MouseLeave += (s, e) => _pressed = false;
        AddControl(_fishFoundBase); AddControl(_fishFoundCircle); AddControl(_fishFoundButton);
    }

    public void UpdateStats(S.FishingStats p)
    {
        if (p == null) return;
        _pointsCurrent = p.CurrentPoints;
        _pointsRequired = p.RequiredPoints;
        _movementSpeed = p.MovementSpeed;
        if (p.ThrowQuality > -1) _throwQuality = p.ThrowQuality;
        if (p.RequiredAccuracy > -1) _requiredAccuracy = p.RequiredAccuracy;
        _autoCastCheckBox.Enabled = p.CanAutoCast;
        if (!_autoCastCheckBox.Enabled) _autoCastCheckBox.Checked = false;
        UpdateFishingVisuals();
    }

    public void SetState(FishingState state, bool fishFound)
    {
        State = state;
        Visible = state == FishingState.Cast;
        if (state != FishingState.Cast)
        {
            _fishingStarted = false;
            _pressed = false;
            _playerLocation = _fishLocation = 0;
        }
        _fishFoundButton.Visible = state == FishingState.Cast && fishFound;
        _catchBar.Visible = _progressBar.Visible = _movingPointer.Visible = _throwDistancePointer.Visible = state == FishingState.Cast;
        _catchInnerBar.Visible = state == FishingState.Cast && fishFound;
        UpdateFishingVisuals();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Visible || State != FishingState.Cast) return;
        if (!_fishingStarted)
        {
            _fishingStarted = true;
            _nextUpdateMs = Godot.Time.GetTicksMsec();
        }
        if (!_fishFoundButton.Visible) { UpdateFishingVisuals(); return; }

        double now = Godot.Time.GetTicksMsec();
        if (now < _nextUpdateMs) return;
        _nextUpdateMs = now + 50;
        int speed = Mathf.Max(1, _movementSpeed);
        if (_pressed) _playerLocation = Mathf.Min(_playerLocation + speed, FishMaxTotal);
        else _playerLocation = Mathf.Max(_playerLocation - speed, 0);
        if (GD.Randf() < 1f / 7f) _fishDirectionRight = !_fishDirectionRight;
        if (_fishDirectionRight) _fishLocation = Mathf.Min(_fishLocation + speed, FishMaxTotal);
        else _fishLocation = Mathf.Max(_fishLocation - speed, 0);
        UpdateFishingVisuals();
    }

    private void Reel()
    {
        if (State != FishingState.Cast) return;
        if (State != FishingState.Cast || !_fishFoundButton.Visible) return;
        GameScene.Game?.SendFishingCast(FishingState.Reel, CaughtFish);
    }

    private void Cancel()
    {
        GameScene.Game?.SendFishingCast(FishingState.Cancel);
        Visible = false;
    }

    private void UpdateFishingVisuals()
    {
        if (_throwDistancePointer == null) return;
        int throwDistance = Mathf.Clamp(FishBlockSize * _throwQuality, 0, FishMaxTotal);
        _throwDistancePointer.Location = new Vector2I(PointerXStart + throwDistance * 216 / FishMaxTotal, ThrowDistancePointerY);

        int current = _fishLocation;
        _movingPointer.Location = new Vector2I(PointerXStart + current * 215 / FishMaxTotal, PlayerPointerY);
        int progressWidth = _pointsRequired <= 0 ? 0 : Mathf.Clamp(_pointsCurrent * 216 / _pointsRequired, 0, 216);
        _progressBar.Size = new Vector2I(progressWidth, 8);

        int left = _playerLocation - _requiredAccuracy;
        int width = Mathf.Max(1, _requiredAccuracy * 2);
        int x = Mathf.Clamp(left * 216 / FishMaxTotal, 0, 215);
        int w = Mathf.Clamp(width * 216 / FishMaxTotal, 1, 216 - x);
        _catchInnerBar.Location = new Vector2I(x, 0);
        _catchInnerBar.Size = new Vector2I(w, 12);
        _catchBarTexture.Location = new Vector2I(-x, 0);
        _catchBarTexture.Opacity = CaughtFish ? 1f : .5f;
    }
}
