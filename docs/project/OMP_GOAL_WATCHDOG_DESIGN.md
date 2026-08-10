# OMP Goal 长任务看门狗设计

更新时间：2026-08-10

## 1. 背景

Mir3 原版客户端逆向任务运行在 82 服务器的 OMP Goal 会话中。Goal 虽然支持自动续跑，
但它不是独立的后台作业：一轮模型回复结束后，可能因为续跑条件未满足、内部状态卡住或
工具回合异常而停止等待输入。

已观察到的现象：

- OMP 和 tmux 进程仍然存在，但长时间没有新的工具调用；
- session JSONL 中出现空的 assistant 回合；
- Goal 仍显示为 `active`，输入“继续”后可以恢复；
- 反汇编脚本偶尔返回错误，但通常不会使整个 Goal 进程退出。

看门狗的目的，是发现这种“仍 active、实际不再推进”的状态后自动发送一次“继续”。
它不负责替代 Goal，不负责修改研究结果，也不能把进程退出误判为成功。

## 2. 监控范围

当前目标会话：

```text
机器：82
仓库：/home/tetsuya/development/Mir3-Research
tmux：zircon
Goal session：019feb87-4104-7000-8548-3a0adb440578
session 日志：
  /home/tetsuya/.omp/agent/sessions/-development-Mir3-Research/
```

实际部署时不能永久写死 pane 编号。程序应通过 tmux pane 的工作目录、窗口标题或前台
`omp/bun` 进程识别目标 pane，避免把“继续”发送给其他交互窗口。

## 3. 状态模型

看门狗使用以下状态，而不是简单按照“有没有输出”判断：

```text
UNKNOWN       无法确认 OMP/session 状态
RUNNING       有未完成工具调用或 OMP 正在处理本轮请求
WAITING       Goal 仍 active，但当前没有正在执行的工具
STALLED       WAITING 超过阈值，且没有新的 session 活动
COMPLETED     Goal 明确报告 complete，并通过交付验收
BLOCKED       Goal 明确报告 blocked，等待外部条件或人工决策
FAILED        OMP 进程退出、session 损坏或连续恢复失败
```

### 3.1 RUNNING 的判定

满足任意条件即可认为正在运行：

- session JSONL 最近出现 `tool_execution_start`，且没有对应的 `toolResult`；
- tmux pane 显示工具执行 spinner、命令框或正在等待工具返回；
- OMP 进程存在，并且存在属于它的工具子进程；
- session 文件在最近检查周期持续增长。

处于 RUNNING 时绝不能发送“继续”。尤其要允许较长的反汇编、资源扫描和验证命令运行，
默认单次工具超时可达 300 秒。

### 3.2 WAITING/STALLED 的判定

只有同时满足以下条件，才允许进入 STALLED：

1. Goal 状态仍为 `active`；
2. 没有未完成的工具调用；
3. OMP 进程仍存在；
4. session JSONL 和 tmux 内容在连续多个检查周期没有变化；
5. 当前 pane 没有明显的命令执行状态；
6. 空闲时间超过阈值，建议初始为 8～10 分钟。

单次检查不能触发恢复，至少需要连续两次确认，避免网络盘、NAS 或模型响应延迟造成误判。

## 4. 自动发送策略

进入 STALLED 后：

1. 记录时间、session ID、pane ID、最近 JSONL 事件和当前 Goal 状态；
2. 向目标 pane 写入 `继续` 并发送 Enter；
3. 等待 30～60 秒观察是否出现新的 assistant/tool 事件；
4. 如果恢复，记录 `recovered` 并回到 RUNNING；
5. 如果没有恢复，进入冷却期，不要连续刷屏；
6. 连续 2～3 次恢复失败后标记 FAILED，并停止自动发送，保留日志供人工检查。

建议参数：

```text
检查周期：60 秒
进入 STALLED：连续 2 次无活动，约 10 分钟
恢复后观察：60 秒
发送冷却：10 分钟
最大自动恢复次数：3 次/小时
```

自动发送必须使用 tmux 的目标 pane，而不是向整个 tmux session 广播。任何无法唯一识别
目标 pane 的情况都应保持 UNKNOWN，不发送按键。

## 5. 成功、完成与失败判定

### 5.1 单个工具成功

工具成功需要同时参考：

- `toolResult` 存在；
- 没有 `isError` 或异常标记；
- shell 命令退出码为 0；
- 输出包含该工具预期的结果标记；
- 生成的文件通过基本存在性和格式检查。

单个脚本成功不代表整个 Goal 完成。

### 5.2 Goal 完成

只有满足以下条件才可标记 COMPLETED：

- OMP Goal 明确调用完成状态（例如 `update_goal(status=complete)`），或 session 明确显示
  Goal 已完成；
- 研究仓库的验证脚本通过；
- UI 证据、地图资料、WIL/WIX 资源和 HTML 模拟器达到项目验收标准；
- 结果已写入文档和数据文件；
- Git 工作区、提交和推送状态符合交付要求。

“没有输出”“进程退出”“工具窗口关闭”都不能单独证明完成。

### 5.3 BLOCKED/FAILED

- Goal 明确报告 `blocked`：停止自动发送并报告阻塞原因；
- OMP 进程消失：先记录最后 session 状态，不能直接发送到已不存在的 pane；
- session JSONL 损坏或无法读取：标记 UNKNOWN/FAILED，不修改 session 文件；
- 连续恢复失败：停止看门狗，防止无限输入“继续”。

## 6. 交付前验收建议

看门狗未来可以只负责“唤醒”，最终验收仍由独立脚本完成：

```text
检查 Mir3-Research 工作区和分支
检查研究文档是否存在
运行证据验证脚本
检查 HTML simulator 是否能启动
检查地图/资源数据统计是否达到目标
检查 git diff、commit 和 push 状态
```

验收失败时应把结果写入 watchdog 日志，不应自动把 Goal 标记为完成。

## 7. 安全限制

- 默认只读检查，不删除 session、日志、仓库文件或数据库；
- 不使用 `git reset --hard`、`git clean` 等破坏性命令；
- 不在工具执行期间发送键盘输入；
- 不向不确定的 tmux pane 发送任何内容；
- 发送“继续”必须有冷却时间和最大次数；
- 看门狗本身异常时应退出并保留日志，而不是循环重启 OMP；
- 不自动修改 Goal 的 objective、token budget 或完成状态。

## 8. 推荐实现形态

第一版建议使用一个独立 Python 程序：

```text
Tools/omp_goal_watchdog.py
```

它通过 `tmux list-panes/capture-pane`、读取 session JSONL、`ps`/`pgrep` 和时间戳完成状态
判断，通过 `tmux send-keys` 执行恢复。稳定后再包装为 82 上的 systemd user service，并将
watchdog 自身日志写入：

```text
/home/tetsuya/.local/state/omp-goal-watchdog.log
```

部署前必须先提供 dry-run 模式，只记录“本来会发送继续”的判断，不实际发送按键；连续观测
一段时间无误后，才允许启用自动恢复。
