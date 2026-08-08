using Godot;
using System.Linq;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 MilestoneAchievedDialog：里程碑达成提示与领取入口。</summary>
public sealed partial class MilestoneDialog : DXWindow
{
    private readonly DXLabel _title;
    private readonly DXLabel _description;
    private readonly DXLabel _task;
    private int _index;

    public MilestoneDialog()
    {
        Text = "Milestone Achieved";
        Size = new Vector2I(380, 210);
        _title = new DXLabel { FontSize = 13, TextColour = new Color(1f, .85f, .3f), Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I(340, 28), Location = new Vector2I(20, 35), IsControl = false };
        _description = new DXLabel { FontSize = 10, Size = new Vector2I(340, 50), Location = new Vector2I(20, 72), IsControl = false };
        _task = new DXLabel { FontSize = 9, TextColour = new Color(.75f, .9f, 1f), Size = new Vector2I(340, 35), Location = new Vector2I(20, 126), IsControl = false };
        AddControl(_title); AddControl(_description); AddControl(_task);
        var claim = new DXButton { Text = "领取", FontSize = 10, Size = new Vector2I(80, 25), Location = new Vector2I(150, 174), Index = -1, LibraryFile = LibraryFile.Interface };
        claim.MouseClick += (s, e) => { GameScene.Game?.ClaimMilestone(_index); WindowManager.Close(this); };
        AddControl(claim);
    }

    public void ShowMilestone(ClientUserMilestone milestone)
    {
        if (milestone == null) return;
        _index = milestone.Index;
        var info = milestone.Info ?? Globals.MilestoneInfoList?.Binding.FirstOrDefault(x => x.Index == milestone.InfoIndex);
        _title.Text = info?.Title ?? "Milestone Achieved";
        _description.Text = info?.Description ?? string.Empty;
        _task.Text = info?.Task ?? string.Empty;
        WindowManager.Open(this, GameScene.Game?.UILayer);
    }
}
