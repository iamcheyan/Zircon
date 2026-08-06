using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Library;

namespace ZirconClient.Formats;

// .Zl 图库共享缓存: LibraryFile -> ZlLibrary
// 路径解析复用 Libraries.LibraryList (Data\xxx.Zl -> 去掉 Data/ 前缀 -> _dataPath 拼接)
public static class LibraryCache
{
    private static readonly Dictionary<LibraryFile, ZlLibrary> _cache = new();

    public static string DataPath { get; private set; }

    public static void Init()
    {
        if (DataPath != null) return;
        string projectDir = ProjectSettings.GlobalizePath("res://");
        DataPath = Path.GetFullPath(Path.Combine(projectDir, "..", "Debug", "Client", "Data"))
                   + Path.DirectorySeparatorChar;
    }

    public static ZlLibrary Get(LibraryFile file)
    {
        Init();
        if (file == LibraryFile.None) return null;
        if (_cache.TryGetValue(file, out var lib)) return lib;
        if (!Libraries.LibraryList.TryGetValue(file, out string path)) return null;
        // LibraryList 路径是 Windows 格式 "Data\xxx.Zl": 去掉 Data\ 前缀, 转正斜杠
        path = path.Replace('\\', '/');
        if (path.StartsWith("Data/")) path = path.Substring(5);
        string fullPath = Path.Combine(DataPath, path);
        if (!File.Exists(fullPath)) return null;

        lib = new ZlLibrary(fullPath);
        _cache[file] = lib;
        return lib;
    }

    public static void DisposeAll()
    {
        foreach (var lib in _cache.Values)
            lib?.Dispose();
        _cache.Clear();
    }
}
