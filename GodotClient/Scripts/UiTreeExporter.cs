using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Godot;
using Library;

using ZirconClient.Controls;

/// <summary>
/// UI 树导出器（--ui-export）：反射枚举全部 DXWindow 子类，无头实例化并强制执行
/// 布局代码（构造器布局 + _Ready 补建的关闭按钮/标题），深度遍历 DXControl.Controls
/// 导出为 GodotClient/UI/ui_tree.json，供 Web 编辑器（Mir3-Research/Tools/uieditor）渲染。
///
/// 运行：
///   godot-mono --path GodotClient res://Scenes/UITestScene.tscn -- --ui-export
///
/// 铁律：所有坐标按逻辑画布 1024x768 导出（UiScaler 缩放前的值）。
/// 控件 location 为相对父控件的逻辑坐标（与游戏内 DXControl.Location 同一基准），
/// web 端叠加 overlay 后可直接回写同坐标系。
///
/// 图片帧不逐个导 PNG —— 只导 manifest（图库 → 帧号集合），web 端经
/// /zl/{lib}/{frame}.png 实时解码（dbeditor 同款 zlsdk 端点）。
/// </summary>
public static class UiTreeExporter
{
    public static bool ExportRequested => OS.GetCmdlineUserArgs().Contains("--ui-export");

    public static async System.Threading.Tasks.Task Run(Node host)
    {
        var errors = new List<string>();
        try
        {
            GD.Print("[UiExport] 开始导出 UI 树…");

            // 1. 反射枚举 DXWindow 子类（非抽象、无参构造、非嵌套类——嵌套的是测试脚手架）
            var assembly = typeof(DXWindow).Assembly;
            var windowTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && typeof(DXWindow).IsAssignableFrom(t)
                            && t.DeclaringType == null
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var skipped = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && typeof(DXWindow).IsAssignableFrom(t)
                            && t.DeclaringType == null
                            && t.GetConstructor(Type.EmptyTypes) == null)
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
            if (skipped.Count > 0)
                GD.Print($"[UiExport] 跳过 {skipped.Count} 个无参构造缺失的窗口: {string.Join(", ", skipped)}");

            // 2. 无头实例化：统一挂到一个独立 CanvasLayer（恒等变换——导出的是逻辑坐标），
            //    AddChild 同步触发整棵 _Ready 链（关闭按钮/标题/滚动条布局全部就位）。
            var layer = new CanvasLayer { Layer = 50 };
            host.AddChild(layer);

            var created = new List<DXWindow>();
            foreach (Type type in windowTypes)
            {
                try
                {
                    var window = (DXWindow)Activator.CreateInstance(type);
                    layer.AddChild(window);
                    created.Add(window);
                }
                catch (Exception ex)
                {
                    errors.Add($"{type.Name}: {ex.GetBaseException().Message}");
                }
            }

            // 3. 等 2 帧：让 _Ready 里可能的 CallDeferred 布局（滚动条/自动尺寸）落地
            await host.GetTree().ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            await host.GetTree().ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);

            // 4. 遍历导出
            var manifest = new SortedDictionary<string, SortedSet<int>>();
            var windows = new List<Dictionary<string, object>>();
            int totalControls = 0;
            int maxDepthOverall = 0;
            var minAbs = new Vector2I(int.MaxValue, int.MaxValue);
            var maxAbs = new Vector2I(int.MinValue, int.MinValue);
            int negativeCoordCount = 0;

            foreach (DXWindow window in created)
            {
                string className = window.GetType().Name;
                try
                {
                    int windowControls = 0, windowDepth = 0;
                    var root = ExportNode(window, className, Vector2I.Zero, manifest,
                        ref windowControls, ref windowDepth, 0,
                        ref minAbs, ref maxAbs, ref negativeCoordCount);
                    totalControls += windowControls;
                    maxDepthOverall = Math.Max(maxDepthOverall, windowDepth);

                    windows.Add(new Dictionary<string, object>
                    {
                        ["className"] = className,
                        ["title"] = window.Text ?? "",
                        ["type"] = window.GetType().Name,
                        ["location"] = Vec(window.Position),
                        ["size"] = Vec(window.Size),
                        ["hasTitle"] = window.HasTitle,
                        ["hasFooter"] = window.HasFooter,
                        ["movable"] = window.Movable,
                        ["allowResize"] = window.AllowResize,
                        ["controlCount"] = windowControls + 1, // 含根
                        ["maxDepth"] = windowDepth + 1,
                        ["controls"] = (List<object>)root["children"],
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{className}: 遍历失败 {ex.GetBaseException().Message}");
                }
            }

            // 5. 组装 + 写盘
            var manifestOut = new Dictionary<string, List<int>>();
            foreach (var (lib, frames) in manifest)
                manifestOut[lib] = frames.ToList();

            var tree = new Dictionary<string, object>
            {
                ["generator"] = "zircon-ui-export",
                ["version"] = 1,
                ["exportedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["logicalCanvas"] = new[] { 1024, 768 },
                ["windowCount"] = windows.Count,
                ["controlCount"] = totalControls + windows.Count,
                ["libManifest"] = manifestOut,
                ["windows"] = windows,
            };

            string outDir = ProjectSettings.GlobalizePath("res://UI");
            string outPath = Path.Combine(outDir, "ui_tree.json");
            Directory.CreateDirectory(outDir);
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 保留中文标题
            };
            File.WriteAllText(outPath, JsonSerializer.Serialize(tree, options));
            long sizeKb = new FileInfo(outPath).Length / 1024;

            // 6. 验证摘要（验收标准 1：≥40 窗口；InventoryDialog 深度 ≥3；坐标在画布内）
            var inventory = windows.FirstOrDefault(w => (string)w["className"] == "InventoryDialog");
            GD.Print($"[UiExport] 完成: {windows.Count} 窗口 / {totalControls + windows.Count} 控件 / " +
                     $"最大深度 {maxDepthOverall} / {manifestOut.Count} 图库 / {sizeKb} KB → {outPath}");
            GD.Print($"[UiExport] 坐标域: absX [{minAbs.X}, {maxAbs.X}] absY [{minAbs.Y}, {maxAbs.Y}]，" +
                     $"负坐标控件 {negativeCoordCount} 个");
            if (inventory != null)
                GD.Print($"[UiExport] InventoryDialog: 控件 {inventory["controlCount"]} 个，深度 {inventory["maxDepth"]}");
            if (errors.Count > 0)
            {
                GD.PushWarning($"[UiExport] {errors.Count} 个窗口实例化/遍历失败:");
                foreach (string error in errors) GD.PushWarning($"[UiExport]   {error}");
            }
            GD.Print("[UiExport] PASS 请核对上方验收数字（≥40 窗口 / Inventory 深度≥3 / 坐标域合理）");
        }
        catch (Exception ex)
        {
            GD.PushError($"[UiExport] 导出失败: {ex}");
        }
        finally
        {
            host.GetTree().Quit();
        }
    }

