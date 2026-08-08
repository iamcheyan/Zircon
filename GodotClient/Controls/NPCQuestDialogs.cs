using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 Interface 209 任务列表：六行可视、滚动条、选中后打开详情。</summary>
public sealed partial class NPCQuestListDialog : DXWindow
{
    private readonly DXControl _list;
    private readonly DXVScrollBar _scroll;
    private readonly List<QuestInfo> _quests = new();
    private readonly List<DXButton> _rows = new();
    private NPCInfo _npc;

    public NPCQuestListDialog()
    {
        HasTitle = false; HasFooter = false; Movable = false;
        var background = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 209, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(background);
        Size = (Vector2I)background.Size;
        AddControl(new DXLabel { Text = "任务列表", FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, Location = new Vector2I(0, 8), Size = new Vector2I((int)Size.X, 18), IsControl = false });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
        int panelWidth = Math.Max(210, (int)Size.X - 25);
        _list = new DXControl { Location = new Vector2I(8, 37), Size = new Vector2I(panelWidth, 134), Clip = true }; AddControl(_list);
        AddControl(new DXLabel { Text = "可接任务", FontSize = 9, Size = new Vector2I(170, 18), Location = new Vector2I(15, 185), IsControl = false, Align = HorizontalAlignment.Center });
        AddControl(new DXLabel { Text = "数量", FontSize = 9, Size = new Vector2I(50, 18), Location = new Vector2I(205, 185), IsControl = false, Align = HorizontalAlignment.Center });
        _scroll = new DXVScrollBar { Location = new Vector2I(panelWidth - 20, 37), Size = new Vector2I(22, 139), VisibleSize = 134, Change = 22, HideWhenNoScroll = true };
        _scroll.UpButton.LibraryFile = LibraryFile.Interface; _scroll.UpButton.Index = 61;
        _scroll.DownButton.LibraryFile = LibraryFile.Interface; _scroll.DownButton.Index = 62;
        _scroll.PositionBar.LibraryFile = LibraryFile.Interface; _scroll.PositionBar.Index = 60;
        _scroll.ValueChanged += (s, e) => RefreshRows(); AddControl(_scroll);
    }

    public void OpenFor(NPCInfo npc)
    {
        _npc = npc;
        _quests.Clear();
        var complete = new List<QuestInfo>();
        var available = new List<QuestInfo>();
        var current = new List<QuestInfo>();
        foreach (var quest in npc?.StartQuests ?? Enumerable.Empty<QuestInfo>())
            if (quest != null && GameScene.Game?.CanAcceptQuest(quest) == true) available.Add(quest);
        foreach (var quest in npc?.FinishQuests ?? Enumerable.Empty<QuestInfo>())
        {
            var userQuest = quest == null ? null : GameScene.Game?.GetUserQuest(quest.Index);
            if (quest == null || userQuest == null || userQuest.Completed) continue;
            if (userQuest.IsComplete) complete.Add(quest); else current.Add(quest);
        }
        static void SortQuests(List<QuestInfo> list) => list.Sort((a, b) => string.Compare(a?.QuestName, b?.QuestName, System.StringComparison.Ordinal));
        SortQuests(complete); SortQuests(available); SortQuests(current);
        _quests.AddRange(complete); _quests.AddRange(available); _quests.AddRange(current);
        _scroll.Value = 0;
        _scroll.MaxValue = _quests.Count * 22;
        RefreshRows();
        if (_quests.Count > 0) WindowManager.Open(this, GameScene.Game?.UILayer);
        else WindowManager.Close(this);
    }

    private void RefreshRows()
    {
        foreach (var row in _rows) { _list.RemoveControl(row); row.QueueFree(); }
        _rows.Clear();
        int first = (int)_scroll.Value / 22;
        for (int i = first; i < _quests.Count && i < first + 7; i++)
        {
            var quest = _quests[i];
            var row = new DXButton
            {
                Text = quest?.QuestName ?? "未知任务", FontSize = 9,
                TextColour = new Color(1f, .85f, .3f), LibraryFile = LibraryFile.Interface, Index = -1,
                Location = new Vector2I(0, (i - first) * 22), Size = new Vector2I(Math.Max(190, (int)_list.Size.X - 25), 21),
            };
            row.MouseClick += (s, e) => GameScene.Game?.OpenNPCQuestDialog(quest);
            _list.AddControl(row); _rows.Add(row);
        }
    }
}

/// <summary>原版 Interface 212 任务详情：描述、目标、奖励和接受/完成按钮。</summary>
public sealed partial class NPCQuestDialog : DXWindow
{
    private readonly DXLabel _name, _description, _tasks;
    private readonly DXItemGrid _rewardGrid, _choiceGrid;
    private readonly ClientUserItem[] _rewards = new ClientUserItem[5];
    private readonly ClientUserItem[] _choices = new ClientUserItem[4];
    private readonly List<QuestReward> _choiceRewards = new();
    private readonly DXButton _accept, _complete;
    private int _selectedChoice = -1;
    private QuestInfo _quest;

