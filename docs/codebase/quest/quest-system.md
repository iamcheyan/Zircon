# 任务系统（Quest System）

## TL;DR 速查表

- 任务 = System.db 纯数据配置（`QuestInfo` + `QuestTask`/`QuestReward`/`QuestRequirement`），服务端零脚本判定，配置见 `LibraryCore/SystemModels/QuestInfo.cs:5`。
- 任务类型 6 种：`General/Daily/Weekly/Repeatable/Story/Account`（`LibraryCore/Enum.cs:1952`）；Daily/Weekly/Repeatable 由 `ProcessQuests()` 每 20 秒自动重置（`ServerLibrary/Models/PlayerObject.cs:698`）。
- 任务步骤只有 3 种：`KillMonster / GainItem / VisitRegion`（`LibraryCore/Enum.cs:1981`）。
- 服务器同步协议：`S.QuestChanged`（单个任务全量快照）+ `S.QuestCancelled`（任务删除），登录时经 `S.StartInfo.Quests` 全量下发（`ServerLibrary/Models/PlayerObject.cs:881`）。
- 玩家侧操作 4 个 C 包：`C.QuestAccept / C.QuestComplete / C.QuestTrack / C.QuestAbandon`（`LibraryCore/Network/ClientPackets.cs:629-648`），处理入口 `ServerLibrary/Envir/SConnection.cs:1292-1315`。
- 交付必须站在 FinishNPC 面前；奖励只有物品（`QuestReward.Item`），金币/经验/货币用特殊 ItemEffect 物品表达，"收集即消耗"。
- NPC 对话由 `NPCObject.NPCCall` 循环驱动（`ServerLibrary/Models/NPCObject.cs:24`），客户端发包 `C.NPCCall`/`C.NPCButton`。
- NPC 头顶任务图标是**客户端本地计算**：`GameScene.UpdateQuestIcons()`（`Client/Scenes/GameScene.cs:4295`），服务端不参与。
- GodotClient：任务日志/追踪/奖励多选/接取交付/网络包均已移植；**NPC 头顶/大地图任务图标未完成**（`NPCInfo.CurrentQuest` 从未赋值）。

## 职责概述

任务系统横跨四层：

1. **静态配置层**（System.db）：`QuestInfo` 定义任务本体（名称/类型/四段剧情文本/起止 NPC），子对象 `QuestTask`（步骤）、`QuestReward`（奖励）、`QuestRequirement`（接取条件）由 `NPCInfo.StartQuests/FinishQuests` 反向关联挂到 NPC 上。
2. **存档层**（Users.db）：`UserQuest`/`UserQuestTask` 记录玩家进行中的任务与进度；`QuestType.Account` 的任务挂在 `AccountInfo` 上（全账号共享），其余挂 `CharacterInfo`。
3. **运行时判定层**：杀怪计数在 `MonsterObject.Die` 的掉落处理里（`ServerLibrary/Models/MonsterObject.cs:2906`）；物品收集在 `PlayerObject.GainItem`（`ServerLibrary/Models/PlayerObject.cs:6208`）；区域到达在 `Map.Cell.GetMovement`（`ServerLibrary/Models/Map.cs:547`），靠启动时 `SEnvir.CreateQuestRegions` 把任务绑到地图格子。
4. **表现层**：客户端本地维护 `ClientUserQuest` 列表，计算 NPC 头顶图标、任务追踪器、任务日志三处 UI。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `LibraryCore/SystemModels/QuestInfo.cs` | 5-155 | 任务本体（QuestInfo），全字段 |
| `LibraryCore/SystemModels/QuestInfo.cs` | 157-273 | QuestReward 奖励项 |
| `LibraryCore/SystemModels/QuestInfo.cs` | 275-353 | QuestRequirement 接取条件 |
| `LibraryCore/SystemModels/QuestInfo.cs` | 355-451 | QuestTask 任务步骤 |
| `LibraryCore/SystemModels/QuestInfo.cs` | 453-557 | QuestTaskMonsterDetails 怪物明细 |
| `LibraryCore/Enum.cs` | 1952-1986 | QuestType/QuestIcon/QuestRequirementType/QuestTaskType 枚举 |
| `ServerLibrary/DBModels/UserQuest.cs` | 12-178 | UserQuest 玩家任务存档 |
| `ServerLibrary/DBModels/UserQuest.cs` | 182-254 | UserQuestTask 步骤进度存档 |
| `ServerLibrary/Models/PlayerObject.cs` | 3490-3685 | Quests 属性 + 接取/校验/交付/追踪/放弃五方法 |
| `ServerLibrary/Models/PlayerObject.cs` | 698-749 | ProcessQuests：Daily/Weekly/Repeatable 重置 |
| `ServerLibrary/Models/PlayerObject.cs` | 6208-6333 | GainItem 中 GainItem 类任务物品绑定与收取 |
| `ServerLibrary/Models/MonsterObject.cs` | 2906-3017 | Die 中 KillMonster 计数 + GainItem 任务物品掉落 |
| `ServerLibrary/Models/Map.cs` | 547-578 | Cell.GetMovement：VisitRegion 触发 |
| `ServerLibrary/Envir/SEnvir.cs` | 945-1041 | CreateQuestRegions：把 VisitRegion 任务绑定到 Cell |
| `ServerLibrary/Models/NPCObject.cs` | 24-60 | NPCCall：对话页循环（条件→动作→Say→发包） |
| `ServerLibrary/Models/NPCObject.cs` | 730-784 | CanBeSeenBy：NPC 可见性（含任务条件） |
| `ServerLibrary/Models/PlayerObject.cs` | 10159-10188 | NPCCall/NPCButton：玩家侧对话入口 |
| `ServerLibrary/Envir/SConnection.cs` | 610-626 | C.NPCCall / C.NPCButton 分发 |
| `ServerLibrary/Envir/SConnection.cs` | 1292-1315 | C.Quest* 四包分发 |
| `LibraryCore/Network/ClientPackets.cs` | 629-648 | QuestAccept/QuestComplete/QuestTrack/QuestAbandon |
| `LibraryCore/Network/ServerPackets.cs` | 650-663 | S.NPCResponse（对话页） |
| `LibraryCore/Network/ServerPackets.cs` | 1162-1170 | S.QuestChanged / S.QuestCancelled |
| `LibraryCore/Globals.cs` | 1004-1053 | ClientUserQuest / ClientUserQuestTask 客户端模型 |
| `Client/Scenes/GameScene.cs` | 4295-4366 | UpdateQuestIcons：NPC 任务图标总计算 |
| `Client/Scenes/GameScene.cs` | 4403-4447 | 大地图/小地图 NPC 图标索引算法 |
| `Client/Models/NPCObject.cs` | 208-276 | UpdateQuests：NPC 头顶动画特效 |
| `Client/Scenes/Views/QuestDialog.cs` | 1538-1592 | 任务日志：条目图标算法 |
| `Client/Scenes/Views/QuestTrackerDialog.cs` | 131-192 | 任务追踪器 |
| `Client/Scenes/Views/NPCDialog.cs` | 3549-3574 | 接取/交付按钮发包 |

