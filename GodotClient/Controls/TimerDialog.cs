using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.Network.ServerPackets;

namespace ZirconClient.Controls;

/// <summary>原版 TimerDialog：只显示最近到期的计时器，按服务器 SetTimer 包刷新。</summary>
public sealed partial class TimerDialog : DXControl
{
    private sealed class TimerEntry
    {
        public string Key;
        public byte Type;
        public DateTime End;
    }

    private readonly Dictionary<string, TimerEntry> _timers = new();
    private readonly DXAnimatedControl _eggTimer;
    private readonly DXImageControl[] _digits = new DXImageControl[4];
    private readonly DXImageControl _colon;

    public TimerDialog()
    {
        PassThrough = true;
        Size = new Vector2I(120, 100);
        _eggTimer = new DXAnimatedControl
        {
            LibraryFile = LibraryFile.GameInter,
            BaseIndex = 960,
            Index = 960,
            FrameCount = 6,
            AnimationDelay = TimeSpan.FromMilliseconds(333),
            Location = new Vector2I(23, 0),
            UseOffSet = true,
            PassThrough = true,
            IsControl = false,
            Animated = true,
            Loop = false,
            Visible = false
        };
        AddControl(_eggTimer);
        for (int i = 0; i < _digits.Length; i++)
        {
            _digits[i] = new DXImageControl
            {
                LibraryFile = LibraryFile.GameInter,
                Index = 6580,
                Location = new Vector2I(i < 2 ? i * 25 : (i + 1) * 25, 70),
                UseOffSet = true,
                Visible = false,
                PassThrough = true,
                IsControl = false
            };
            AddControl(_digits[i]);
        }
        _colon = new DXImageControl
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 6590,
            Location = new Vector2I(50, 70),
            UseOffSet = true,
            Visible = false,
            PassThrough = true,
            IsControl = false
        };
        AddControl(_colon);
        Visible = false;
    }

    public void AddTimer(SetTimer packet)
    {
        if (packet == null || string.IsNullOrEmpty(packet.Key)) return;
        if (packet.Seconds <= 0) { _timers.Remove(packet.Key); return; }
        _timers[packet.Key] = new TimerEntry { Key = packet.Key, Type = packet.Type, End = DateTime.UtcNow.AddSeconds(packet.Seconds) };
        Visible = true;
    }

    public void ExpireTimer(string key)
    {
        if (!string.IsNullOrEmpty(key)) _timers.Remove(key);
    }

    public override void _Process(double delta)
    {
        var current = _timers.Values.OrderBy(x => x.End).FirstOrDefault();
        if (current == null) { Visible = false; return; }
        var remaining = current.End - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) { _timers.Remove(current.Key); return; }
        int seconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        int leftA, leftB, rightA, rightB;
        if (seconds >= 3600)
        {
            int hours = seconds / 3600;
            leftA = hours / 10; leftB = hours % 10;
            int minutes = seconds / 60 % 60;
            rightA = minutes / 10; rightB = minutes % 10;
        }
        else
        {
            int minutes = seconds / 60;
            leftA = minutes / 10; leftB = minutes % 10;
            rightA = seconds / 10 % 6; rightB = seconds % 10;
        }
        int[] values = { leftA, leftB, rightA, rightB };
        for (int i = 0; i < values.Length; i++) { _digits[i].Index = 6580 + values[i]; _digits[i].Visible = true; }
        _colon.Visible = true;
        if (current.Type == 0)
        {
            _eggTimer.Visible = false;
        }
        else
        {
            _eggTimer.Visible = true;
            _eggTimer.Animated = false;
            _eggTimer.BaseIndex = 6600;
            _eggTimer.Index = 6600;
            _eggTimer.Loop = true;
            _eggTimer.AnimationStart = DateTime.MinValue;
            _eggTimer.Animated = true;
        }
    }
}
