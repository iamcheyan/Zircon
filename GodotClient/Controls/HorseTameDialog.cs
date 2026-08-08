using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 HorseTameDialog：套索提示、进度条与成功回包；作为世界层上的透明提示控件。
/// 完整移植旧版角度判定 (Client/Scenes/Views/HorseTameDialog.cs: 随机延迟显示目标角度 ->
/// 点击套索当前帧 == 目标角度 +10~20 否则 -10~20 -> 满 100 发 C.TamingSuccess)。</summary>
public sealed partial class HorseTameDialog : DXControl
{
    private const int LoopBaseIndex = 7600;
    private const int ResultBaseIndex = 7610;
    private const int AngleBaseIndex = 7620;
    private const int AngleCount = 10;
    private const int MaximumInitialProgress = 50;
    private const int ResultDurationMs = 400;

    private readonly DXControl _progress;
    private readonly DXAnimatedControl _lasso;
    private readonly DXImageControl _progressOutline;
    private readonly DXImageControl _angleImage;
    private uint _targetObjectID;
    private int _value;
    private int _targetAngle;
    private bool _promptVisible;
    private bool _completed;

    public HorseTameDialog()
    {
        Size = new Vector2I(80, 48);
        IsControl = true;
        Visible = false;
        _lasso = new DXAnimatedControl
        {
            LibraryFile = LibraryFile.GameInter,
            BaseIndex = LoopBaseIndex,
            Index = LoopBaseIndex,
            FrameCount = AngleCount,
            AnimationDelay = System.TimeSpan.FromMilliseconds(200),
            Location = Vector2I.Zero,
            Loop = true,
            Animated = false,
            PassThrough = false,
            IsControl = true,
        };
        _lasso.MouseClick += (s, e) => TryLasso();
        AddControl(_lasso);
        _angleImage = new DXImageControl
        {
            LibraryFile = LibraryFile.GameInter,
            Index = AngleBaseIndex,
            Location = Vector2I.Zero,
            IsControl = false,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        AddControl(_angleImage);
        _progress = new DXControl { Location = new Vector2I(2, 42), Size = new Vector2I(76, 6), Clip = true, IsControl = false };
        _progress.AddControl(new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 7630, IsControl = false, MouseFilter = MouseFilterEnum.Ignore });
        AddControl(_progress);
        _progressOutline = new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = 7631, Location = new Vector2I(2, 42), IsControl = false, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(_progressOutline);
    }

    public void SetTarget(uint objectID, Vector2 screenPosition)
    {
        _targetObjectID = objectID;
        _value = new Random(unchecked((int)objectID)).Next(MaximumInitialProgress + 1);
        _completed = false;
        _promptVisible = false;
        _angleImage.Visible = false;
        Position = screenPosition - new Vector2(Size.X / 2f, Size.Y + 52);
        _lasso.Animated = false;
        _lasso.BaseIndex = LoopBaseIndex;
        _lasso.Index = LoopBaseIndex;
        _lasso.AnimationStart = System.DateTime.MinValue;
        _lasso.Animated = true;
        Visible = objectID != 0;
        UpdateProgress();
        if (Visible) StartNextRound();
    }

    public void SetState(TamingState state)
    {
        if (state == TamingState.Cancel || state == TamingState.None)
        {
            _targetObjectID = 0;
            _completed = true;
            _promptVisible = false;
            _angleImage.Visible = false;
            Visible = false;
            return;
        }
        _lasso.Visible = true;
    }

    private void StartNextRound()
    {
        if (!Visible || _completed || _targetObjectID == 0) return;
        _promptVisible = false;
        _angleImage.Visible = false;
        _lasso.AnimationStart = System.DateTime.MinValue;
        _lasso.Animated = true;
        // 旧版: 1000~5000ms 随机延迟后显示目标角度提示
        double delay = Random.Shared.Next(1000, 5001) / 1000.0;
        var timer = GetTree().CreateTimer(delay);
        timer.Timeout += () => { if (Visible && !_completed) ShowPrompt(); };
    }

    private void ShowPrompt()
    {
        _targetAngle = Random.Shared.Next(AngleCount);
        _promptVisible = true;
        _angleImage.Index = AngleBaseIndex + _targetAngle;
        _angleImage.Visible = true;
    }

    private void TryLasso()
    {
        if (_targetObjectID == 0 || !_promptVisible || _completed) return;
        int clickedAngle = Math.Min(AngleCount - 1, Math.Max(0, _lasso.Index - LoopBaseIndex));
        int change = Random.Shared.Next(10, 21);
        if (clickedAngle == _targetAngle)
            _value = Math.Min(100, _value + change);
        else
            _value = Math.Max(0, _value - change);
        UpdateProgress();
        _promptVisible = false;
        _angleImage.Visible = false;
        _lasso.Animated = false;
        _lasso.Index = ResultBaseIndex + clickedAngle;
        if (_value < 100)
        {
            // 结果动画展示 ResultDuration 后进入下一轮
            var timer = GetTree().CreateTimer(ResultDurationMs / 1000.0);
            timer.Timeout += () => { if (Visible && !_completed) StartNextRound(); };
            return;
        }
        _completed = true;
        GameScene.Game?.SendTamingSuccess(_targetObjectID);
        _targetObjectID = 0;
        Visible = false;
    }

    private void UpdateProgress() => _progress.Size = new Vector2I(Mathf.Clamp(76 * _value / 100, 0, 76), 6);

    public bool AuditLayout(out string details)
    {
        details = $"size={Size} lasso={_lasso.Position}/{_lasso.Size} progress={_progress.Position}/{_progress.Size} outline={_progressOutline.Position}/{_progressOutline.Size}";
        return Size == new Vector2I(80, 48)
            && _progress.Position == new Vector2I(2, 42)
            && _progress.Size == new Vector2I(76, 6)
            && _progressOutline.Position == new Vector2I(2, 42)
            && MirSkin.GetSize(LibraryFile.GameInter, 7630) == new Vector2I(76, 4)
            && MirSkin.GetSize(LibraryFile.GameInter, 7631) == new Vector2I(76, 6);
    }
}
