# Godot 客户端 UI 按钮接线审计 — 2026-08-13

> 任务：用户反馈"UI 里好多东西点不了（设置/装备页/行会等）"，要求逐窗口逐按钮检查链路。
> 本文 = 自动化扫描 + 人工链路核对结果 + 已知空壳清单。HEAD = `0089bfe`。

## 一、审计方法（三层）

1. **静态扫描**：全部 Controls/*.cs 统计 `new DXButton` vs `.MouseClick +=`（脚本见本文附录）
2. **模式校正**：本项目按钮分两种接线模式，扫描缺口≠真缺口——
   - 模式 A：按钮类内部绑定（多数窗口）
   - 模式 B：MainPanel 建按钮、**GameScene 统一绑事件**（主面板 9 键全是这种）
3. **链路核对**：对"绑定了事件"的按钮，追事件处理器是否真做事（发网络包/开窗口/改状态），
   以及服务端有无对应 handler

## 二、总账（好消息）

| 范围 | 结果 |
|---|---|
| 主面板 9 按钮（角色/背包/腰带/技能/任务/菜单/邮件/组队/商城） | ✅ 全部接线（GameScene 4419-4447） |
| 60+ 窗口文件静态扫描 | 仅 5 处疑似缺口，逐一核实后**全部为假阳性**（模式 B 或内部封装） |
| 行会窗口 GuildDialog | ✅ 28 按钮 28 绑定；G 键开窗；客户端 SendGuildCreate + 服务端 GuildCreate + GM @CREATEGUILD 全链路在 |
| 设置界面 | ✅ 当日已修（4df0ef3：下拉遮挡/音频接线/特效开关/滚动条） |

**结论：客户端按钮接线层没有系统性缺口。**用户感觉"点不了"的来源按已证实的可能性排序：
① 设置界面下拉被遮挡（已修 4df0ef3）
② 无行会时行会窗空白（属数据缺失，本次补建行会）
③ 音效无反馈造成的"没反应"错觉（BGM/音量已修 93c7ecb/4df0ef3）
④ **未逐一游戏内实测的窗口仍是风险区**（见 §四清单）

## 三、逐窗口接线明细（静态扫描原始数据）

绑定数≥按钮数 = 正常。文件名后括号 = (按钮数/绑定数/物品格/格绑定)。

- 全部正常：GuildDialog(28/28)、GameStoreDialog(14/17)、CommunicationDialog(10/19)、
  ConsignmentDialog(10/17)、ChatOptionsDialog(6/9)、GroupDialog(8/10)、
  InventoryDialog(4/7)、MenuDialog(2/8)、KeyBindDialog(1/4)、CharacterDialog(5/6)、
  DungeonFinderDialog(5/6)、CurrencyDialog(2/4)、TradeDialog(4/5)、
  NPCCompanionStorageDialog(4/6)、NPCQuestDialogs(5/6)、QuestDialog(3/4)、
  BundleDialog(2/3)、ExitDialog(2/3)、MagicDialog/HelpDialog/MonsterDialog/
  MilestoneDialog/FortuneCheckerDialog(4/4)/FishingDialog(3/3)/RankingDialog(5/5)/
  AutoPotionDialog(5/5)/StorageDialog/ItemAmountDialog/NPCSocketDialogs(4/4)/
  FilterDropDialog(2/2)/ChatTextBox(2/2)/BigMapDialog(2/2)/NPCRepairPanel(2/2)/
  GuildMemberDialog(3/3)/NPCGoodsPanel(3/3)/MiniMapDialog(3/3)/EditCharacterDialog(4/4)/
  ConfirmDialog(2/2)/GameStoreGiftDialog(2/2)/MarketHistoryDialog(1/1)/
  CaptionDialog(1/1)/DXColourControl(4/4)/MagicBar(2/2)/DXWindow(1/1)

- 疑似缺口 5 处（全部核实为假阳性）：
  | 文件 | 疑似 | 真相 |
  |---|---|---|
  | MainPanel(1/0) | 1 | 模式 B：GameScene 绑（9 键全有） |
  | CompanionDialog(4/3) | 1 | 第 4 个是 _saveFilter，AddBottomButton 封装内绑定 |
  | ConfigDialog(4/3) | 1 | KeyBind 按钮在别处绑 |
  | DXVScrollBar(3/2) | 1 | 滚动条内部机制，无需 MouseClick |
  | LegacyPanelDialog(2/1) | 1 | 遗留面板，另一按钮走关闭封装 |

## 四、游戏内待实测清单（下一步）

静态层通过 ≠ 运行时可点。以下窗口需要开服后进游戏逐个点开验证
（用 TestHero GM 号 + Xvfb :100/:101 无头环境，流程见 mir3-project skill）：

**P0（用户点名的）**
- [ ] 设置界面（4df0ef3 修复后回归：下拉开合/音量即时生效/特效开关/分辨率切换）
- [ ] 装备/角色窗（Q/W 键 + 主面板按钮两种入口）
- [ ] 行会窗（G 键）——本日补建行会后验证 28 按钮与页签

**P1（高频交互）**
- [ ] 背包→物品拖拽/右键使用/双击装备
- [ ] 技能窗→拖到快捷栏→快捷栏释放
- [ ] 任务窗→追踪
- [ ] 组队/交易/商店/寄售（需要两个号：test2 注册后可互测）

**P2（低频/依赖服务端功能）**
- [ ] 排行榜/邮件/商城（支付链路走不通属正常，仅验证 UI 响应）
- [ ] 宠物窗/钓鱼/副本查找器（服务端对应系统未启用的，UI 空白属预期）

## 五、附录：扫描脚本

```bash
cd GodotClient && python3 - << 'EOF'
import re, os, glob
for f in sorted(glob.glob('Controls/*.cs')):
    src = open(f, encoding='utf-8', errors='ignore').read()
    b = len(re.findall(r'new DXButton', src))
    c = len(re.findall(r'\.MouseClick\s*\+=', src))
    if b - c > 0: print(f"{os.path.basename(f)}: {b} 按钮 {c} 绑定 缺口{b-c}")
EOF
```

## 六、行会补建（本日动作）

服务端有 GM 命令 `@CREATEGUILD 行会名`（ServerLibrary/Envir/Commands/Command/Admin/CreateGuild.cs，
建 GuildInfo + Leader 角色，MemberLimit 10）。TestHero 所在账号无行会即可建。
执行方式：进游戏聊天框输入（待服务端空闲窗口执行，见 §四 P0）。
