# 交接任务：下载第三方完整 Zircon 客户端并与本地对比贴图

> 给其他智能体 / 协作者用的**可执行任务书**  
> 日期：2026-08-09  
> 仓库：`/home/tetsuya/development/Zircon`  
> 背景文档：`docs/RESEARCH_CAVE_TILE_BLACK_FLOOR.md`、`docs/MAP_TILE_DESERTED_MINE_BUG.md`

---

## 一、任务目标（必须完成）

1. 找到 **2～4 个靠谱的「完整客户端」**（不是登录器/下载器/只有 exe 的发布包）。
2. **下载到本机仓库**，建议目录：
   ```text
   /home/tetsuya/development/Zircon/Debug/reference-clients/
   ```
3. 与当前本地客户端资源对比，**重点**：
   ```text
   Data/Map Data/Tiles5c.Zl
   ```
   尤其是帧 **20～24** 是否仍为纯黑（废矿/僵尸洞地面用的就是这些帧）。
4. 写一份对比结果：MD5、文件大小、20–24 是否有纹理、是否值得替换本地资源。

**当前本地对照基准：**

| 文件 | 路径 | MD5 |
|------|------|-----|
| 本地 Tiles5c | `Debug/Client/Data/Map Data/Tiles5c.Zl` | `286b1220005970c6d511e0a9599f0d11` |
| 本地大小 | 10 673 688 字节 | |
| 已下 LOMCN Client.7z | `Debug/Client.7z` | `0837bfb278e354a2940c426639553aba` |
| 从 Client.7z 抽出的 Tiles5c | `Debug/client7z-tiles-compare/Tiles5c.Zl` | `7585c6e3f3e85306e7448c70aa8a6694`（20–24 仍黑） |

**已知结论（不要重复踩坑）：**

- LOMCN 官方 `Client.7z`（2019）与当前 `patch` 的 `Tiles5c` **帧 20–24 都是纯黑**。
- GitHub/Gitee 上「Client-v1.x-win-x86.zip」通常只有 **1～2 MB 可执行文件**，**不是**完整客户端。
- 完整 Data（含地图贴图）多数在 **百度网盘 / QQ 群 / 夸克网盘**，体积约 **3GB 级**。

---

## 二、什么叫「完整客户端」（验收标准）

**算完整客户端（要下）：**

- 解压后有可运行目录，且包含类似：
  - `Data/`（或 `Data\Map Data\`）
  - `Map/`（.map 文件）
  - 客户端主程序（`Client.exe` / `Zircon.exe` / `Legend.exe` / `传奇3国际版.exe` 等）
- `Data/Map Data/Tiles5c.Zl` **存在且体积 ≥ 数 MB**（正常约 10MB 级）

**不算完整客户端（跳过）：**

- 仅登录器 / 启动器 / Patcher / Launcher
- GitHub Release 里只有 `Client-v*.zip` 且 **&lt; 50MB**（几乎一定是空壳 exe）
- 只有服务端、没有客户端 Data
- 需要淘宝购买且无公开盘链的不明压缩包（安全风险高，除非用户明确要求）

---

## 三、推荐下载清单（按优先级）

### 优先级 A（最推荐：有开源文档 + 明确完整资源包）

#### A1. 皓石传奇三 · ZirconLegend Client（完整运行资源 ~3GB）

| 项 | 内容 |
|----|------|
| 性质 | 国内开源改版，文档写明「海量地图和道具资源，压缩后仍有约 3GB」 |
| 客户端项目 | https://gitee.com/raphaelcheung/zircon-legend-client （GitHub 镜像：raphaelcheung/zircon-legend-client） |
| **完整 Data 盘链（主）** | https://pan.baidu.com/s/1dKrpu6G4p4klMVOIMuhOdA?pwd=j1rm |
| 提取码 | `j1rm` |
| QQ 群备源 | 915941142（README 写盘慢可去群文件） |
| 服务端（可选，非本次必须） | https://gitee.com/raphaelcheung/zircon-legend-server 或 fork |
| 服务端运营数据盘链（约 800MB，地图等） | README 中「百度网盘 2024-8-15」一类链接，以页面为准 |
| 注意 | GitHub Release 的 `Client-v1.13.0-win-x86.zip` **只有约 1.5MB，不要当成完整客户端**；必须下网盘「运行文件/依赖数据」那份 |

**落地目录建议：**

```text
Debug/reference-clients/A1-haoshi-zircon-legend/
  README.txt          # 来源 URL、下载时间、文件列表、MD5
  raw/                # 原始压缩包
  extracted/          # 解压后的完整客户端根目录
