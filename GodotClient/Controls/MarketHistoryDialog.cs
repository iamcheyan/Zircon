using Godot;
using Library;
using System.Linq;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 MarketPlaceHistoryDialog：显示物品销量、最近成交价和平均价。</summary>
public sealed partial class MarketHistoryDialog : DXWindow
{
    private readonly DXLabel _item, _sales, _last, _average;
    private int _itemIndex;
    private int _display;

    public MarketHistoryDialog()
    {
        Text = "销售历史";
        // 原版 SetClientSize(270,110)：复用通用标题窗口框架，总尺寸为 288x156。
        Size = new Vector2I(288, 156);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(258, 3) };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);
        _item = AddLine(37);
        _sales = AddLine(65);
        _last = AddLine(89);
        _average = AddLine(113);
    }

    private DXLabel AddLine(int y)
    {
        var label = new DXLabel { FontSize = 10, Location = new Vector2I(9, y), Size = new Vector2I(270, 20), IsControl = false };
        AddControl(label);
        return label;
    }

    public void ShowFor(ClientUserItem item)
    {
        if (item?.Info == null) return;
        _itemIndex = item.Info.Index;
        _display++;
        int partIndex = item.AddedStats?[Stat.ItemIndex] ?? 0;
        var displayInfo = item.Info;
        if (item.Info.ItemEffect == ItemEffect.ItemPart && partIndex > 0)
            displayInfo = Globals.ItemInfoList?.Binding.FirstOrDefault(x => x.Index == partIndex) ?? displayInfo;
        _item.Text = displayInfo.ItemName;
        _sales.Text = "销量：查询中……";
        _last.Text = string.Empty;
        _average.Text = string.Empty;
        WindowManager.Open(this, GameScene.Game?.UILayer ?? GetParent());
        GameScene.Game?.SendMarketHistory(_itemIndex, partIndex, _display);
    }

    public void Apply(int index, int display, long saleCount, long lastPrice, long averagePrice)
    {
        if (index != _itemIndex || display != _display) return;
        _sales.Text = $"销量：{saleCount:#,##0}";
        _last.Text = $"最近成交：{lastPrice:#,##0}";
        _average.Text = $"平均价格：{averagePrice:#,##0}";
    }
}
