using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// UI overlay 加载器：读取 UI/ui_overlay.json（Web 编辑器 :8820 产出的视觉属性 diff），
/// 在每个 DXWindow 布局完成后按 path（子索引链，如 "InventoryDialog/1"）应用覆盖。
///
/// 格式：
///   { "InventoryDialog": { "InventoryDialog/1": { "location": [52, 8], "fontSize": 10 } } }
///
/// 铁律：
///   - 只覆盖视觉属性（location/size/text/fontSize/visible/foreColour/backColour/textColour），
///     永不改逻辑/事件绑定 —— 与 Web 编辑器属性面板开放同一集合。
///   - 坐标 = 逻辑画布 1024x768 基准（UiScaler 缩放前的值），与窗口内相对坐标一致。
///   - path 不存在 → 告警日志跳过（不崩）；类型不匹配的属性 → 忽略。
///   - overlay 文件缺失/为空 → 零副作用。
///
/// 热重载：游戏内 F12（GameScene._Input 顶部拦截）触发 ReloadAll()，
/// 浏览器编辑器「同步」后按一下 F12 即可零重启生效。
/// </summary>
public static class UiOverlay
{
    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>> Empty = new();

    private static Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>> _byWindow = Empty;
    private static JsonDocument _document;

    /// <summary>是否存在可应用的覆盖（false 时所有 Apply 调用零开销）。</summary>
    public static bool HasOverrides => _byWindow.Count > 0;

    /// <summary>上次 ReloadAll/ApplyAll 实际应用的属性条数（日志/验收用）。</summary>
    public static int LastAppliedCount { get; private set; }

    public static string OverlayPath => ProjectSettings.GlobalizePath("res://UI/ui_overlay.json");

    /// <summary>读取 overlay 文件。文件缺失或解析失败 → 保持空表（游戏零变化）。</summary>
    public static void Load()
    {
        _document?.Dispose();
        _document = null;
        _byWindow = Empty;
        try
        {
            string path = OverlayPath;
            if (!File.Exists(path))
            {
                GD.Print($"[UiOverlay] 无 overlay 文件（{path}），跳过");
                return;
            }

            _document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            var result = new Dictionary<string, Dictionary<string, Dictionary<string, JsonElement>>>();
            foreach (JsonProperty windowProp in _document.RootElement.EnumerateObject())
            {
                if (windowProp.Value.ValueKind != JsonValueKind.Object) continue;
                var paths = new Dictionary<string, Dictionary<string, JsonElement>>();
                foreach (JsonProperty pathProp in windowProp.Value.EnumerateObject())
                {
                    if (pathProp.Value.ValueKind != JsonValueKind.Object) continue;
                    var props = new Dictionary<string, JsonElement>();
                    foreach (JsonProperty prop in pathProp.Value.EnumerateObject())
                        props[prop.Name] = prop.Value.Clone(); // Clone 脱离文档生命周期
                    if (props.Count > 0)
                        paths[pathProp.Name] = props;
                }
                if (paths.Count > 0)
                    result[windowProp.Name] = paths;
            }

            _byWindow = result;
            int controls = 0;
            foreach (var paths in result.Values) controls += paths.Count;
            GD.Print($"[UiOverlay] 已加载 {path}: {result.Count} 窗口 / {controls} 个控件覆盖");
        }
        catch (Exception ex)
        {
            _byWindow = Empty;
            GD.PushWarning($"[UiOverlay] 读取失败，忽略 overlay: {ex.Message}");
        }
    }

    /// <summary>重读文件并刷新全部已创建窗口（F12 热重载入口）。</summary>
    public static void ReloadAll()
    {
        Load();
        ApplyAll();
    }

    /// <summary>对 DXWindow.Windows 里所有存活窗口应用 overlay（GameScene 布局完成后调用）。</summary>
    public static void ApplyAll()
    {
        LastAppliedCount = 0;
        if (!HasOverrides) return;

        foreach (DXWindow window in DXWindow.Windows.ToArray())
        {
            if (window is not GodotObject g || !GodotObject.IsInstanceValid(g)) continue;
            ApplyWindow(window);
        }
        GD.Print($"[UiOverlay] ApplyAll: 应用了 {LastAppliedCount} 条覆盖");
    }