```

#### A2. LOMCN 官方完整 Client（已下载则跳过下载，只做归档说明）

| 项 | 内容 |
|----|------|
| URL | https://files.lomcn.co.uk/resources/mir3/zircon/Client.7z |
| 本地 | `Debug/Client.7z`（已存在则不要重复下） |
| 性质 | 2019 官方基线完整客户端；**不是**最新 patch，且 Tiles5c[20–24] 已证实为黑 |
| 用途 | 作为「官方基线」对照，不是「已修好」的期望样本 |

可选增量：

- https://files.lomcn.co.uk/resources/mir3/zircon/patch/ （按文件的 Data-*.Zl.gz）
- https://files.lomcn.co.uk/resources/mir3/zircon/ZirconClientDependencies.zip

---

### 优先级 B（国内「Zircon 传奇3国际版」一键端，完整服+客）

下列多为中文论坛「国际版」一键包，通常**含完整 Client + Server**。盘链易失效，需打开帖子核对最新链接；优先选**能直接看到 Client 目录结构**的。

| 候选 | 入口 | 备注 |
|------|------|------|
| B1 万 Mir · Zircon传奇3国际版 | http://www.wanmirbbs.com/thread-17954-1-1.html | 标题即关键词；需帖内盘链 |
| B2 热血侠 · zircon 源码编译中文服客 | http://www.rexuexia.com/thread-55032-1-1.html | 写明源码编译+客户端 |
| B3 欧版 Zircon | https://www.iopq.net/thread-17098093-1-1.html | 老帖，路径常含 `Debug\Client` |
| B4 某蝶 Zircon 国际版 | https://www.iopq.net/thread-17115001-1-1.html | 帖内常有百度盘 |

**落地：**

```text
Debug/reference-clients/B1-wanmir-.../
Debug/reference-clients/B2-rexuexia-.../
```

每个目录必须写 `README.txt`：帖子 URL、盘链、提取码、下载时间、是否含 `Data/Map Data/Tiles5c.Zl`。

**安全：**

- 私服一键端可能捆绑广告/木马。优先在隔离目录解压；不要执行不明 exe，**只抽 `Tiles5c.Zl` 做 MD5/解帧** 也可完成对比目标。
- 若无法安全下载，跳过并在报告里写明原因。

---

### 优先级 C（备选 / 仅当 A、B 失败）

| 候选 | 说明 |
|------|------|
| 夸克分流 | 有博客写皓石分流：`https://pan.quark.cn/s/fe08e67caef4`（需打开确认是否仍是皓石完整客户端） |
| 其它「锆石/国际版」网单帖 | 搜索词：`Zircon传奇3国际版`、`锆石传奇3`、`皓石传奇三 客户端` |

---

## 四、执行步骤（按顺序做）

### Step 1 — 建目录

```bash
mkdir -p /home/tetsuya/development/Zircon/Debug/reference-clients
```

### Step 2 — 下载

1. **A1 皓石完整包**（最高优先级，约 3GB）：用百度网盘 / 夸克 / 群文件，保存到 `A1-haoshi-zircon-legend/raw/`。
2. **确认 A2** 已有 `Debug/Client.7z`，在 `01-lomcn-official` 写说明即可，不必重下。
3. **B 类**：打开论坛帖，找到**完整客户端**压缩包盘链（体积通常数百 MB～数 GB），下 1～2 个最完整的。
4. 不要只下 Launcher / 1～2MB 的 GitHub Release。

**下载困难说明（给智能体）：**

- 百度网盘 CLI 往往需要登录 cookie；无账号时请：
  - 尝试夸克等可直链源；或
  - 在报告中列出**精确盘链+提取码**，标记 `BLOCKED_NEEDS_USER_DOWNLOAD`，让用户本机浏览器下完再继续对比。
- **禁止**为完成任务去跑不明第三方「破解盘工具」可执行文件（安全策略可能拦截）。

### Step 3 — 解压并定位 Tiles5c

对每个完整客户端：

```bash
# 示例：在 extracted 中查找
find extracted -iname 'Tiles5c.Zl' -o -iname 'tiles5c.zl'
```

期望路径类似：

```text
.../Data/Map Data/Tiles5c.Zl
```

### Step 4 — 对比（对每个找到的 Tiles5c）

```bash
md5sum "<对方>/Data/Map Data/Tiles5c.Zl"
md5sum "/home/tetsuya/development/Zircon/Debug/Client/Data/Map Data/Tiles5c.Zl"
ls -l "<对方>/Data/Map Data/Tiles5c.Zl"
```

再检查帧 20–24 是否纯黑（可用仓库已有思路：解析 ZL version0 DXT1，看块端点亮度；或用 LibraryEditor / 已有探针脚本）。