## 核心流程

### 1. 任务生命周期总览

```text
玩家面向 NPC 发 C.NPCCall → NPCObject.NPCCall(EntryPage) → S.NPCResponse（对话页）
        ↓ 玩家点[接取] C.QuestAccept
QuestAccept() → QuestCanAccept() 校验 → 创建 UserQuest → S.QuestChanged
        ↓ 进行中（三种判定引擎）
MonsterObject.Die → KillMonster 计数 / GainItem 掉任务物品
PlayerObject.GainItem → 收任务物品（够数即消耗删除）
Map.Cell.GetMovement → VisitRegion 置 1
        ↓ 每次进度变化都 Enqueue(S.QuestChanged)
UserQuest.IsComplete（所有 UserQuestTask.Completed）
        ↓ 玩家回 FinishNPC 点[交付] C.QuestComplete
QuestComplete() → 职业过滤奖励 → Choice 校验 → 背包空间校验 → 发物品
→ Completed=true / DateCompleted / Track=false → S.QuestChanged
        ↓ 周期任务由 ProcessQuests 每 20 秒清理（S.QuestCancelled）
```

### 2. 接取：QuestAccept / QuestCanAccept（照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:3500-3573
public void QuestAccept(int index)
{
    if (Dead || NPC == null) return;

    foreach (QuestInfo quest in NPC.NPCInfo.StartQuests)
    {
        if (quest.Index != index) continue;

        if (!QuestCanAccept(quest)) return;

        UserQuest userQuest = SEnvir.UserQuestList.CreateNewObject();

        userQuest.QuestInfo = quest;

        if (quest.QuestType == QuestType.Account)
            userQuest.Account = Character.Account;
        else
            userQuest.Character = Character;

        userQuest.DateTaken = SEnvir.Now;

        Enqueue(new S.QuestChanged { Quest = userQuest.ToClientInfo() });
        break;
    }
}
public bool QuestCanAccept(QuestInfo quest)
{
    if (Quests.Any(x => x.QuestInfo == quest)) return false;

    foreach (QuestRequirement requirement in quest.Requirements)
    {
        switch (requirement.Requirement)
        {
            case QuestRequirementType.MinLevel:
                if (Level < requirement.IntParameter1) return false;
                break;
            case QuestRequirementType.MaxLevel:
                if (Level > requirement.IntParameter1) return false;
                break;
            case QuestRequirementType.NotAccepted:
                if (Quests.Any(x => x.QuestInfo == requirement.QuestParameter)) return false;

                break;
            case QuestRequirementType.HaveCompleted:
                if (Quests.Any(x => x.QuestInfo == requirement.QuestParameter && x.Completed)) break;

                return false;
            case QuestRequirementType.HaveNotCompleted:
                if (Quests.Any(x => x.QuestInfo == requirement.QuestParameter && x.Completed)) return false;

                break;
            case QuestRequirementType.Class:
                switch (Class)
                {
                    case MirClass.Warrior:
                        if ((requirement.Class & RequiredClass.Warrior) != RequiredClass.Warrior) return false;
                        break;
                    case MirClass.Wizard:
                        if ((requirement.Class & RequiredClass.Wizard) != RequiredClass.Wizard) return false;
                        break;
                    case MirClass.Taoist:
                        if ((requirement.Class & RequiredClass.Taoist) != RequiredClass.Taoist) return false;
                        break;
                    case MirClass.Assassin:
                        if ((requirement.Class & RequiredClass.Assassin) != RequiredClass.Assassin) return false;
                        break;
                }
                break;
        }

    }
    return true;
}
```

要点：
- 接取**必须在对话中的 NPC**（`NPC == null` 拒绝），且该任务必须挂在此 NPC 的 `StartQuests` 上——不能隔空接任务。
- `Quests` 属性 = `Character.Quests.Concat(Character.Account.Quests)`（`ServerLibrary/Models/PlayerObject.cs:3492-3498`），账号任务与角色任务合并判重。
- 串行主线靠 `HaveCompleted` 前置条件实现；`QuestInfo.OnCreated` 默认给每个任务加一条"自身 HaveNotCompleted"的自反条件（`LibraryCore/SystemModels/QuestInfo.cs:140-149`），即默认任务不可重复做。

### 3. 交付：QuestComplete（照抄，含奖励发放与容量检查）

```csharp
// ServerLibrary/Models/PlayerObject.cs:3575-3663
public void QuestComplete(C.QuestComplete p)
{
    if (Dead) return;
    if (Dead || NPC == null) return;

    foreach (QuestInfo quest in NPC.NPCInfo.FinishQuests)
    {
        if (quest.Index != p.Index) continue;

        UserQuest userQuest = Quests.FirstOrDefault(x => x.QuestInfo == quest);

        if (userQuest == null || userQuest.Completed || !userQuest.IsComplete) return;

        List<ItemCheck> checks = new List<ItemCheck>();

        bool hasChoice = false;
        bool hasChosen = false;

        foreach (QuestReward reward in quest.Rewards)
        {
            switch (Class)
            {
                case MirClass.Warrior:
                    if ((reward.Class & RequiredClass.Warrior) != RequiredClass.Warrior) continue;
                    break;
                case MirClass.Wizard:
                    if ((reward.Class & RequiredClass.Wizard) != RequiredClass.Wizard) continue;
                    break;
                case MirClass.Taoist:
                    if ((reward.Class & RequiredClass.Taoist) != RequiredClass.Taoist) continue;
                    break;
                case MirClass.Assassin:
                    if ((reward.Class & RequiredClass.Assassin) != RequiredClass.Assassin) continue;
                    break;
            }

            if (reward.Choice)
            {
                hasChoice = true;
                if (reward.Index != p.ChoiceIndex) continue;

                hasChosen = true;
            }

            UserItemFlags flags = UserItemFlags.None;
            TimeSpan duration = TimeSpan.FromSeconds(reward.Duration);

            if (reward.Bound)
                flags |= UserItemFlags.Bound;

            if (duration != TimeSpan.Zero)
                flags |= UserItemFlags.Expirable;

            ItemCheck check = new ItemCheck(reward.Item, reward.Amount, flags, duration);

            checks.Add(check);
        }

        if (hasChoice && !hasChosen)
        {
            Connection.ReceiveChatWithObservers(con => con.Language.QuestSelectReward, MessageType.System);
            return;
        }

        if (!CanGainItems(false, checks.ToArray()))
        {
            Connection.ReceiveChatWithObservers(con => con.Language.QuestNeedSpace, MessageType.System);
            return;
        }

        foreach (ItemCheck check in checks)
        {
            while (check.Count > 0)
                GainItem(SEnvir.CreateFreshItem(check));
        }

        userQuest.Track = false;
        userQuest.Completed = true;
        userQuest.DateCompleted = SEnvir.Now;

        LogMilestone(MilestoneType.QuestComplete, 1, quest: quest);

        if (hasChosen)
            userQuest.SelectedReward = p.ChoiceIndex;

        Enqueue(new S.QuestChanged { Quest = userQuest.ToClientInfo() });
        break;
    }
}
```

奖励发放的关键事实：
- **奖励只有物品**。`QuestReward` 没有 Exp/Gold/技能字段——经验/金币/货币奖励配置成特殊物品（经验物品 `ItemEffect.Experience`，`ServerLibrary/Models/PlayerObject.cs:6274-6280` 在 `GainItem` 里折算成 `GainExperience(item.Count, false)` 并删除物品；货币物品走 `GetCurrency` 累加，`ServerLibrary/Models/PlayerObject.cs:6262-6272`）。技能书就是普通物品，学习逻辑在物品使用侧，不在任务侧。
- 职业过滤用 `RequiredClass` 位掩码；`Choice=true` 的奖励是多选一，`p.ChoiceIndex` 是 `QuestReward.Index`（不是列表下标）。
- 容量检查 `CanGainItems(false, checks)`，空间不足直接 return，任务保持可交付状态。
- 交付完成后 `Completed=true` 但 `UserQuest` 记录保留（供 `HaveCompleted` 前置判断），仅 `Track` 置 false 让追踪器消失。

### 4. 杀怪判定：MonsterObject.Die（照抄核心）

```csharp
// ServerLibrary/Models/MonsterObject.cs:2906-2948（节选）
foreach (UserQuest quest in owner.Quests)
{
    //For Each Active Quest
    if (quest.Completed) continue;
    bool changed = false;

    foreach (QuestTask task in quest.QuestInfo.Tasks)
    {
        bool valid = false;
        int count = 0;
        foreach (QuestTaskMonsterDetails details in task.MonsterDetails)
        {
            if (details.Monster != MonsterInfo) continue;
            if (details.Map != null && CurrentMap.Info != details.Map) continue;

            if (SEnvir.Random.Next(details.Chance) > 0) continue;

            if ((DropSet & details.DropSet) != details.DropSet) continue;

            valid = true;
            count = details.Amount;
            break;
        }

        if (!valid) continue;

        UserQuestTask userTask = quest.Tasks.FirstOrDefault(x => x.Task == task);

        if (userTask == null)
        {
            userTask = SEnvir.UserQuestTaskList.CreateNewObject();
            userTask.Task = task;
            userTask.Quest = quest;
        }

        if (userTask.Completed) continue;

        switch (task.Task)
        {
            case QuestTaskType.KillMonster:
                userTask.Amount = Math.Min(task.Amount, userTask.Amount + count);
                changed = true;
                break;
```

`GainItem` 型任务的物品由怪物直接掉出（`ServerLibrary/Models/MonsterObject.cs:2949-2957`）：`item.UserTask = userTask; item.Flags |= UserItemFlags.QuestItem; item.SetTemporary(true);`——带 `UserTask` 引用的物品落地即与任务绑定。循环尾部统一 `Enqueue(new S.QuestChanged ...)`（同文件 3006-3017 区域，进度变化时）。

### 5. 收集判定：PlayerObject.GainItem（照抄核心）

```csharp
// ServerLibrary/Models/PlayerObject.cs:6212-6235（捡起时绑定）+ 6238-6260（收取）
foreach (UserQuest quest in Quests)
{
    if (quest.Completed) continue;

    foreach (QuestTask task in quest.QuestInfo.Tasks)
    {
        if (task.Task != QuestTaskType.GainItem || task.ItemParameter != item.Info) continue;

        if (task.MonsterDetails.Count > 0) continue;   // 怪物掉落型在 Die() 里已绑定

        UserQuestTask userTask = quest.Tasks.FirstOrDefault(x => x.Task == task);
        ...userTask = SEnvir.UserQuestTaskList.CreateNewObject(); ...
        item.UserTask = userTask;
        item.Flags |= UserItemFlags.QuestItem;
    }
}

if (item.UserTask != null)
{
    if (item.UserTask.Completed) continue;

    item.UserTask.Amount = Math.Min(item.UserTask.Task.Amount, item.UserTask.Amount + item.Count);

    changedQuests.Add(item.UserTask.Quest);

    if (item.UserTask.Completed)
    {
        for (int i = item.UserTask.Objects.Count - 1; i >= 0; i--)
            item.UserTask.Objects[i].Despawn();
    }

    item.UserTask = null;
    item.Flags &= ~UserItemFlags.QuestItem;

    item.SetTemporary(true);
    item.Delete();
    ...
}
```

- **收集即消耗**：任务物品进入背包瞬间计数并删除，玩家永远拿不到手上；进度封顶 `Math.Min(task.Amount, ...)`。
- 循环外批量同步：`foreach (UserQuest quest in changedQuests) Enqueue(new S.QuestChanged { Quest = quest.ToClientInfo() });`（`ServerLibrary/Models/PlayerObject.cs:6329-6330`）。
- 非怪物掉落型（商店购买/合成获得也算）由 `GainItem` 兜底绑定；`MonsterDetails.Count > 0` 的任务物品只认怪物掉落。

### 6. 区域判定：VisitRegion

启动时 `SEnvir.CreateQuestRegions()`（`ServerLibrary/Envir/SEnvir.cs:945-1041`）遍历所有 `QuestInfo.Tasks`，把 `VisitRegion` 任务的 `QuestTask` 塞进目标区域每个 `Cell.QuestTasks` 列表（`Cell.QuestTasks` 定义在 `ServerLibrary/Models/Map.cs:490`）。玩家移动经过格子时：

```csharp
// ServerLibrary/Models/Map.cs:549-578（Cell.GetMovement 内）
if (QuestTasks != null && QuestTasks.Count > 0)
{
    if (ob.Race == ObjectType.Player)
    {
        PlayerObject player = (PlayerObject)ob;

        foreach (var task in QuestTasks)
        {
            var userQuest = player.Quests.FirstOrDefault(x => x.QuestInfo == task.Quest && !x.Completed);

            if (userQuest == null) continue;

            UserQuestTask userTask = userQuest.Tasks.FirstOrDefault(x => x.Task == task);

            if (userTask?.Completed == true) continue;

            if (userTask == null)
            {
                userTask = SEnvir.UserQuestTaskList.CreateNewObject();
                userTask.Task = task;
                userTask.Quest = userQuest;
            }

            userTask.Amount = 1;

            player.Enqueue(new S.QuestChanged { Quest = userQuest.ToClientInfo() });
        }
    }
}
```

注意 `userTask.Amount = 1` 是**直接置 1**，不是累加——区域任务只能"到过即完成"，无法做多次到访/停留计时。

### 7. 周期任务重置：ProcessQuests（照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:698-748（节选）
public void ProcessQuests()
{
    if (SEnvir.Now <= DailyQuestTime) return;

    DailyQuestTime = SEnvir.Now.AddSeconds(20);

    for (int i = Character.Quests.Count - 1; i >= 0; i--)
    {
        bool cancel = false;

        var quest = Character.Quests[i];

        switch (quest.QuestInfo.QuestType)
        {
            case QuestType.Daily:
                {
                    if (quest.Completed && quest.DateCompleted.Date != DateTime.UtcNow.Date)
                    {
                        Character.Quests.RemoveAt(i);
                        cancel = true;
                    }
                }
                break;
            case QuestType.Weekly:
                {
                    CultureInfo cul = CultureInfo.CurrentCulture;
                    ... // 跨 ISO 周则移除
                }
                break;
            case QuestType.Repeatable:
                {
                    if (quest.Completed)
                    {
                        Character.Quests.RemoveAt(i);
                        cancel = true;
                    }
                }
                break;
        }

        if (cancel)
        {
            Enqueue(new S.QuestCancelled { Index = quest.Index });
        }
    }
}
```

- Daily：跨 UTC 日删除已完成记录（可重接）；Weekly：跨周删除；Repeatable：完成即删（立刻可重接）。
- 只有 `Character.Quests`（角色级）会被重置清理；`Account` 型任务不参与此循环。
- `ProcessQuests` 由玩家主循环每 tick 调用（`ServerLibrary/Models/PlayerObject.cs:349`），内部用 20 秒节流。

### 8. NPC 对话驱动

服务端对话引擎（`ServerLibrary/Models/NPCObject.cs:24-60`）：

```csharp
public void NPCCall(PlayerObject ob, NPCPage page)
{
    while (true)
    {
        if (page == null) return;

        if (!CheckPage(ob, page, out NPCPage failPage))   // 条件不满足 → 跳 FailPage
        {
            page = failPage;
            continue;
        }

        DoActions(ob, page);                               // 执行页面动作（传送/给物品/给金币/剧情标记…）

        if (string.IsNullOrEmpty(page.Say))
        {
            if (page.SuccessPage != null)
            {
                page = page.SuccessPage;                   // 无文本页自动跳转
                continue;
            }

            ob.NPC = null;
            ob.NPCPage = null;
            ob.Enqueue(new S.NPCClose());
            return;
        }

        var values = GetValues(ob, page);

        ob.NPC = this;
        ob.NPCPage = page;

        ob.Enqueue(new S.NPCResponse { ObjectID = ObjectID, Index = page.Index, Values = values });
        break;
    }
}
```

玩家侧入口（`ServerLibrary/Models/PlayerObject.cs:10159-10188`）：`NPCCall(uint objectID)` 校验面前的 NPC 并调用 `ob.NPCCall(this, NPCInfo.EntryPage)`；`NPCButton(int buttonID)` 在当前 `NPCPage.Buttons` 中找 `ButtonID` 匹配项跳 `DestinationPage`。包分发在 `ServerLibrary/Envir/SConnection.cs:610-626`（客户端点击 NPC 发 `C.NPCCall`，见 `Client/Scenes/Views/MapControl.cs:767`；点对话按钮发 `C.NPCButton`，见 `Client/Scenes/Views/NPCDialog.cs:610`）。

NPC 可见性（决定 NPC 是否显示，间接决定能否接任务）由 `NPCObject.CanBeSeenBy`（`ServerLibrary/Models/NPCObject.cs:730-784`）逐条检查 `NPCInfo.Requirements`：`MaxLevel/MinLevel/Accepted/NotAccepted/HaveCompleted/HaveNotCompleted/Class/DaysOfWeek`（星期几可见用位掩码，`ServerLibrary/Models/NPCObject.cs:775-779`）。

接取/交付按钮在客户端任务页里（`Client/Scenes/Views/NPCDialog.cs`）：

```csharp
// Client/Scenes/Views/NPCDialog.cs:3549-3550, 3572-3574
CEnvir.Enqueue(new C.QuestAccept { Index = SelectedQuest.QuestInfo.Index });
...
CEnvir.Enqueue(new C.QuestComplete { Index = SelectedQuest.QuestInfo.Index, ChoiceIndex = ((QuestReward)SelectedCell?.Tag)?.Index ?? 0 });
```

### 9. 客户端任务图标算法（纯客户端）

服务端**不发任何任务图标**。原版客户端在每次任务数据变化后调用 `UpdateQuestIcons()`（`Client/Scenes/GameScene.cs:4295-4359`）：

```csharp
// Client/Scenes/GameScene.cs:4295-4339（节选）
public void UpdateQuestIcons()
{
    foreach (NPCInfo info in Globals.NPCInfoList.Binding)
        info.CurrentQuest = null;              // 清空所有 NPC 的 CurrentQuest

    bool completed = false;

    foreach (QuestInfo quest in QuestBox.CurrentTab.Quests)      // 进行中任务：标到 FinishNPC
    {
        if (quest?.FinishNPC == null) continue;

        ClientUserQuest userQuest = QuestLog.First(x => x.Quest == quest);

        if (quest.FinishNPC.CurrentQuest != null) continue;

        var current = new CurrentQuest { Type = quest.QuestType };

        if (userQuest.IsComplete)
        {
            current.Icon = QuestIcon.Complete;   // 可交付（金色?）
            completed = true;
        }
        else
        {
            current.Icon = QuestIcon.Incomplete; // 进行中（白色?）
        }

        quest.FinishNPC.CurrentQuest = current;
    }

    foreach (QuestInfo quest in QuestBox.AvailableTab.Quests.OrderBy(q => QuestDialog.QuestTypeOrder.IndexOf(q.QuestType)))
    {
        if (quest?.StartNPC == null) continue;

        if (quest.StartNPC.CurrentQuest != null) continue;

        quest.StartNPC.CurrentQuest = new CurrentQuest          // 可接：标到 StartNPC
        {
            Type = quest.QuestType,
            Icon = QuestIcon.New
        };
    }
    ... // 更新 BigMap/MiniMap/MainPanel 提示灯 + 地图对象特效
}
```

图标索引换算（QuestIcons.Zl 图库，`Client/Models/NPCObject.cs:208-267` 头顶特效、`Client/Scenes/GameScene.cs:4403-4447` 地图标记共用同一套偏移）：

```csharp
// Client/Models/NPCObject.cs:222-257（头顶特效）
switch (CurrentQuest.Type)
{
    case QuestType.General:    startIndex = 10; break;
    case QuestType.Daily:      startIndex = 70; break;
    case QuestType.Weekly:     startIndex = 70; break;
    case QuestType.Repeatable: startIndex = 10; break;
    case QuestType.Story:      startIndex = 50; break;
    case QuestType.Account:    startIndex = 30; break;
}

switch (CurrentQuest.Icon)
{
    case QuestIcon.New:       startIndex += 0; break;
    case QuestIcon.Incomplete: startIndex = 0;  break;   // 进行中回到 0 组（灰）
    case QuestIcon.Complete:  startIndex += 2;  break;   // 可交付用 +2 帧（亮）
}
```

任务日志/追踪器条目用同一图库的另一组基址（如 `QuestTrackerDialog` 用 BaseIndex 83 起，`Client/Scenes/Views/QuestTrackerDialog.cs:132-192`；`QuestDialog.cs:1538-1592` 逻辑相同）。

## 数据结构/协议细节

### QuestInfo 全字段（LibraryCore/SystemModels/QuestInfo.cs:5-155）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `QuestName` | string `[IsIdentity]` | 8-21 | 任务唯一标识名（编辑器主键） |
| `QuestType` | QuestType | 23-36 | General/Daily/Weekly/Repeatable/Story/Account |
| `AcceptText` | string | 38-51 | 接取阶段剧情文本 |
| `ProgressText` | string | 53-66 | 进行中对话文本 |
| `CompletedText` | string | 68-81 | 可交付对话文本 |
| `ArchiveText` | string | 83-96 | 完成/归档文本 |
| `Requirements` | `DBBindingList<QuestRequirement>` | 98-99 | 接取条件列表 |
| `StartNPC` | NPCInfo | 101-115 | 接取 NPC（反向 Association "StartQuests"） |
| `FinishNPC` | NPCInfo | 117-131 | 交付 NPC（反向 Association "FinishQuests"） |
| `Rewards` | `DBBindingList<QuestReward>` | 134-135 | 奖励列表 |
| `Tasks` | `DBBindingList<QuestTask>` | 137-138 | 步骤列表 |

### QuestReward（LibraryCore/SystemModels/QuestInfo.cs:157-273）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `Quest` | QuestInfo | 159-173 | 所属任务 |
| `Item` | ItemInfo | 175-188 | 奖励物品（经验/金币/货币均为特殊物品） |
| `Amount` | int | 190-203 | 数量 |
| `Choice` | bool | 205-218 | true=多选一组；全组只能选一个（用 QuestReward.Index 选择） |
| `Bound` | bool | 220-233 | true=发放时加 `UserItemFlags.Bound` |
| `Duration` | int | 235-248 | 秒；非 0 加 `UserItemFlags.Expirable` |
| `Class` | RequiredClass | 250-263 | 职业位掩码过滤 |

### QuestRequirement（LibraryCore/SystemModels/QuestInfo.cs:275-353）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `Quest` | QuestInfo | 277-291 | 所属任务 |
| `Requirement` | QuestRequirementType | 293-306 | MinLevel/MaxLevel/NotAccepted/HaveCompleted/HaveNotCompleted/Class |
| `IntParameter1` | int | 308-321 | 等级参数 |
| `QuestParameter` | QuestInfo | 323-336 | 前置任务引用（串行链） |
| `Class` | RequiredClass | 338-351 | 职业位掩码 |

### QuestTask（LibraryCore/SystemModels/QuestInfo.cs:355-451）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `Quest` | QuestInfo | 357-371 | 所属任务 |
| `Task` | QuestTaskType | 373-386 | KillMonster/GainItem/VisitRegion |
| `ItemParameter` | ItemInfo | 388-401 | GainItem 目标物品 |
| `RegionParameter` | MapRegion | 403-417 | VisitRegion 目标区域（反向 "RegionQuestTasks"） |
| `MobDescription` | string | 419-432 | 客户端显示用怪物描述文本 |
| `Amount` | int | 434-447 | 所需数量（Kill/Gain 计数上限；Visit 恒比 1） |
| `MonsterDetails` | `DBBindingList<QuestTaskMonsterDetails>` | 449-450 | 怪物明细 |

### QuestTaskMonsterDetails（LibraryCore/SystemModels/QuestInfo.cs:453-557）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `Task` | QuestTask | 455-469 | 所属步骤 |
| `Monster` | MonsterInfo | 471-485 | 目标怪物 |
| `Map` | MapInfo（可空） | 487-501 | 限定地图，null=不限 |
| `Chance` | int | 503-516 | 1/Chance 掉率（OnCreated 默认 1=必成） |
| `Amount` | int | 518-531 | 每次命中计数（默认 1） |
| `DropSet` | int | 534-548 | 位掩码，需与怪物 DropSet 相与相等才有效 |

### UserQuest / UserQuestTask（ServerLibrary/DBModels/UserQuest.cs）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `QuestInfo` | QuestInfo | 14-27 | 任务配置 |
| `Character` | CharacterInfo | 29-43 | 归属角色（非 Account 型） |
| `Account` | AccountInfo | 45-59 | 归属账号（Account 型任务全角色共享） |
| `Completed` | bool | 61-74 | **已交付**（不是"任务条件达成"） |
| `SelectedReward` | int | 76-89 | 多选一奖励的 QuestReward.Index |
| `Track` | bool | 91-104 | 是否显示在追踪器（创建时默认 true，交付置 false） |
| `DateTaken` | DateTime | 106-119 | 接取时间 |
| `DateCompleted` | DateTime | 121-134 | 交付时间（Daily 判断当日重置用） |
| `IsComplete` | 计算属性 | 137-138 | `Tasks.Count == QuestInfo.Tasks.Count && Tasks.All(x => x.Completed)` |
| `Tasks` | `DBBindingList<UserQuestTask>` | 141-142 | 各步骤进度 |

`UserQuestTask`（同文件 182-254）：`Quest`(185)、`Task`(200)、`Amount`(215)、`Completed => Amount >= Task.Amount`(231)、`Objects` 地面物品引用列表(253)。

### 状态机

```text
[未接取] ──C.QuestAccept(经 NPC 对话)──> [进行中 UserQuest{Completed=false}]
    进行中: UserQuestTask.Amount 累加（Kill/Gain）或置 1（Visit）
    某步骤 Amount >= Task.Amount → 该步骤 Completed
    全部步骤 Completed 且步骤数齐全 → UserQuest.IsComplete == true（可交付）
[进行中] ──C.QuestComplete(在 FinishNPC)──> [已交付 Completed=true, Track=false]
[进行中] ──C.QuestAbandon──> Character.Quests.Remove + S.QuestCancelled → [删除]
[已交付] ──ProcessQuests──> Daily: 跨日删 / Weekly: 跨周删 / Repeatable: 立即删 + S.QuestCancelled → [删除，可重接]
             General/Story/Account: 永久保留（供 HaveCompleted 前置判断）
```

注意两个"完成"语义：`IsComplete`（条件达成、可交付）与 `Completed`（已交付存档标记），所有文档/移植都必须区分。

### 协议包

| 包 | 方向 | 定义 | 载荷 |
|---|---|---|---|
| `C.QuestAccept` | C→S | `LibraryCore/Network/ClientPackets.cs:629-632` | `Index`（QuestInfo.Index） |
| `C.QuestComplete` | C→S | 同上 633-638 | `Index` + `ChoiceIndex` |
| `C.QuestTrack` | C→S | 同上 639-644 | `Index` + `Track` |
| `C.QuestAbandon` | C→S | 同上 645-648 | `Index` |
| `S.QuestChanged` | S→C | `LibraryCore/Network/ServerPackets.cs:1162-1165` | `ClientUserQuest Quest`（全量快照） |
| `S.QuestCancelled` | S→C | 同上 1167-1170 | `Index`（UserQuest.Index） |
| `S.NPCResponse` | S→C | 同上 650-663 | `ObjectID`/`Index`(NPCPage)/`Values`；客户端 `Complete()` 用 Index 查 `NPCPageList` |
| `S.StartInfo.Quests` | S→C | `ServerLibrary/Models/PlayerObject.cs:881` | 登录全量 `ClientUserQuest` 列表 |

客户端模型 `ClientUserQuest`/`ClientUserQuestTask`（`LibraryCore/Globals.cs:1004-1053`）是 `UserQuest` 的镜像快照：`Quest`/`IsComplete` 标了 `[IgnorePropertyPacket]`，靠 `QuestIndex`/`TaskIndex` 在 `Complete()` 反查全局配置表。

### 交叉引用：研究文档

- 【研究文档】`/home/tetsuya/development/Mir3-Research/docs/quest-design/14-任务系统实现调查报告.md`：对任务系统的独立调查，结论与本文一致（零脚本、三步骤引擎、DataList/DataValue 剧情分支、Daily/Weekly 重置）；另指出 6 个已知限制——`NPCCheckType.Gender`/`NPCActionType.Message` 枚举存在但服务端未实现、VisitRegion 只检测一次、CheckDataList 无反向语义、NPC 可见性缓存在视野级、已交付任务不可重复交付。
- 【研究文档】`quest-design/` 目录 01-16 号文档是任务**内容设计**（三线序章/共享主线/技能觉醒/副线/悬赏板/NPC 总表/DataList 标记登记表），可直接作为 System.db 任务配置的需求来源。

## GodotClient 现状

| 功能 | 状态 | Godot 文件与证据 |
|---|---|---|
| 任务日志窗口（当前/可接/已完成三页） | 已移植 | `GodotClient/Controls/QuestDialog.cs:12`（DXWindow，732x480，含详情面板/滚动条/放弃确认 `ConfirmAbandon` 432） |
| 任务追踪器 | 已移植（静态帧） | `GodotClient/Controls/QuestTrackerDialog.cs:10-15`——注释明确"QuestIcons.Zl 分类/完成状态索引；Godot 版保留静态帧"；快捷键 L（`GodotClient/Scripts/ClientSettings.cs:34`、`GodotClient/Controls/KeyBindManager.cs:31`） |
| 奖励多选一弹窗 | 已移植 | `GodotClient/Controls/QuestRewardChoiceDialog.cs:36-39`（`SendQuestComplete(quest.Index, choice.Index)`） |
| NPC 对话任务接取/交付按钮 | 已移植 | `GodotClient/Controls/NPCQuestDialogs.cs:131-146`（`SendQuestAccept`/`SendQuestComplete`） |
| NPC 对话页渲染（S.NPCResponse） | 已移植 | `GodotClient/Controls/NPCDialog.cs:50-128`（`ShowPage`：`<ID:默认>` 动态值替换、按钮正则、DialogType 分发） |
| C→S 任务四包 | 已移植 | `GodotClient/Scripts/GameScene.cs:390-408`（`SendQuestAccept/Complete/Track/Abandon`，接取/交付带音效 `SoundIndex.QuestTake/QuestComplete`，见 `GodotClient/SoundCatalog/SoundCatalog.cs:97-98`） |
| S→C 任务包处理 | 已移植 | `GodotClient/Scripts/GameScene.cs:2615-2635`（`OnQuestChanged`/`OnQuestCancelled`/`RefreshQuestUi`）；网络事件注册 `GodotClient/Network/ServerConnection.cs:254-255, 564-565` |
| 主面板任务提示灯 | 已移植 | `GodotClient/Controls/MainPanel.cs:80-103, 342-346`（AvailableQuestIcon Index 240 / CompletedQuestIcon Index 241） |
| 小地图/大地图 NPC 任务标记 | 部分移植 | `GodotClient/Controls/MapMarkerFactory.cs:16-48` 完整复刻了图标索引算法（16/76/基址/Incomplete→2/Complete→+2），但它读取的 `npc.CurrentQuest` **在 GodotClient 中没有任何代码赋值**（全库 grep `CurrentQuest =` 无结果）——原版在 `Client/Scenes/GameScene.cs:4295` 的 `UpdateQuestIcons()` 里计算，Godot 缺这段，导致地图任务图标实际不显示 |
| NPC 头顶任务动画特效 | 未移植 | 原版 `Client/Models/NPCObject.cs:208-269`（MirEffect 循环动画 + AdditionalOffSet(0,-80)）；GodotClient 无 `QuestEffect`/`UpdateQuests` 对应实现（grep 无结果） |
| 登录全量任务数据 | 已移植 | `GodotClient/Scripts/GameScene.cs:4954-4963`（`info.Quests` 逐条 `Complete()` 后装 `_userQuests`，并刷新追踪器/任务日志/主面板提示灯）；追踪器数据源为其 `Track=true` 项（`GodotClient/Controls/QuestTrackerDialog.cs:12`） |

## 移植注意事项

1. **服务端是权威，客户端图标是本地计算**。Godot 移植必须补一个 `UpdateQuestIcons` 等价物：遍历 `StartInfo.Quests`（进行中→FinishNPC 标 Complete/Incomplete）+ `NPCInfo.StartQuests`（可接→StartNPC 标 New），写入 `NPCInfo.CurrentQuest`，否则 `MapMarkerFactory` 永远画不出任务图标。
2. `QuestChanged` 是**全量快照**（整个 `ClientUserQuest`），不是增量字段——客户端直接 `_userQuests[quest.Index] = quest` 覆盖即可（`GodotClient/Scripts/GameScene.cs:2620`），但必须先 `quest.Complete()` 反查 `QuestInfoList`，否则 `IsComplete`（依赖 `Quest.Tasks.Count`）不可用。
3. 两个"完成"语义（`IsComplete` vs `Completed`）在 UI 上要区分：金/亮图标=可交付，灰图标=进行中，"!"=可接。
4. 任务物品"收集即消耗"依赖 `UserItemFlags.QuestItem` + `UserTask` 引用 + `SetTemporary(true)` 三件套；地面任务物品对非归属玩家不可见（`ServerLibrary/Models/ItemObject.cs:160-171`），移植客户端地面物品渲染时注意服务端本来就不发别人的任务物品。
5. QuestIcon 图库（QuestIcons.Zl）索引规则：类型决定基址（General/Repeatable=10、Story=50、Account=30、Daily/Weekly=70；地图标记是 General=16、Daily=76），图标状态再加偏移（New=+0、Complete=+2、Incomplete 归 0/2 组）。Godot `MapMarkerFactory`/`QuestTrackerDialog.QuestIconIndex` 已照抄，勿另起炉灶。
6. 周期任务重置是服务端主动推 `QuestCancelled`，客户端只需删记录刷新 UI，不要自己按时间算。
7. `C.QuestComplete.ChoiceIndex` 传的是 `QuestReward.Index`（配置对象索引），不是奖励列表下标——Godot `QuestRewardChoiceDialog.cs:37` 传 `choice.Index`，正确。
