using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>旧版 DXItemAmountWindow：拆分堆叠物品时选择实际数量。</summary>
public sealed partial class ItemAmountDialog : DXWindow
{
    private readonly DXTextInput _amount;
    public long Amount { get; private set; }
    private readonly long _max;
    private readonly ClientUserItem _item;
    private readonly Action<long> _confirm;

    public ItemAmountDialog(ClientUserItem item, Action<long> confirm)
        // 原版 DXItemAmountWindow 在打开时统一从 1 开始，而不是默认半堆。
        : this(item?.Info?.ItemName ?? "物品", Math.Max(1, item?.Count ?? 1), 1, confirm, item)
    {
    }

    public ItemAmountDialog(string title, long max, long initial, Action<long> confirm)
        : this(title, max, initial, confirm, null)
    {
    }

    private ItemAmountDialog(string title, long max, long initial, Action<long> confirm, ClientUserItem item)
    {
        Text = "选择数量";
        HasFooter = true;
        Size = new Vector2I(260, 135);
        _confirm = confirm;
        // 原版 DXItemAmountWindow 的客户区为 200x46；这里保留物品格和数量
        // 控件的原始相对关系，窗口总尺寸按标题/底栏框架计算。
        Size = new Vector2I(218, 146);
        _max = Math.Max(1, max);
        Amount = Math.Clamp(initial, 1, _max);
        _item = item;
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        var itemCell = new DXItemCell { Location = new Vector2I(18, 38), ReadOnly = true, Border = true };
        if (item != null)
        {
            itemCell.ItemGrid = new[] { item };
            itemCell.Slot = 0;
        }
        AddControl(itemCell);
        _amount = new DXTextInput { Text = Amount.ToString(), Location = new Vector2I(64, 43), Size = new Vector2I(78, 22), MaxLength = 12 };
        _amount.TextChanged += value => UpdateAmount(value);
        AddControl(_amount);
        // 原版 DXNumberBox.Change = Max(1, Count/5)：上下按钮按总量的
        // 五分之一步进，而不是固定 ±1。
        long step = ComputeStep(_max);
        var up = new DXButton { Text = "▲", FontSize = 8, Type = DXButton.ButtonType.SmallButton, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(144, 42), Size = new Vector2I(20, 12) };
        up.MouseClick += (s, e) => UpdateAmount((Amount + step).ToString());
        AddControl(up);
        var down = new DXButton { Text = "▼", FontSize = 8, Type = DXButton.ButtonType.SmallButton, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(144, 55), Size = new Vector2I(20, 12) };
        down.MouseClick += (s, e) => UpdateAmount((Amount - step).ToString());
        AddControl(down);
        var ok = new DXButton { Text = "确定", Type = DXButton.ButtonType.Default, Location = new Vector2I(128, 103), Size = new Vector2I(80, 25), LibraryFile = LibraryFile.Interface, Index = -1 };
        ok.MouseClick += (s, e) => { _confirm?.Invoke(Amount); WindowManager.Close(this); };
        AddControl(ok);
        // 原版 AmountBox_KeyPress：Enter 确认、Escape 关闭。
        _amount.TextSubmitted += _ => { _confirm?.Invoke(Amount); WindowManager.Close(this); };
        _amount.Canceled += () => WindowManager.Close(this);
        _amount.GrabFocus();
    }

    private void UpdateAmount(string value)
    {
        if (!long.TryParse(value, out long amount)) return;
        Amount = Math.Clamp(amount, 1, _max);
        if (_amount.Text != Amount.ToString()) _amount.Text = Amount.ToString();
        // 原版 DXItemAmountWindow.ValueChanged 边框反馈：<=0 红、等于上限橙、其余绿。
        _amount.BorderColour = BorderColourFor(Amount, _max);
        if (_item != null && _item.Info?.ItemName == "金币") _item.Count = Amount;
    }

    /// <summary>原版 DXNumberBox.Change = Max(1, Count/5) 的步进值。</summary>
    public static long ComputeStep(long max) => Math.Max(1, max / 5);

    /// <summary>原版 DXItemAmountWindow 边框反馈颜色：<=0 红、等于上限橙、其余绿。</summary>
    public static Color BorderColourFor(long amount, long max) => amount <= 0
        ? new Color(1f, .25f, .25f)
        : amount == max
            ? new Color(1f, .65f, .1f)
            : new Color(.3f, .9f, .35f);
}