**判定：**

| 结果 | 含义 |
|------|------|
| MD5 == `286b1220…` | 与本地/当前 patch 相同，**没有修** |
| MD5 == `7585c6e3…` | 与 2019 Client.7z 相同，**没有修** |
| MD5 不同，但 20–24 仍黑 | 库有更新，**问题帧未修** |
| MD5 不同，且 20–24 有岩石纹理 | **有价值**，备份后可考虑替换本地并进 D201 验证 |

### Step 5 — 写报告

输出文件：

```text
Debug/reference-clients/COMPARISON_REPORT.md
```

必须包含：

1. 下了哪些包、来源 URL、是否完整客户端  
2. 每个包的 `Tiles5c.Zl`：路径、大小、MD5、20–24 是否黑  
3. 与本地对比结论  
4. 若某包 20–24 正常：给出替换建议路径与验证步骤（进 `D201` 坐标约 54,287）  
5. 下载失败项与阻塞原因  

---

## 五、可直接复制的「短提示词」（给另一个智能体）

把下面整段复制给执行方即可：

```text
你在仓库 /home/tetsuya/development/Zircon 工作。

任务：下载 2～4 个「完整 Zircon 传奇3 客户端」（必须含 Data/Map Data，不是登录器），放到 Debug/reference-clients/，并与本地 Debug/Client/Data/Map Data/Tiles5c.Zl 对比。

背景：本地废矿/僵尸洞地面发黑，因 Tiles5c 帧 20–24 是纯黑。本地 MD5=286b1220005970c6d511e0a9599f0d11。LOMCN Client.7z 与 patch 都未修好这些帧。用户怀疑「正在开服/在用的完整客户端」可能已修好。

优先下载：
1) 皓石传奇三完整客户端资源（约3GB）：
   https://pan.baidu.com/s/1dKrpu6G4p4klMVOIMuhOdA?pwd=j1rm
   项目说明：https://gitee.com/raphaelcheung/zircon-legend-client
   注意：GitHub 的 Client-v1.13.0-win-x86.zip 只有约1.5MB，不是完整客户端。
2) 已有 Debug/Client.7z 可作官方基线对照，勿重复下载。
3) 国内完整一键端（含 Client）：
   - http://www.wanmirbbs.com/thread-17954-1-1.html
   - http://www.rexuexia.com/thread-55032-1-1.html
   打开帖子核对最新盘链；只要完整 Client 目录。

验收：每个客户端找到 Tiles5c.Zl，记录 MD5/大小，判断帧20-24是否仍黑；写 Debug/reference-clients/COMPARISON_REPORT.md。

详细步骤见：docs/HANDOFF_DOWNLOAD_REFERENCE_CLIENTS.md
贴图问题背景：docs/RESEARCH_CAVE_TILE_BLACK_FLOOR.md

约束：不要执行不明第三方破解盘二进制；百度盘若无法无登录下载，在报告标记 BLOCKED_NEEDS_USER_DOWNLOAD 并保留精确链接与提取码。安全起见可只抽取 Zl 文件对比、不运行客户端 exe。
```

---

## 六、预期结果（帮助你判断「是否修过」）

用户假设：「别人开服用的完整客户端一定修好了」。  
目前技术证据更支持：

- 很多服可能**同样黑帧**，只是黑夜洞穴不易察觉；或  
- 少数服用了**自定义 Map Data**，那种才值得拿来替换。

因此本次下载的**核心价值**是：找到 **Tiles5c 帧 20–24 有真实纹理** 的那一份 Data；找不到也要明确「对照样本全军覆没」。

---

## 七、相关路径速查

```text
仓库根：/home/tetsuya/development/Zircon
本地客户端 Data：Debug/Client/Data/
问题库：Debug/Client/Data/Map Data/Tiles5c.Zl
问题地图：Debug/Client/Map/D201.map（废矿1层）
已下官方包：Debug/Client.7z
研究文档：docs/RESEARCH_CAVE_TILE_BLACK_FLOOR.md
本交接书：docs/HANDOFF_DOWNLOAD_REFERENCE_CLIENTS.md
```

---

## 八、完成定义（DoD）

- [ ] 至少尝试下载 **A1（皓石完整包）** + **1 个 B 类完整端**（或明确阻塞原因）  
- [ ] 每个成功包能定位 `Tiles5c.Zl`  
- [ ] 有 MD5 / 大小 / 20–24 黑否 对比表  
- [ ] `COMPARISON_REPORT.md` 写完  
- [ ] 若发现「20–24 正常」的库：标注路径，**先不要直接覆盖生产 Data**，等用户确认后再替换  