    /// <summary>对单个窗口按类名查表应用覆盖（DXWindow._Ready deferred hook）。</summary>
    public static void ApplyWindow(DXWindow window)
    {
        if (window == null || !HasOverrides) return;
        if (!_byWindow.TryGetValue(window.GetType().Name, out var paths)) return;

        foreach (var (path, props) in paths)
        {
            DXControl target = ResolveByPath(window, path);
            if (target == null)
            {
                GD.PushWarning($"[UiOverlay] 路径不存在: {window.GetType().Name}#{path}，跳过");
                continue;
            }
            ApplyProps(target, props);
        }
    }

    /// <summary>path 形如 "WindowClass/0/3/1"：首段是窗口类名（可省略），其余为 Controls 子索引链。</summary>
    private static DXControl ResolveByPath(DXWindow window, string path)
    {
        string[] segments = path.Split('/');
        DXControl current = window;
        foreach (string segment in segments)
        {
            if (!int.TryParse(segment, out int index)) continue; // 类名段跳过
            if (index < 0 || index >= current.Controls.Count) return null;
            current = current.Controls[index];
        }
        return current;
    }

    private static void ApplyProps(DXControl control, Dictionary<string, JsonElement> props)
    {
        foreach (var (name, value) in props)
        {
            try
            {
                switch (name)
                {
                    case "location":
                        control.Location = ReadVector2I(value);
                        LastAppliedCount++;
                        break;
                    case "size":
                        Vector2I size = ReadVector2I(value);
                        control.Size = new Vector2(size.X, size.Y);
                        LastAppliedCount++;
                        break;
                    case "text":
                        control.Text = value.GetString() ?? "";
                        LastAppliedCount++;
                        break;
                    case "visible":
                        control.Visible = value.ValueKind == JsonValueKind.True;
                        LastAppliedCount++;
                        break;
                    case "fontSize":
                        if (control is DXLabel label)
                        {
                            label.FontSize = value.GetInt32();
                            LastAppliedCount++;
                        }
                        else if (control is DXButton button)
                        {
                            button.FontSize = value.GetInt32();
                            LastAppliedCount++;
                        }
                        // 其余类型无 FontSize —— 类型不匹配的属性忽略
                        break;
                    case "foreColour":
                        control.ForeColour = ReadColor(value);
                        LastAppliedCount++;
                        break;
                    case "backColour":
                        control.BackColour = ReadColor(value);
                        LastAppliedCount++;
                        break;
                    case "textColour":
                        if (control is DXLabel textLabel)
                        {
                            textLabel.TextColour = ReadColor(value);
                            LastAppliedCount++;
                        }
                        else if (control is DXButton textButton)
                        {
                            textButton.TextColour = ReadColor(value);
                            LastAppliedCount++;
                        }
                        break;
                    default:
                        GD.PushWarning($"[UiOverlay] 未知属性 {name}（{control.GetType().Name}），忽略");
                        break;
                }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[UiOverlay] 应用属性失败 {name}={value}: {ex.Message}，跳过");
            }
        }
    }

    private static Vector2I ReadVector2I(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) throw new FormatException("期望 [x,y] 数组");
        int x = (int)Math.Round(value[0].GetDouble());
        int y = (int)Math.Round(value[1].GetDouble());
        return new Vector2I(x, y);
    }

    private static Color ReadColor(JsonElement value)
    {
        // Web 端导出 [r,g,b,a] 0-255 整数（与 ui_tree.json 同一约定）
        if (value.ValueKind == JsonValueKind.Array)
        {
            float r = value[0].GetSingle() / 255f;
            float g = value[1].GetSingle() / 255f;
            float b = value[2].GetSingle() / 255f;
            float a = value.GetArrayLength() > 3 ? value[3].GetSingle() / 255f : 1f;
            return new Color(r, g, b, a);
        }
        if (value.ValueKind == JsonValueKind.String)
            return new Color(value.GetString());
        throw new FormatException("颜色期望数组或 #RRGGBB 字符串");
    }
}
