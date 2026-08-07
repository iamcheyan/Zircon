using Godot;

namespace ZirconClient.Scripts;

/// <summary>
/// 小型的世界绘制原语。世界使用逻辑 48x32 格，外层 GameScene 再统一放大，
/// 所以这里不能把 UI 的缩放值混进来。
/// </summary>
internal static class RenderPrimitives
{
    public static void DrawGroundShadow(CanvasItem canvas, float width = 26f, float height = 9f,
        float x = 0f, float y = 1f, float alpha = 0.42f)
    {
        int segments = 24;
        var points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.Tau * i / segments;
            points[i] = new Vector2(x + Mathf.Cos(a) * width * 0.5f,
                y + Mathf.Sin(a) * height * 0.5f);
        }
        canvas.DrawColoredPolygon(points, new Color(0f, 0f, 0f, alpha));
    }

    public static void DrawLabel(CanvasItem canvas, string text, Vector2 baseline,
        Color colour, float size = 10f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Font font = ThemeDB.FallbackFont;
        if (font == null) return;
        Vector2 extent = font.GetStringSize(text, HorizontalAlignment.Left, -1, (int)size);
        Vector2 p = baseline - new Vector2(extent.X * 0.5f, 0f);
        canvas.DrawString(font, p + new Vector2(1f, 1f), text,
            HorizontalAlignment.Left, -1f, (int)size, new Color(0f, 0f, 0f, 0.85f));
        canvas.DrawString(font, p, text, HorizontalAlignment.Left, -1f, (int)size, colour);
    }
}
