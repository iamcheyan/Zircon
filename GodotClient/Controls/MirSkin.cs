using System.Collections.Generic;
using System.IO;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Controls;

/// <summary>
/// 皮肤基础设施: .Zl 图库缓存 + 贴图缓存 + 中文字体。
/// 控件库统一从这里取图, 避免每个控件自己开文件流。
/// </summary>
public static class MirSkin
{
    /// <summary>客户端数据目录 (与 MapView 保持一致, 后续统一成配置)</summary>
    public static string DataPath = "/home/tetsuya/development/Zircon/Debug/Client/Data/";

    private static readonly Dictionary<LibraryFile, ZlLibrary> _libraries = new();
    private static readonly Dictionary<(LibraryFile, int), Texture2D> _textures = new();

    private static FontFile _font;
    private static bool _fontFailed;

    public static ZlLibrary GetLibrary(LibraryFile file)
    {
        if (_libraries.TryGetValue(file, out var lib)) return lib;
        if (!Libraries.LibraryList.TryGetValue(file, out string path)) return null;

        // LibraryList 路径是 Windows 格式 "Data\xxx.Zl": 先转正斜杠再剥 Data/ 前缀
        string p = path.Replace('\\', '/');
        if (p.StartsWith("Data/")) p = p.Substring(5);
        string full = Path.Combine(DataPath, p);
        if (!File.Exists(full)) return null;

        lib = new ZlLibrary(full);
        _libraries[file] = lib;
        return lib;
    }

    /// <summary>取图库第 index 帧的 Texture2D; 缺图返回 null (控件应静默跳过)</summary>
    public static Texture2D GetTexture(LibraryFile file, int index)
    {
        if (index < 0) return null;
        var key = (file, index);
        if (_textures.TryGetValue(key, out var tex) && tex != null) return tex;

        var lib = GetLibrary(file);
        if (lib == null || index >= lib.Images.Length) return null;

        tex = lib.GetImageTexture(index);
        if (tex == null) return null;
        _textures[key] = tex;
        return tex;
    }

    public static Vector2I GetSize(LibraryFile file, int index)
    {
        if (index < 0) return Vector2I.Zero;
        var lib = GetLibrary(file);
        if (lib == null || index >= lib.Images.Length || lib.Images[index] == null) return Vector2I.Zero;
        return new Vector2I(lib.Images[index].Width, lib.Images[index].Height);
    }

    public static Vector2I GetOffset(LibraryFile file, int index)
    {
        if (index < 0) return Vector2I.Zero;
        var lib = GetLibrary(file);
        if (lib == null || index >= lib.Images.Length || lib.Images[index] == null) return Vector2I.Zero;
        return new Vector2I(lib.Images[index].OffSetX, lib.Images[index].OffSetY);
    }

    /// <summary>中文字体 (Noto Sans CJK, 系统自带)。Godot 默认字体不含 CJK, UI 文字必须用它。</summary>
    public static FontFile GetFont()
    {
        if (_font != null) return _font;
        if (_fontFailed) return null;

        string[] candidates =
        {
            "/usr/share/fonts/google-noto-sans-cjk-vf-fonts/NotoSansCJK-VF.ttc",
            "/usr/share/fonts/noto-cjk/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
        };

        foreach (string path in candidates)
        {
            if (!File.Exists(path)) continue;
            var font = new FontFile();
            if (font.LoadDynamicFont(path) == Error.Ok)
            {
                _font = font;
                return font;
            }
            font.Dispose();
        }

        _fontFailed = true;
        GD.PrintErr("[MirSkin] 找不到中文字体 (Noto Sans CJK), UI 中文将无法显示");
        return null;
    }

    public static Vector2 MeasureText(string text, int fontSize)
    {
        var font = GetFont();
        if (font == null || string.IsNullOrEmpty(text)) return Vector2.Zero;
        return font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
    }
}
