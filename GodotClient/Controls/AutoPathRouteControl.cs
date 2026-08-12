using System;
using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>原版 AutoPathRouteControl：在小地图/大地图上绘制路径点、当前进度和终点编号。</summary>
public sealed partial class AutoPathRouteControl : DXControl
{
    private IReadOnlyList<AutoPathRoute> _routes = System.Array.Empty<AutoPathRoute>();
    private int _mapIndex = -1;
    private int _progressMap = -1;
    private int _progressPoint = -1;
    private float _scaleX = 1f, _scaleY = 1f;

    public AutoPathRouteControl()
    {
        IsControl = false;
        PassThrough = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetRoutes(IReadOnlyList<AutoPathRoute> routes, int mapIndex, int progressMap, int progressPoint, float scaleX, float scaleY)
    {
        _routes = routes ?? System.Array.Empty<AutoPathRoute>();
        _mapIndex = mapIndex;
        _progressMap = progressMap;
        _progressPoint = progressPoint;
        _scaleX = scaleX;
        _scaleY = scaleY;
        QueueRedraw();
    }

    protected override void DrawControl()
    {
        foreach (var route in _routes)
        {
            var leg = route?.Legs?.Find(x => x.MapIndex == _mapIndex);
            if (leg?.Points == null || leg.Points.Count == 0) continue;

            Color routeColour = leg.MapIndex != _progressMap
                ? new Color(.5f, .5f, .5f, .9f)
                : new Color(1f, 1f, 1f, .95f);
            DrawRouteDots(leg.Points, routeColour);

            Vector2 waypoint = route.DestinationMapIndex == _mapIndex
                ? Project(route.Destination)
                : Project(leg.Points[leg.Points.Count - 1]);
            DrawWaypoint(waypoint, route.WaypointNumber, routeColour);
        }
    }

    private void DrawRouteDots(IReadOnlyList<System.Drawing.Point> points, Color routeColour)
    {
        const float spacing = 6f;
        if (points.Count < 2) return;

        Vector2 segmentStart = Project(points[0]);
        float distanceToNext = spacing;
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 segmentEnd = Project(points[i]);
            float segmentLength = segmentStart.DistanceTo(segmentEnd);
            if (segmentLength <= 0.001f) continue;

            while (segmentLength >= distanceToNext)
            {
                float ratio = distanceToNext / segmentLength;
                segmentStart = segmentStart.Lerp(segmentEnd, ratio);
                int pointIndex = i;
                bool passed = _mapIndex == _progressMap && pointIndex <= _progressPoint;
                if (!passed)
                {
                    DrawRect(new Rect2(segmentStart - new Vector2(2, 2), new Vector2(4, 4)), Colors.Black);
                    DrawRect(new Rect2(segmentStart - new Vector2(1, 1), new Vector2(2, 2)), routeColour);
                }
                segmentLength = segmentStart.DistanceTo(segmentEnd);
                distanceToNext = spacing;
            }

            distanceToNext -= segmentLength;
            segmentStart = segmentEnd;
        }
    }

    private void DrawWaypoint(Vector2 centre, int waypointNumber, Color colour)
    {
        // 原版 WaypointBackground/WaypointBorder 是 15x15 的像素镂空标记。
        DrawRect(new Rect2(centre - new Vector2(5, 7), new Vector2(10, 1)), Colors.Black);
        DrawRect(new Rect2(centre - new Vector2(7, 5), new Vector2(14, 10)), Colors.Black);
        DrawRect(new Rect2(centre - new Vector2(5, 7), new Vector2(10, 1)), colour);
        DrawRect(new Rect2(centre - new Vector2(6, 5), new Vector2(12, 10)), colour);
        var font = MirSkin.GetFont();
        if (font != null)
            DrawString(font, centre + new Vector2(-3, 4), waypointNumber.ToString(), HorizontalAlignment.Left, -1, MirSkin.ScaledSize(10), Colors.Black);
    }

    private Vector2 Project(System.Drawing.Point point)
        => new(point.X * _scaleX, point.Y * _scaleY);
}
