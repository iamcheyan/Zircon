using System;
using Godot;

namespace ZirconClient.Scripts;

public partial class DamagePopupNode : Node2D
{
    private string _text = string.Empty;
    private Color _colour = Colors.White;
    private double _start;
    private const double Duration = 900.0;

    public void Setup(int value, bool critical)
    {
        _text = Math.Abs(value).ToString();
        _colour = critical ? new Color(1f, 0.85f, 0.15f) : new Color(1f, 0.35f, 0.25f);
        _start = Time.GetTicksMsec();
        ZIndex = 4095;  // Godot 4 上限 4096, 用最大值保证飘字在最顶层
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        double age = Time.GetTicksMsec() - _start;
        if (age >= Duration) { QueueFree(); return; }
        Position -= new Vector2(0f, (float)(delta * 18.0));
        QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = 1f - Mathf.Clamp((float)((Time.GetTicksMsec() - _start) / Duration), 0f, 1f);
        RenderPrimitives.DrawLabel(this, _text, new Vector2(0f, 0f), new Color(_colour, alpha), 11f);
    }
}