    private static Dictionary<string, object> ExportNode(
        DXControl control, string path, Vector2I absOrigin,
        SortedDictionary<string, SortedSet<int>> manifest,
        ref int count, ref int maxDepth, int depth,
        ref Vector2I minAbs, ref Vector2I maxAbs, ref int negativeCoords)
    {
        count++;
        maxDepth = Math.Max(maxDepth, depth);

        Vector2I location = new(
            (int)Math.Round(control.Position.X),
            (int)Math.Round(control.Position.Y));
        Vector2I size = new(
            (int)Math.Round(control.Size.X),
            (int)Math.Round(control.Size.Y));
        Vector2I abs = new(absOrigin.X + location.X, absOrigin.Y + location.Y);
        if (location.X < 0 || location.Y < 0) negativeCoords++;
        minAbs = new Vector2I(Math.Min(minAbs.X, abs.X), Math.Min(minAbs.Y, abs.Y));
        maxAbs = new Vector2I(Math.Max(maxAbs.X, abs.X + size.X), Math.Max(maxAbs.Y, abs.Y + size.Y));

        var node = new Dictionary<string, object>
        {
            ["path"] = path,
            ["type"] = control.GetType().Name,
            ["location"] = new[] { location.X, location.Y },
            ["size"] = new[] { size.X, size.Y },
            ["absLocation"] = new[] { abs.X, abs.Y },
            ["visible"] = control.Visible,
            ["foreColour"] = ColourBytes(control.ForeColour),
            ["backColour"] = ColourBytes(control.BackColour),
        };

        // Godot 自动节点名（@DXLabel@3）没有辨识价值，只有显式命名才导出
        string name = control.Name;
        if (!string.IsNullOrEmpty(name) && !name.StartsWith("@"))
            node["name"] = name;
        if (!string.IsNullOrEmpty(control.Text))
            node["text"] = control.Text;
        string hint = control.TooltipText;
        if (!string.IsNullOrEmpty(hint))
            node["hint"] = hint;
        if (control.Opacity < 1f)
            node["opacity"] = Math.Round(control.Opacity, 3);

        switch (control)
        {
            case DXLabel label:
                node["fontSize"] = label.FontSize;
                node["textColour"] = ColourBytes(label.TextColour);
                node["autoSize"] = label.AutoSize;
                node["align"] = label.Align.ToString();
                node["valign"] = label.VAlign.ToString();
                node["drawOutline"] = label.DrawOutline;
                break;
            case DXButton button:
                node["fontSize"] = button.FontSize;
                node["textColour"] = ColourBytes(button.TextColour);
                break;
        }

        if (control is DXImageControl image && image.Index >= 0)
        {
            string lib = LibraryName(image.LibraryFile);
            node["image"] = new Dictionary<string, object>
            {
                ["library"] = lib,
                ["index"] = image.Index,
                ["hoverIndex"] = image.HoverIndex,
                ["pressedIndex"] = image.PressedIndex,
                ["fixedSize"] = image.FixedSize,
            };
            if (lib != null)
            {
                if (!manifest.TryGetValue(lib, out var frames))
                    manifest[lib] = frames = new SortedSet<int>();
                frames.Add(image.Index);
                if (image.HoverIndex >= 0) frames.Add(image.HoverIndex);
            }
        }

        var children = new List<object>();
        for (int i = 0; i < control.Controls.Count; i++)
            children.Add(ExportNode(control.Controls[i], $"{path}/{i}", abs, manifest,
                ref count, ref maxDepth, depth + 1, ref minAbs, ref maxAbs, ref negativeCoords));
        node["children"] = children;
        return node;
    }

    /// <summary>LibraryFile 枚举 → .Zl 文件基名（与 web 端 /zl/{lib}/{frame}.png 端点对齐）。</summary>
    private static string LibraryName(LibraryFile file)
    {
        if (Libraries.LibraryList.TryGetValue(file, out string path))
            return Path.GetFileNameWithoutExtension(path.Replace('\\', '/'));
        return file.ToString();
    }

    private static int[] Vec(Vector2 v) => new[] { (int)Math.Round(v.X), (int)Math.Round(v.Y) };
    private static int[] ColourBytes(Color c) => new[] { (int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255), (int)(c.A * 255) };
}
