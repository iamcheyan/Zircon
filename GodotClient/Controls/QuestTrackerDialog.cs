using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Library;
using Library.SystemModels;

namespace ZirconClient.Controls;

/// <summary>
/// 任务追踪窗口 (移植自 Client/Scenes/Views/QuestTrackerDialog.cs)。
/// 无标题/边框, 悬停半透明; 数据源 = StartInfo.Quests 中 Track=true 项。
/// M12 不移植 QuestIcon 动画与 (Complete) 后的 NPC 指引行图标。
/// </summary>
public partial class QuestTrackerDialog : DXWindow
{
    private DXVScrollBar ScrollBar;
    private DXControl TextPanel;
    private readonly List<DXLabel> Lines = new();

    public QuestTrackerDialog()
    {
        HasFooter = false;
        HasTitle = false;
        HasTopBorder = false;
        Opacity = 0.0f;
        Size = new Vector2I(250, 100);

        ScrollBar = new DXVScrollBar { Change = 15 };
        TextPanel = new DXControl { PassThrough = true };

        AddControl(ScrollBar);
        AddControl(TextPanel);
        MouseEnter += (o, e) => Opacity = 0.3f;
        MouseLeave += (o, e) => Opacity = 0.0f;
        UpdateLayout();
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (ScrollBar == null || TextPanel == null) return;

        int resizeBuffer = 4;
        ScrollBar.Size = new Vector2I(14, (int)Size.Y - resizeBuffer * 2);
        ScrollBar.Location = new Vector2I((int)(Size.X - ScrollBar.Size.X) - resizeBuffer, resizeBuffer);
        ScrollBar.VisibleSize = (int)TextPanel.Size.Y;
        ScrollBar.HideWhenNoScroll = true;

        TextPanel.Location = new Vector2I(0, resizeBuffer);
        TextPanel.Size = new Vector2I((int)(Size.X - ScrollBar.Size.X - 1) - resizeBuffer, (int)Size.Y - resizeBuffer * 2);
        ScrollBar.VisibleSize = (int)TextPanel.Size.Y;
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            ScrollBar.Value -= ScrollBar.Change;
        if (@event is InputEventMouseButton mb2 && mb2.ButtonIndex == MouseButton.WheelDown && mb2.Pressed)
            ScrollBar.Value += ScrollBar.Change;
    }

    /// <summary>任务行 (Track=true), 滚动条复位</summary>
    public void PopulateQuests(IEnumerable<ClientUserQuest> quests)
    {
        foreach (var line in Lines)
            line.QueueFree();
        Lines.Clear();

        if (quests == null)
        {
            Visible = false;
            return;
        }

        foreach (var userQuest in quests.Where(q => q.Track))
        {
            var quest = userQuest.Quest;
            if (quest == null) continue;

            var label = new DXLabel
            {
                Text = quest.QuestName,
                DrawOutline = true,
                OutlineColour = Colors.Black,
                IsControl = false,
                Location = new Vector2I(15, Lines.Count * 15),
            };
            TextPanel.AddControl(label);
            Lines.Add(label);

            if (userQuest.IsComplete)
            {
                label.Text += " (Complete)";
                var finish = new DXLabel
                {
                    Text = $"Goto {quest.FinishNPC?.NPCName} in {quest.FinishNPC?.RegionName}",
                    TextColour = Colors.White,
                    DrawOutline = true,
                    OutlineColour = Colors.Black,
                    IsControl = false,
                    Location = new Vector2I(25, Lines.Count * 15),
                };
                TextPanel.AddControl(finish);
                Lines.Add(finish);
            }
            else
            {
                foreach (var task in quest.Tasks)
                {
                    var userTask = userQuest.Tasks.FirstOrDefault(x => x.Task == task);
                    if (userTask != null && userTask.Completed) continue;

                    var taskLabel = new DXLabel
                    {
                        Text = GetTaskText(task, userQuest),
                        TextColour = Colors.White,
                        DrawOutline = true,
                        OutlineColour = Colors.Black,
                        IsControl = false,
                        Location = new Vector2I(25, Lines.Count * 15),
                    };
                    TextPanel.AddControl(taskLabel);
                    Lines.Add(taskLabel);
                }
            }
        }

        Visible = Lines.Count > 0;
        ScrollBar.Value = 0;
        UpdateScrollBar();
    }

    private void UpdateScrollBar()
    {
        ScrollBar.MaxValue = Lines.Count * 15;

        for (int i = 0; i < Lines.Count; i++)
        {
            var pos = Lines[i].Location;
            Lines[i].Location = new Vector2I(pos.X, i * 15 - ScrollBar.Value);
        }
    }

    /// <summary>任务行文本 (移植自原版 GameScene.GetTaskText)</summary>
    public static string GetTaskText(QuestTask task, ClientUserQuest userQuest)
    {
        var builder = new StringBuilder();

        var userTask = userQuest?.Tasks.FirstOrDefault(x => x.Task == task);

        switch (task.Task)
        {
            case QuestTaskType.KillMonster:
                builder.AppendFormat("Kill {0} ", task.Amount);
                break;
            case QuestTaskType.GainItem:
                builder.AppendFormat("Collect {0} {1}", task.Amount, task.ItemParameter?.ItemName);
                break;
            case QuestTaskType.VisitRegion:
                builder.AppendFormat("Goto {0} in {1}", task.RegionParameter?.Description, task.RegionParameter?.Map?.PlayerDescription);
                break;
        }

        if (string.IsNullOrEmpty(task.MobDescription))
        {
            if (task.Task == QuestTaskType.GainItem && task.MonsterDetails.Count > 0)
                builder.Append(" from ");

            bool needComma = false;
            for (int i = 0; i < task.MonsterDetails.Count; i++)
            {
                var monster = task.MonsterDetails[i];
                if (monster == null) continue;
                if (i > 2)
                {
                    builder.Append("...");
                    break;
                }

                if (needComma)
                    builder.Append(" or ");
                needComma = true;

                builder.Append(monster.Monster?.MonsterName);

                if (monster.Map != null)
                    builder.AppendFormat(" in {0}", monster.Map.PlayerDescription);
            }
        }
        else
        {
            if (task.Task == QuestTaskType.GainItem)
            {
                if (task.MonsterDetails.Count > 0)
                {
                    builder.Append(" from ");
                    builder.Append(task.MobDescription);
                }
            }
            else
            {
                builder.Append(task.MobDescription);
            }
        }

        if (userQuest != null)
        {
            if (userTask != null && userTask.Completed)
                builder.Append(" (Completed)");
            else if (task.Task != QuestTaskType.VisitRegion)
                builder.Append($" ({userTask?.Amount ?? 0}/{task.Amount})");
        }

        return builder.ToString();
    }
}
