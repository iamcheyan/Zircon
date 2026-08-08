using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版钱包窗口：按 CurrencyCategory 分组，组头可折叠，内容可滚动。</summary>
public sealed partial class CurrencyDialog : DXWindow
{
    private readonly CurrencyTree _tree;

    public CurrencyDialog()
    {
        Movable = true;
        Text = "Currency";
        // 原版 SetClientSize(227, 7 * 43 + 1) 经 Interface 通用窗口框架
        // 展开后为 245x348，CurrencyTree 位于客户区 (9,37)。
        Size = new Vector2I(245, 348);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(215, 3) };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);
        _tree = new CurrencyTree { Location = new Vector2I(9, 37), Size = new Vector2I(227, 302) };
        AddControl(_tree);
    }

    public bool AuditLayout(out string details)
    {
        details = $"size={Size} tree={_tree.Position}/{_tree.Size} scroll={_tree.ScrollGeometry}";
        return Size == new Vector2I(245, 348)
            && _tree.Position == new Vector2I(9, 37)
            && _tree.Size == new Vector2I(227, 302)
            && _tree.ScrollGeometry == new Vector2I(213, 302);
    }

    public void RefreshCurrencies(IEnumerable<ClientUserCurrency> currencies)
    {
        _tree.Rebuild(currencies);
    }
}

public sealed partial class CurrencyTree : DXControl
{
    private const int HeaderHeight = 22;
    private const int RowHeight = 42;
    private readonly DXVScrollBar _scroll;
    private readonly List<DXControl> _lines = new();
    private readonly Dictionary<CurrencyCategory, bool> _expanded = new();
    public Vector2I ScrollGeometry => new((int)_scroll.Position.X, (int)_scroll.Size.Y);

    public CurrencyTree()
    {
        Border = true;
        BorderColour = new Color(.55f, .4f, .18f);
        Clip = true;
        _scroll = new DXVScrollBar { Location = new Vector2I(213, 0), Size = new Vector2I(14, 302), VisibleSize = 302, Change = HeaderHeight };
        AddControl(_scroll);
        _scroll.ValueChanged += (s, e) => Relayout();
        MouseWheel += _scroll.DoMouseWheel;
    }

    public void Rebuild(IEnumerable<ClientUserCurrency> source)
    {
        foreach (var line in _lines)
        {
            RemoveControl(line);
            line.QueueFree();
        }
        _lines.Clear();

        var grouped = (source ?? Enumerable.Empty<ClientUserCurrency>())
            .Where(x => x?.Info != null)
            .GroupBy(x => x.Info.Category)
            .OrderBy(x => x.Key);

        foreach (var group in grouped)
        {
            if (!_expanded.ContainsKey(group.Key)) _expanded[group.Key] = true;
            var header = new CurrencyHeader(group.Key, _expanded[group.Key]) { Size = new Vector2I(210, 20) };
            header.MouseClick += (s, e) => { _expanded[group.Key] = !header.Expanded; Rebuild(source); };
            header.ExpandButton.MouseClick += (s, e) => { _expanded[group.Key] = !header.Expanded; Rebuild(source); };
            AddLine(header);

            if (!header.Expanded) continue;
            foreach (var currency in group.OrderBy(x => x.Info.Name, StringComparer.Ordinal))
                AddLine(new CurrencyRow(currency));
        }
        Relayout();
    }

    private void AddLine(DXControl line)
    {
        AddControl(line);
        line.MouseWheel += _scroll.DoMouseWheel;
        _lines.Add(line);
    }

    private void Relayout()
    {
        int total = 0;
        foreach (var line in _lines) total += line is CurrencyHeader ? HeaderHeight : RowHeight;
        _scroll.MaxValue = total;
        int y = 0;
        foreach (var line in _lines)
        {
            line.Location = new Vector2I(1, y - _scroll.Value);
            y += line is CurrencyHeader ? HeaderHeight : RowHeight;
        }
    }
}

public sealed partial class CurrencyHeader : DXControl
{
    public CurrencyCategory Category { get; }
    public bool Expanded { get; }
    public DXButton ExpandButton { get; }

    public CurrencyHeader(CurrencyCategory category, bool expanded)
    {
        Category = category;
        Expanded = expanded;
        ExpandButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = expanded ? 4871 : 4870,
            Location = new Vector2I(2, 2),
            Size = new Vector2I(16, 16),
        };
        AddControl(ExpandButton);
        AddControl(new DXLabel
        {
            Text = category.ToString(),
            FontSize = 10,
            TextColour = new Color(1f, .85f, .45f),
            Location = new Vector2I(25, 2),
            IsControl = false,
        });
    }
}

public sealed partial class CurrencyRow : DXControl
{
    private readonly ClientUserCurrency _currency;
    private readonly DXItemCell _item;
    private readonly DXImageControl _currencyImage;
    private readonly DXLabel _name;
    private readonly DXLabel _amount;

    public CurrencyRow(ClientUserCurrency currency)
    {
        _currency = currency;
        Size = new Vector2I(210, 40);
        BackColour = new Color(.08f, .06f, .04f, .45f);
        Border = false;
        BorderColour = new Color(.25f, .25f, .25f);

        _item = new DXItemCell
        {
            GridType = GridType.Inspect,
            ItemGrid = new[] { currency.Info.DropItem == null ? null : new ClientUserItem(currency.Info.DropItem, currency.Amount) },
            Slot = 0,
            ReadOnly = true,
            Location = new Vector2I(2, 2),
        };
        AddControl(_item);

        _currencyImage = new DXImageControl
        {
            LibraryFile = LibraryFile.StoreItem,
            Index = 2683,
            Location = new Vector2I(3, 3),
            Visible = currency.Info.DropItem == null,
            IsControl = false,
        };
        AddControl(_currencyImage);
        _name = new DXLabel { Text = currency.Info.Name ?? currency.Info.Abbreviation, FontSize = 10, Location = new Vector2I(40, 2), Size = new Vector2I(165, 18), IsControl = false };
        _amount = new DXLabel { Text = currency.Amount.ToString("N0"), FontSize = 10, TextColour = new Color(.55f, 1f, .85f), Location = new Vector2I(40, 20), Size = new Vector2I(165, 18), IsControl = false };
        AddControl(_name);
        AddControl(_amount);
        MouseClick += (s, e) => GameScene.Game?.SelectCurrency(_currency);
        _item.MouseClick += (s, e) => GameScene.Game?.SelectCurrency(_currency);
    }

    public void RefreshAmount()
    {
        _amount.Text = _currency.Amount.ToString("N0");
        if (_item.Item != null) _item.Item.Count = _currency.Amount;
        _item.RefreshItem();
    }
}
