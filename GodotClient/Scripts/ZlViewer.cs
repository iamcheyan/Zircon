using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 图片查看器: 浏览 Data 目录下的 .Zl 图库, 查看每一帧图像与元数据。
/// 用法:
///   godot-mono --path GodotClient/ -- --zl-dir Debug/Client/Data    浏览整个目录
///   godot-mono --path GodotClient/ -- --view-zl <file.Zl>           只看单个文件
/// </summary>
public partial class ZlViewer : Control
{
    private ItemList _fileList;
    private GridContainer _grid;
    private Label _statusLabel;
    private TextureRect _bigView;
    private Label _metaLabel;

    private ZlLibrary _lib;
    private string _currentPath;

    public override void _Ready()
    {
        _fileList = GetNode<ItemList>("HSplit/FilePanel/FileList");
        _grid = GetNode<GridContainer>("HSplit/Right/GridScroll/Grid");
        _statusLabel = GetNode<Label>("HSplit/Right/StatusLabel");
        _bigView = GetNode<TextureRect>("HSplit/Right/BigScroll/BigView");
        _metaLabel = GetNode<Label>("HSplit/Right/MetaLabel");

        GetNode<Button>("HSplit/FilePanel/RefreshBtn").Pressed += RefreshFileList;
        _fileList.ItemSelected += OnFileSelected;

        var args = OS.GetCmdlineUserArgs();
        string singleFile = null;
        string dir = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--view-zl" && i + 1 < args.Length) singleFile = args[i + 1];
            if (args[i] == "--zl-dir" && i + 1 < args.Length) dir = args[i + 1];
        }

        if (!string.IsNullOrEmpty(singleFile))
        {
            if (File.Exists(singleFile))
            {
                _fileList.AddItem(Path.GetFileName(singleFile));
                _fileList.SetItemMetadata(0, Path.GetFullPath(singleFile));
                _fileList.Select(0);
                LoadLibrary(Path.GetFullPath(singleFile));
            }
            else
            {
                _statusLabel.Text = string.Format(Lang.ZlViewerUi605Label, singleFile);
            }
            return;
        }

        // 默认目录: 参数 > ../Debug/Client/Data > ./Debug/Client/Data
        if (string.IsNullOrEmpty(dir))
        {
            foreach (var candidate in new[] { "../Debug/Client/Data", "Debug/Client/Data" })
            {
                if (Directory.Exists(candidate))
                {
                    dir = candidate;
                    break;
                }
            }
        }
        RefreshFileList(dir);

        // headless 自检: 解码每个文件前若干帧, 打印统计后退出
        if (DisplayServer.GetName() == "headless")
            CallDeferred(MethodName.HeadlessCheck);
    }

    private void HeadlessCheck()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            // 目录模式: 遍历所有文件
            var dir = FindDataDir();
            if (dir == null)
            {
                GD.Print("[ZlViewer] 未找到 Data 目录");
                GetTree().Quit(1);
                return;
            }
            foreach (var file in Directory.GetFiles(dir, "*.Zl").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                ScanFile(file);
        }
        else
        {
            ScanFile(_currentPath);
        }
        GetTree().Quit(0);
    }

    private void ScanFile(string path)
    {
        try
        {
            using var lib = new ZlLibrary(path);
            string name = Path.GetFileName(path);
            if (lib.Images.Length == 0)
            {
                GD.Print($"[ZlViewer] {name}: 无法读取 (ZL2 或损坏)");
                return;
            }
            int decoded = 0, failed = 0;
            for (int i = 0; i < lib.Images.Length; i++)
            {
                var img = lib.Images[i];
                if (img == null || img.Width <= 0 || img.Height <= 0) continue;
                decoded++;
                // 抽样解码: 每文件最多 3 帧
                if (decoded <= 3)
                {
                    try { lib.GetImageData(i); }
                    catch { failed++; }
                }
            }
            GD.Print($"[ZlViewer] {name}: {lib.Images.Length} 帧, version {lib.Version}, 非空 {decoded}, 抽样解码失败 {failed}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ZlViewer] {Path.GetFileName(path)}: 打开失败 {ex.Message}");
        }
    }

    private void RefreshFileList()
    {
        RefreshFileList(FindDataDir());
    }

    private string FindDataDir()
    {
        foreach (var candidate in new[] { "../Debug/Client/Data", "Debug/Client/Data" })
            if (Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
        return null;
    }

    private void RefreshFileList(string dir)
    {
        _fileList.Clear();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            _statusLabel.Text = Lang.ZlViewerUi606Label;
            return;
        }

        var files = Directory.GetFiles(dir, "*.Zl")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var file in files)
        {
            _fileList.AddItem(Path.GetFileName(file));
            _fileList.SetItemMetadata(_fileList.ItemCount - 1, file);
        }
        _statusLabel.Text = string.Format(Lang.ZlViewerUi607Label, files.Length, Path.GetDirectoryName(dir));
    }

    private void OnFileSelected(long index)
    {
        string path = (string)_fileList.GetItemMetadata((int)index);
        LoadLibrary(path);
    }

    private void LoadLibrary(string path)
    {
        try
        {
            _lib?.Dispose();
            _lib = new ZlLibrary(path);
            _currentPath = path;
            _bigView.Texture = null;
            _metaLabel.Text = "";

            int count = _lib.Images.Length;
            int shown = count;
            if (_lib.Images.Length == 0)
            {
                // ZL2 或解析失败
                _statusLabel.Text = string.Format(Lang.ZlViewerNoneLabel, Path.GetFileName(path));
            }
            else
            {
                _statusLabel.Text = string.Format(Lang.ZlViewerUi609Label, Path.GetFileName(path), count, _lib.Version);
            }
            BuildGrid();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = string.Format(Lang.ZlViewerUi610Label, Path.GetFileName(path), ex.Message);
        }
    }

    private void BuildGrid()
    {
        foreach (Node child in _grid.GetChildren())
            child.QueueFree();

        if (_lib == null) return;

        var images = _lib.Images;
        for (int i = 0; i < images.Length; i++)
        {
            var meta = images[i];
            if (meta == null || meta.Width <= 0 || meta.Height <= 0) continue;

            int idx = i;
            try
            {
                var tex = _lib.GetImageTexture(idx);
                if (tex == null) continue;

                var thumb = new TextureButton
                {
                    TextureNormal = tex,
                    CustomMinimumSize = new Vector2(64, 64),
                    StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                    TooltipText = $"{idx}: {meta.Width}x{meta.Height}",
                };
                thumb.Pressed += () => ShowFrame(idx);
                _grid.AddChild(thumb);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ZlViewer] 帧 {idx} 解码失败: {ex.Message}");
            }
        }
    }

    private void ShowFrame(int index)
    {
        var meta = _lib?.Images[index];
        if (meta == null) return;

        try
        {
            var tex = _lib.GetImageTexture(index);
            _bigView.Texture = tex;
            _metaLabel.Text =
                $"index={index}  {meta.Width}x{meta.Height}  " +
                $"offset=({meta.OffSetX},{meta.OffSetY})  codec={meta.ImageCodec}  " +
                $"shadow={meta.ShadowType}";
        }
        catch (Exception ex)
        {
            _metaLabel.Text = string.Format(Lang.ZlViewerUi611Label, ex.Message);
        }
    }

    public override void _ExitTree()
    {
        _lib?.Dispose();
        _lib = null;
    }
}
