# Mud3 老版 DAT 解码任务交接文档

## 任务目标

解析以下老服务器文件，并生成可用于“20年前 vs 当前 Zircon”内容对照的结构化数据：

- `/home/tetsuya/NAS/TMP/Mud3/Envir/stditem.dat`
- `/home/tetsuya/NAS/TMP/Mud3/Envir/magic.dat`
- `/home/tetsuya/NAS/TMP/Mud3/Envir/monster.dat`

最终需要逐条得到老版技能、装备、物品、怪物记录，并输出 JSON/CSV/Markdown，接入：

- `docs/legacy-atlas/content/catalog-skills.html`
- `docs/legacy-atlas/content/catalog-items.html`
- `docs/legacy-atlas/content/catalog-world.html`

## 已下载和分析的工具

公开工具源码已下载到：

```
/tmp/mir3-dat-decoder/
```

来源：[Wincha/Legend-Of-Mir-Dat-XML-Decryption-Encryption-Tool](https://github.com/Wincha/Legend-Of-Mir-Dat-XML-Decryption-Encryption-Tool)

真正的算法在：

```
/tmp/mir3-dat-decoder/Library/WemadeCryptLib.dll
```

已用 ILSpy 反编译到：

```
/tmp/mir3-dat-decoder-src/WemadeCrypt/WemadeCrypt.cs
```

工具界面只是调用：

```csharp
new WemadeCrypt.WemadeCrypt().DecodeBytes(bytes)
```

该 DLL 使用固定 S-box 的 8 字节分组算法，类似 Blowfish，并针对某些 Mir3 GSP/KOR 的 item.dat/Mir3Res.dat。直接用于当前 Mud3 文件时，补齐 8 字节后虽能运行，但输出仍是随机二进制，不是 XML。因此不能直接把这个 DLL 当作当前文件的解码器。

## 已确认的文件结构

### stditem.dat

- 文件大小：210316 字节。
- 前 4 字节小端数值：`0x477 = 1143`。
- 记录长度：184 字节。
- 校验：`4 + 1143 * 184 = 210316`。

### magic.dat

- 文件大小：12604 字节。
- 前 4 字节小端数值：`0x69 = 105`。
- 记录长度：120 字节。
- 校验：`4 + 105 * 120 = 12604`。

## 重要发现：Mud3 文件使用简单字节掩码

记录区存在明显的固定填充值：

- stditem 记录区大量使用 `0x04`。
- magic 记录区大量使用 `0x11`。

初步反变换：

```
stditem decoded_byte = raw_byte XOR 0x04
magic decoded_byte   = raw_byte XOR 0x11
```

对 stditem 逐条 XOR 后，第一字段出现连续的小端记录编号：

- 第 0 条 -> 0
- 第 1 条 -> 1
- 第 10 条 -> 10
- 第 500 条原始开头 `f0 05 04 04` -> XOR 后 `f4 01 00 00` -> 500

这说明它很可能不是公开工具所处理的整体 Blowfish 格式，而是“文件头 + 固定长度记录 + 每文件字节掩码/字段编码”。

## 已写入的探针代码

```
Tools/DatDecodeProbe.cs
Tools/DatDecodeProbe.csproj
```

这个程序只用于验证公开 DLL 的行为，不要把它当成最终解析器。

## 下一步实现路线

### 1. 先实现独立记录拆分器

不要覆盖 NAS 原始文件。先输出到：

```
/tmp/mud3-decoded/stditem.records.bin
/tmp/mud3-decoded/magic.records.bin
```

参数：

```
stditem: count=1143, record_size=184, xor=0x04
magic:   count=105,  record_size=120, xor=0x11
```

### 2. 做字段偏移分析

对 XOR 后每条记录：

- 输出每个偏移的十六进制。
- 按小端 BYTE/WORD/UINT 解释。
- 统计每个偏移的非零率。
- 搜索 GBK/GB18030/UTF-8 可打印文本。
- 对名称字段尝试固定长度、长度前缀、零结尾。
- 与早期 Mir3 结构体交叉验证。

参考结构体：

```
/tmp/legendofmir3-src/LegendOfMir3_Client/GameProcess/Item.h
/tmp/legendofmir3-src/LegendOfMir3_Server/Def/Protocol.h
```

### 3. 解析 magic.dat

优先确认：

- 技能编号。
- 技能名称。
- 职业/Job。
- NeedL1/NeedL2/NeedL3。
- L1Train/L2Train/L3Train。
- Power/MaxPower。
- Delay/CoolTime。
- 技能效果字段。

对照资料：

```
docs/database/views/skills.md
docs/notes/22-传奇EI2.0资料整理-地图装备技能阶段.md
docs/notes/28-技能魔法系统.md
```

### 4. 解析 stditem.dat

优先确认：

- 记录编号。
- 名称和前缀名称。
- StdMode。
- Shape。
- Weight。
- AniCount。
- Looks。
- DuraMax。
- AC/MAC/DC/MC/SC。
- Need/NeedLevel。
- Price。
- 职业限制和其他扩展字段。

早期 Mir3 的参考字段见客户端 `STANDARDITEM` 和服务器 `_TSTANDARDITEM`，但不要假设 184 字节记录就是该结构体的直接内存布局；184 字节明显包含额外字段或扩展区。

### 5. 解析 monster.dat

先按同样方式确认：

- 记录数量。
- 记录长度。
- 文件掩码。
- 名称字段。
- 等级、生命、攻击、防御、经验、掉落字段。

如果 monster.dat 的掩码或记录长度不同，必须单独测量，不要复用 stditem/magic 参数。

## 建议输出格式

生成：

```
docs/research/mud3-dat-decoded/stditem.json
docs/research/mud3-dat-decoded/magic.json
docs/research/mud3-dat-decoded/monster.json
docs/research/mud3-dat-decoded/README.md
```

每条记录至少保留：

```json
{
  "index": 123,
  "source_file": "stditem.dat",
  "record_offset": 22616,
  "record_size": 184,
  "xor_mask": 4,
  "name": null,
  "raw_fields": {},
  "confidence": "unverified",
  "notes": []
}
```

不能确认的字段保留 raw_fields，不能凭当前 Zircon 数据补写。

## 网站接入状态

现有当前版本目录生成器：

```
Tools/build_content_catalog.py
```

现有详细页面：

```
docs/legacy-atlas/content/catalog.html
docs/legacy-atlas/content/catalog-skills.html
docs/legacy-atlas/content/catalog-items.html
docs/legacy-atlas/content/catalog-maps.html
docs/legacy-atlas/content/catalog-world.html
```

解码后为老版记录增加状态：

- `old-only`：老版存在，当前未找到。
- `current-only`：当前存在，老版未找到。
- `both`：两个版本都存在。
- `changed`：两个版本都有但名称或属性变化。
- `unverified`：记录存在但字段尚未完全确认。

## 禁止事项

1. 不要覆盖 `/home/tetsuya/NAS/TMP/Mud3/Envir/` 原文件。
2. 不要把公开 DLL 的随机输出当成解码成功。
3. 不要用当前技能/装备反推老版记录。
4. 不要把当前 Dragon Lord、Sama、Odyn 等后期装备直接算进老版。
5. 不要为了网页完整而填充未经验证的字段。
6. 不要删除已有详细目录页。

## 验收标准

- 说明三个 DAT 的记录头、记录数量和记录长度。
- 有独立的 XOR/记录拆分实现。
- 至少解析出一批连续且可验证的中文名称和数值字段。
- 给出每个已确认字段的偏移和类型。
- 输出 JSON/CSV/Markdown。
- 网页显示老版逐条记录和当前逐条记录。
- 每条比较结果有明确的版本状态。

