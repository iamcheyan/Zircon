using Godot;

namespace ZirconClient.Scripts;

/// <summary>
/// 登录/选人场景的 UI 缩放（与 GameScene 的 UiScale 逻辑一致）。
///
/// GameScene 的 HUD 挂在 CanvasLayer 上并应用 UiScale Transform（逻辑画布
/// 1024x768 放大到窗口）。但 LoginScene/SelectScene 是独立场景，没有这套
/// 逻辑——窗口放大（2 倍）时它们仍按 1024x768 布局，UI 不跟随缩放。
///
/// 用法：场景 _Ready 时调用 Attach()，把 UI 根节点挂到缩放层下。
/// </summary>
public static class UiScaler
{
    public const float BaseHeight = 768f;
    public const float BaseWidth = 1024f;

    /// <summary>按视口计算 UI 缩放倍率（1..2，与 GameScene.RefreshUiScale 一致）。</summary>
    public static float ComputeScale(Viewport viewport)
    {
        if (viewport == null) return 2f;
        Vector2 size = viewport.GetVisibleRect().Size;
        if (size.X <= 0 || size.Y <= 0) return 2f;
        float byHeight = size.Y / BaseHeight;
        float byWidth = size.X / BaseWidth;
        return Mathf.Clamp(Mathf.Min(byHeight, byWidth), 1f, 2f);
    }
    /// <summary>
    /// 创建一个缩放层（CanvasLayer），返回层节点。UI 根节点应 AddChild 到这个层。
    /// 之后调用 UpdateScale() 同步倍率。
    /// </summary>
    public static CanvasLayer CreateLayer(Control uiRoot, Node parent)
    {
        var layer = new CanvasLayer();
        parent.AddChild(layer);
        if (uiRoot != null)
        {
            uiRoot.GetParent()?.RemoveChild(uiRoot);
            layer.AddChild(uiRoot);
        }
        return layer;
    }

    /// <summary>把缩放层 Transform 更新为当前视口倍率。</summary>
    public static void UpdateScale(CanvasLayer layer, Viewport viewport)
    {
        if (layer == null || !GodotObject.IsInstanceValid(layer)) return;
        float scale = ComputeScale(viewport);
        GD.Print($"[UiScaler] scale={scale} viewport={viewport?.GetVisibleRect().Size ?? Vector2.Zero}");
        layer.Transform = Transform2D.Identity.Scaled(Vector2.One * scale);
    }
}
