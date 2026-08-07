using System;
using Godot;
using Library;
using Library.SystemModels;

namespace ZirconClient.Scripts;

// 地图光照层：环境光 + .map 格子光。绘制在所有世界对象之上、UI 之下。
// 采用 Godot 2D 的轻量叠加实现，保持原版 LightSetting 的主要视觉语义。
public partial class MapLightLayer : Node2D
{
    private const float WorldScale = 2f;
    private MapInfo _mapInfo;
    private MapView _mapView;
    private float _dayTime = 1f;

    public void SetMap(MapInfo info, MapView view)
    {
        _mapInfo = info;
        _mapView = view;
        QueueRedraw();
    }

    public void SetDayTime(float dayTime)
    {
        _dayTime = Math.Clamp(dayTime, 0f, 1f);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_mapInfo != null) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_mapInfo == null || _mapView?.Map == null) return;

        Vector2 viewport = GetViewport().GetVisibleRect().Size / WorldScale;
        float ambient = _mapInfo.Light switch
        {
            LightSetting.Light => 1f,
            LightSetting.Night => 0.06f,
            LightSetting.Twilight => 0.39f,
            _ => _dayTime,
        };

        if (ambient < 0.999f)
            DrawRect(new Rect2(Vector2.Zero, viewport), new Color(0f, 0f, 0f, 1f - ambient));

        // 格子光用柔和圆形叠加近似原版 LightLayer 的光晕。
        int minX = Math.Max(0, _mapView.CenterX - _mapView.ViewRangeX - 15);
        int maxX = Math.Min(_mapView.Map.Width - 1, _mapView.CenterX + _mapView.ViewRangeX + 15);
        int minY = Math.Max(0, _mapView.CenterY - _mapView.ViewRangeY - 15);
        int maxY = Math.Min(_mapView.Map.Height - 1, _mapView.CenterY + _mapView.ViewRangeY + 15);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        {
            ref var cell = ref _mapView.Map.Cells[x, y];
            if (cell.Light <= 0) continue;

            Vector2 center = _mapView.CellToScreen(x, y, false) + new Vector2(24, 16);
            float radius = 18f + cell.Light * 30f * 0.02f;
            float alpha = Math.Clamp(cell.Light * 0.018f, 0.05f, 0.35f);
            DrawCircle(center, radius, new Color(1f, 0.9f, 0.62f, alpha));
        }
    }
}
