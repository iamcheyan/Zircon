using System;
using Godot;
using Library;
using ZirconClient.Controls;

namespace ZirconClient.Scripts;

/// <summary>
/// UI 控件库冒烟测试: 摆出窗口/按钮/标签/贴图, 截图验证。
/// 运行: godot-mono --path GodotClient/ res://Scenes/UITestScene.tscn
/// 不会自动退出: 按 Esc 或关闭窗口退出。
/// </summary>
public partial class UITestScene : Control
{
    public override void _Ready()
    {
        GD.Print($"[UITest] viewport={GetViewport().GetVisibleRect().Size}");

        // 深色背景模拟游戏画面
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.07f, 0.05f),
            Size = GetViewport().GetVisibleRect().Size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(bg);

        // 1. 一个窗口: 背景贴图 + 标题 + 关闭按钮
        var win = new TestWindow
        {
            Position = new Vector2(160, 120),
            Size = new Vector2(420, 300),
            Text = "背包 (测试窗口)",
        };
        win.ShowWindow(this);

        // 窗口背景 = 一张 Interface 贴图 (旧客户端窗口都用这个模式)
        win.AddControl(new DXImageControl
        {
            Index = 164,
            LibraryFile = LibraryFile.Interface,
            UseOffSet = false,
            FixedSize = true,
            Size = new Vector2(420, 300),
            MouseFilter = MouseFilterEnum.Ignore,
        });

        // 窗口里的标签
        win.AddControl(new DXLabel
        {
            Text = "这是中文标签: 攻击 12-25 · 防御 5",
            FontSize = 14,
            Location = new Vector2I(30, 40),
            TextColour = new Color(0.9f, 0.85f, 0.7f),
        });

        // 2. 按钮区 (窗口下方, 直接放根节点)
        var btn1 = new DXButton
        {
            Text = "普通按钮",
            Index = 210,
            HoverIndex = 211,
            PressedIndex = 212,
            LibraryFile = LibraryFile.Interface,
            Position = new Vector2(160, 470),
            FixedSize = true,
        };
        AddChild(btn1);

        var btn2 = new DXButton
        {
            Text = "红方块按钮(无贴图兜底)",
            Position = new Vector2(160, 530),
            Size = new Vector2(160, 36),
            FixedSize = true,
        };
        AddChild(btn2);

        btn2.MouseClick += (o, e) => GD.Print("[UITest] 点击了测试按钮");

        // 3. 直接贴图测试 (Interface 图库若干帧)
        for (int i = 0; i < 6; i++)
        {
            win.AddControl(new DXImageControl
            {
                Index = 200 + i,
                LibraryFile = LibraryFile.Interface,
                UseOffSet = false,
                FixedSize = true,
                Location = new Vector2I(30 + i * 40, 220),
            });
        }

        // 自检: 打印关键信息
        SelfCheck(win, btn1, btn2);

        // 等 10 帧后截图一次 (供我分析), 然后挂起等用户按键
        ScreenshotThenWait();
    }

    private void SelfCheck(DXWindow win, DXButton btn1, DXButton btn2)
    {
        GD.Print($"[UITest] 窗口可见={win.Visible} 位置={win.Position} 尺寸={win.Size}");
        GD.Print($"[UITest] 背景贴图164={MirSkin.GetTexture(LibraryFile.Interface, 164) != null} 尺寸={MirSkin.GetSize(LibraryFile.Interface, 164)}");
        GD.Print($"[UITest] 按钮1贴图210={MirSkin.GetTexture(LibraryFile.Interface, 210) != null} 尺寸={MirSkin.GetSize(LibraryFile.Interface, 210)}");
        GD.Print($"[UITest] 按钮2尺寸={btn2.Size} 中文字体={MirSkin.GetFont() != null}");
        GD.Print($"[UITest] 字体尺寸测试='攻击'={MirSkin.MeasureText("攻击", 14)}");
    }

    private async void ScreenshotThenWait()
    {
        for (int i = 0; i < 10; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var img = GetViewport().GetTexture().GetImage();
        img.SavePng("/tmp/ui_test.png");
        GD.Print($"[UITest] 截图已保存 /tmp/ui_test.png ({(int)img.GetWidth()}x{(int)img.GetHeight()})");
        GD.Print("[UITest] 画面已就绪, 按 Esc 或关闭窗口退出");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            GetTree().Quit();
    }

    private partial class TestWindow : DXWindow { }
}