    public NPCQuestDialog()
    {
        HasTitle = false; HasFooter = false; Movable = false;
        var background = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 212, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(background);
        Size = (Vector2I)background.Size;
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15 };
        close.Location = new Vector2I((int)Size.X - (int)close.Size.X - 3, 3);
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
        _name = new DXLabel { FontSize = 12, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, Size = new Vector2I(334, 28), Location = new Vector2I(10, 40), IsControl = false }; AddControl(_name);
        _description = new DXLabel { FontSize = 10, TextColour = Colors.White, Size = new Vector2I(313, 81), Location = new Vector2I(13, 86), IsControl = false }; AddControl(_description);
        _tasks = new DXLabel { FontSize = 10, TextColour = Colors.White, Size = new Vector2I(334, 61), Location = new Vector2I(13, 185), IsControl = false }; AddControl(_tasks);
        AddControl(new DXLabel { Text = "奖励", FontSize = 10, DrawOutline = true, Location = new Vector2I(10, 270), IsControl = false });
        _rewardGrid = new DXItemGrid { GridSize = new Vector2I(5, 1), GridType = GridType.None, ItemGrid = _rewards, ReadOnly = true, Location = new Vector2I(12, 292) }; AddControl(_rewardGrid);
        AddControl(new DXLabel { Text = "可选奖励", FontSize = 10, DrawOutline = true, Location = new Vector2I(215, 270), IsControl = false });
        _choiceGrid = new DXItemGrid { GridSize = new Vector2I(4, 1), GridType = GridType.None, ItemGrid = _choices, ReadOnly = true, Location = new Vector2I(217, 292) }; AddControl(_choiceGrid);
        for (int i = 0; i < _choiceGrid.Cells.Length; i++)
        {
            int choice = i;
            _choiceGrid.Cells[i].MouseClick += (s, e) =>
            {
                if (_choices[choice] == null) return;
                _selectedChoice = choice;
                for (int j = 0; j < _choiceGrid.Cells.Length; j++) _choiceGrid.Cells[j].Border = j == choice;
            };
        }
        _accept = new DXButton { Text = "接受任务", FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(250, (int)Size.Y - 43), Size = new Vector2I(80, 25) };
        _accept.MouseClick += (s, e) =>
        {
            if (_quest == null || GameScene.Game?.IsObserver == true) return;
            GameScene.Game.SendQuestAccept(_quest.Index);
            WindowManager.Close(this);
        }; AddControl(_accept);
        _complete = new DXButton { Text = "完成任务", FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(250, (int)Size.Y - 43), Size = new Vector2I(80, 25), Visible = false };
        _complete.MouseClick += (s, e) =>
        {
            if (_quest == null || GameScene.Game?.IsObserver == true) return;
            if (_choiceRewards.Count > 0 && (_selectedChoice < 0 || _selectedChoice >= _choiceRewards.Count))
            {
                GameScene.Game?.ReceiveChat("Please select a reward.", MessageType.System);
                return;
            }
            GameScene.Game?.SendQuestComplete(_quest.Index, _selectedChoice >= 0 ? _choiceRewards[_selectedChoice].Index : 0);
            WindowManager.Close(this);
        };
        AddControl(_complete);
    }

    public void OpenFor(QuestInfo quest)
    {
        _quest = quest;
        if (quest == null) return;
        _name.Text = quest.QuestName ?? "任务";
        var game = GameScene.Game;
        var userQuest = game?.GetUserQuest(quest.Index);
        _description.Text = game?.GetQuestText(quest, userQuest) ?? quest.AcceptText ?? quest.ProgressText ?? quest.CompletedText ?? string.Empty;
        _tasks.Text = game?.GetTaskText(quest, userQuest) ?? string.Empty;
        _selectedChoice = -1;
        _choiceRewards.Clear();
        for (int i = 0; i < _rewards.Length; i++) _rewards[i] = null;
        for (int i = 0; i < _choices.Length; i++) _choices[i] = null;
        int rewardIndex = 0, choiceIndex = 0;
        foreach (var reward in quest.Rewards ?? Enumerable.Empty<QuestReward>())
        {
            if (reward?.Item == null) continue;
            var item = new ClientUserItem(reward.Item, reward.Amount);
            if (reward.Bound) item.Flags |= UserItemFlags.Bound;
            if (reward.Choice)
            {
                if (choiceIndex < _choices.Length) { _choices[choiceIndex] = item; _choiceRewards.Add(reward); choiceIndex++; }
            }
            else if (rewardIndex < _rewards.Length) _rewards[rewardIndex++] = item;
        }
        _rewardGrid.RefreshGrid();
        _choiceGrid.RefreshGrid();
        _accept.Visible = userQuest == null;
        _complete.Visible = userQuest != null && !userQuest.Completed && userQuest.IsComplete;
        WindowManager.Open(this, GameScene.Game?.UILayer);
    }
}
