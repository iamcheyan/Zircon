# 数据库层（MirDB 引擎 / System.db / Users.db / 存档调度）

## TL;DR 速查表

- Zircon 没有外部 SQL 数据库：**自研文件型 ORM `MirDB`**，两个库文件 `./Database/System.db`（策划配置）+ `./Database/Users.db`（玩家数据），自定义二进制格式（可选 AES 加密）。
- 表扫描靠**反射**：`Session.Initialize` 扫描传入程序集中所有 `DBObject` 子类建 `DBCollection<T>`（LibraryCore/MirDB/Session.cs:106-111）；`[UserObject]` 特性决定进 Users.db，否则进 System.db（DBCollection.cs:30-35）。
- 列扫描也是反射：`DBMapping` 收集公开属性中「DBValue.TypeList 支持类型 / 枚举 / DBObject 子类」且无 `[IgnoreProperty]` 的（DBMapping.cs:21-29）；**枚举按底层类型存 int，DBObject 引用存对方 `Index`（int 外键）**（DBValue.cs:215-234）。
- 保存是**整库重写**：先写 `.TMP`，再把旧库 gzip 备份到 `Backup/`，最后 `File.Move` 替换（Session.cs:274-351）；服务端定时由 `Config.DBSaveDelay`（默认 5 分钟）驱动（SEnvir.cs:1376,1482-1490）。
- System.db 的「版本」是 `yyyy.MM.dd.count` 字符串，存在库内 `SystemDatabaseInfo` 行（Name="System"）里，每次系统库内容变更 +1（Session.cs:21,526-540）。
- **双 Session 架构**：管理端 `SMain` 用 `SessionMode.System`（只读写 System.db），游戏服 `SEnvir` 用 `SessionMode.Users`（读写 Users.db、只读 System.db）；两个进程可同时开。
- System.db 模型 77 个（LibraryCore/SystemModels/，40 文件，无 `[UserObject]`）；Users.db 模型 35 个（ServerLibrary/DBModels/，33 文件，全部 `[UserObject]`）。
- 掉落链：`MonsterInfo →(Drops)→ DropInfo →(Item)→ ItemInfo`，DropInfo 双外键 `[IsIdentity]`（DropInfo.cs:9,26）。
- 玩家链：`AccountInfo →(Characters)→ CharacterInfo →(Items/Magics/Quests/…)→ UserItem/UserMagic/UserQuest → …`。
- Godot 客户端已移植 System.db 只读加载（GodotClient/Network/DatabaseLoader.cs），并经 ProjectReference 直接复用 LibraryCore 的 MirDB 引擎（GodotClient/ZirconClient.csproj:12）。

## 职责概述

MirDB 是一个嵌入式的、单文件-per-库的序列化 ORM：

1. **建表**：启动时反射程序集，把每个 `DBObject` 子类注册成一张「表」（`DBCollection<T>`），属性即列（`DBMapping`/`DBValue`）。
2. **建库**：首次保存时若文件不存在则创建（`.TMP` → move）；库文件头部持久化全表 schema（mapping），用于加载时做新旧列对齐与类型迁移。
3. **关系**：`[Association(Identity, Aggregate)]` 声明双向 1:N（父 `DBBindingList<T>` ↔ 子标量属性）；无 Association 的模型类型属性是「裸外键」。
4. **脏检查**：模型属性 setter 调 `OnChanged` 置 `IsModified`（DBObject.cs:315-330），保存时只重序列化脏对象（DBCollection.cs:131-148），但**文件级别仍是全量重写**。
5. **调度**：游戏服主循环按 `DBSaveDelay` 周期调用 `SEnvir.Save()`（序列化在主循环线程、落盘在后台线程）；关服/关管理端时再强制 Save 一次。

System.db 由 `Server/`（SMain 管理端，48 个 DevExpress View 窗口）编辑；Users.db 由游戏服运行时读写。`LibraryEditor/` 是**贴图库（.lib/WTL/Wemade）编辑器，与数据库无关**——DB 编辑视图实际全部在 `Server/Views/`。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/MirDB/Session.cs | 15-604 | 会话与库生命周期：Initialize 反射建表、加载 System/Users、Save/Commit 落盘与 gzip 备份、版本号管理、级联删除 |
| LibraryCore/MirDB/DBObject.cs | 11-332 | 所有持久化模型基类：Index/脏标记/RawData 序列化/OnChanged 双向链接维护/Delete 级联 |
| LibraryCore/MirDB/ADBCollection.cs | 7-35 | 集合抽象基类（Mapping/IsSystemData/ReadOnly/HasChanges/Load/SaveObjects 等抽象契约） |
| LibraryCore/MirDB/DBCollection.cs | 10-260 | 泛型集合实现：Binding 列表、索引分配、加载、脏检查、序列化输出、删除 |
| LibraryCore/MirDB/DBMapping.cs | 9-70 | 表结构（列清单）的反射构建 / 二进制读写 / IsMatch 版本比对 |
| LibraryCore/MirDB/DBValue.cs | 11-257 | 单列编解码：24 种内置类型 + 枚举 + DBObject 外键的读写函数表 |
| LibraryCore/MirDB/Attributes.cs | 6-51 | SessionMode / UserObjectAttribute / IgnorePropertyAttribute / AssociationAttribute / IsIdentityAttribute |
| LibraryCore/MirDB/DBBindingList.cs | 7-104 | 子表集合：Insert/Remove 时自动维护父引用与双向 Association |
| LibraryCore/MirDB/DBRelationship.cs | 9-45 | 加载期外键→对象解析的暂存队列（并发安全），`ConsumeKeys` 完成二次装配 |
| LibraryCore/Encryption.cs | 39-92 | `GetReader/GetWriter` 流包装（未设密钥=明文 BinaryWriter，设密钥=加密流），`SetKey` |
| Server/SMain.cs | 79-152, 297-407 | 管理端：System 模式 Session、View 打开、插行后 Users.db 引用重排 |
| ServerLibrary/Envir/SEnvir.cs | 436-519, 1373-1643, 1794-1814 | 游戏服：Users 模式 Session、集合句柄、定时/关服存档 |
| Server/Views/（48 个 View） | — | System.db/Users.db 的编辑 UI（见 §「DB 编辑器」） |

## 核心流程

### 1. 初始化与表扫描（Session.Initialize）

```csharp
// LibraryCore/MirDB/Session.cs:90-124
public void Initialize(params Assembly[] assemblies)
{
    Assemblies = assemblies;

    if (!Directory.Exists(Root))
        Directory.CreateDirectory(Root);

    Collections = new Dictionary<Type, ADBCollection>();

    List<Type> types = assemblies
        .Select(x => x.GetTypes())
        .SelectMany(x => x)
        .ToList();

    Type collectionType = typeof(DBCollection<>);

    foreach (Type type in types)
    {
        if (!type.IsSubclassOf(typeof(DBObject))) continue;

        Collections[type] = (ADBCollection)Activator.CreateInstance(collectionType.MakeGenericType(type), this);
    }

    InitializeSystem();

    if ((Mode & SessionMode.Users) == SessionMode.Users)
        InitializeUsers();

    Parallel.ForEach(Relationships, x => x.Value.ConsumeKeys(this));

    Relationships = null;

    foreach (KeyValuePair<Type, ADBCollection> pair in Collections)
        pair.Value.OnLoaded();
}
```

要点：库目录不存在就创建（首启自动建目录）；所有 `DBObject` 子类都会建表，无特性过滤；**外键列在 Load 时只读出 int，塞进 `DBRelationship.LinkTargets` 队列，等全部表加载完后 `ConsumeKeys` 统一换成真实对象引用**（DBObject.cs:69-95, DBRelationship.cs:19-39）——这就是外键前向引用（子表先于父表加载）能成立的原因。

哪个库归属哪张表：

```csharp
// LibraryCore/MirDB/DBCollection.cs:30-35
IsSystemData = Type.GetCustomAttribute<UserObjectAttribute>() == null;
RaisePropertyChanges = IsSystemData;
...
ReadOnly = IsSystemData ? (Session.Mode & SessionMode.System) != SessionMode.System
                        : (Session.Mode & SessionMode.Users) != SessionMode.Users;
```

`[UserObject]` → Users.db；其余 → System.db。ReadOnly 决定该会话能否写这张表：`SessionMode.Users`（游戏服）对系统表只读，`SessionMode.System`（管理端）对用户表只读。`SessionMode` 是 Flags（Attributes.cs:6-12），`DatabaseEncryptionForm` 用 `SessionMode.Both` 同时开两库（Server/Views/DatabaseEncryptionForm.cs:45-48）。

### 2. 加载（InitializeSystem / InitializeUsers，两库逻辑同构）

```csharp
// LibraryCore/MirDB/Session.cs:151-175（System 侧）
SystemDatabaseExists = File.Exists(SystemPath);

if (!SystemDatabaseExists) return;

using (BinaryReader reader = Library.Encryption.GetReader(File.OpenRead(SystemPath)))
{
    int count = reader.ReadInt32();

    for (int i = 0; i < count; i++)
        mappings.Add(new DBMapping(Assemblies, reader));

    List<Task> loadingTasks = new List<Task>();
    foreach (DBMapping mapping in mappings)
    {
        byte[] data = reader.ReadBytes(reader.ReadInt32());

        ADBCollection value;
        if (mapping.Type == null || !Collections.TryGetValue(mapping.Type, out value)) continue;

        loadingTasks.Add(Task.Run(() => value.Load(data, mapping)));
    }

    if (loadingTasks.Count > 0)
        Task.WaitAll(loadingTasks.ToArray());
}
```

文件不存在直接返回（空库，等首次 Save 生成）。每张表**并行**加载；`mapping.Type == null`（程序集里已删除的类）静默跳过——即删模型不炸库。类型名解析失败时 DBMapping 还有一个 `Server.DBModels → Library.SystemModels` 的命名空间 fallback（DBMapping.cs:38-44）。

表内加载与**列版本对齐**：

```csharp
// LibraryCore/MirDB/DBCollection.cs:96-117
internal override void Load(byte[] data, DBMapping mapping)
{
    VersionValid = mapping.IsMatch(Mapping);
    ...
    Index = reader.ReadInt32();          // 该表已分配的最大 Index
    int count = reader.ReadInt32();
    for (int i = 0; i < count; i++)
    {
        T ob = new T { Collection = this };
        Binding.Add(ob);
        ob.RawData = reader.ReadBytes(reader.ReadInt32());
        ob.Load(mapping);
    }
}
```

`VersionValid = mapping.IsMatch(Mapping)`：文件里的列清单 vs 当前代码的列清单逐列比对（DBMapping.cs:61-69，`DBValue.IsMatch` 比较列名 + 类型，DBValue.cs:252-255）。**不一致 → 整表视为脏，下次 Save 全表重序列化**（DBCollection.cs:118-148 里 `!VersionValid || ob.IsModified` 就 Save）——这就是 MirDB 的「版本迁移」：新增/删除/改名属性后首次保存自动按新 schema 重写全表；`DBObject.Load` 里逐列 `Convert.ChangeType` 兜底类型微调（DBObject.cs:112-116），旧列多余数据被丢弃、新列保持默认值。

### 3. 保存（Session.Save → Commit → SaveSystem/SaveUsers）

```csharp
// LibraryCore/MirDB/Session.cs:232-254
public void Save(bool commit)
{
    bool systemChanged = HasSystemChanges();
    bool usersChanged = HasUserChanges();
    bool versionAlreadyPending = SystemVersionPending;
    SystemVersionPending |= systemChanged;
    UsersChangesPending |= usersChanged;

    if ((Mode & SessionMode.System) == SessionMode.System && ((systemChanged && !versionAlreadyPending) || string.IsNullOrWhiteSpace(SystemDatabaseVersion)))
    {
        BumpSystemVersion();
        SystemVersionPending = true;
    }

    Parallel.ForEach(Collections, x =>
    {
        if (x.Value.IsSystemData ? SystemVersionPending : UsersChangesPending)
            x.Value.SaveObjects();
    });

    if (commit)
        Commit(SystemVersionPending, UsersChangesPending);
}
```

`Save(false)` 只做内存序列化（各对象 `RawData`），`Commit` 才落盘——游戏服利用这个拆分把序列化放主循环、把磁盘 IO 放后台线程（见 §存档调度）。系统库有变更时先 `BumpSystemVersion()`（版本号 +1，当天首 bump 为 `yyyy.MM.dd.1`，同天再 bump count 递增；Session.cs:526-540）。

落盘与备份（两库同构，以 System 为例）：

```csharp
// LibraryCore/MirDB/Session.cs:281-311
using (BinaryWriter writer = Library.Encryption.GetWriter(File.Create(SystemPath + TempExtension)))
{
    writer.Write(SystemHeader);

    foreach (KeyValuePair<Type, ADBCollection> pair in Collections)
    {
        if (!pair.Value.IsSystemData) continue;
        byte[] data = pair.Value.GetSaveData();

        writer.Write(data.Length);
        writer.Write(data);
    }
}

if (BackUp && !Directory.Exists(SystemBackupPath))
    Directory.CreateDirectory(SystemBackupPath);

if (File.Exists(SystemPath))
{
    if (BackUp)
    {
        using (FileStream sourceStream = File.OpenRead(SystemPath))
        using (FileStream destStream = File.Create(SystemBackupPath + "System " + ToBackUpFileName(DateTime.UtcNow) + Extension + CompressExtension))
        using (GZipStream compress = new GZipStream(destStream, CompressionMode.Compress))
            sourceStream.CopyTo(compress);
    }

    File.Delete(SystemPath);
}

File.Move(SystemPath + TempExtension, SystemPath);
```

- 先写 `System.db.TMP`（头 + 每表 [长度][数据]），成功后旧库 gzip 成 `Backup/System/System 2026-08-14 15-00.db.gz`，再原子替换。**压缩只用于备份，库文件本体不压缩**（`.gz` 后缀在备份名上，Session.cs:19）。
- 备份文件名粒度由 `BackUpDelay`（分钟）取整：`BackUpDelay=0` 时每分钟一个文件（关服前会置 0，SEnvir.cs:1637-1638、SMain.cs:144），运行中 60 分钟一个（ToBackUpFileName，Session.cs:542-550）。
- 表数据格式：`[Index 已分配最大索引][count][每对象：长度 + RawData]`（DBCollection.cs:150-171）；`IsTemporary` 的对象跳过不写（SaveObjects，DBCollection.cs:137-144）。

### 4. 对象生命周期与关系维护

- **创建**：`CreateNewObject()` → `Index = ++Index`（表内自增主键）、`OnCreated()`（IsModified=true）、加入 Binding（DBCollection.cs:52-71）。`Session.InsertObjectAfter` 支持在中间插行并整体后移 Index（管理端「按策划顺序插行」用，Session.cs:370-414），随后 `MarkReferencesModified` 把所有引用了被移行对象的行标脏（Session.cs:416-434）。
- **修改**：模型属性 setter 尾调 `OnChanged(old, new, name)` → 置 `IsModified`；若新旧值是 DBObject 则同步维护双向 Association 链接（DBObject.cs:315-330）。`DBBindingList.InsertItem/RemoveItem` 也做同样的事（DBBindingList.cs:30-45）。
- **序列化**：`DBObject.Save()` 把每列写进复用的 MemoryStream，DBObject 引用列写 `linkOb?.Index ?? 0`（DBObject.cs:120-148）。
- **删除**：`Delete()` 走 `Session.Delete`：从集合移除后，反射所有属性——带 `[Association(aggregate:true)]` 的子对象/子列表**级联删除**，否则只断开引用置 null/清空（Session.cs:552-595）。`FastDelete()` 仅标记 `IsTemporary = true`，保存时直接不写（Session.cs:596-603）。

### 5. 游戏服存档调度（SEnvir）

**定时触发**（主循环每秒节拍内检查）：

```csharp
// ServerLibrary/Envir/SEnvir.cs:1373-1376 + 1480-1490
public static void EnvirLoop()
{
    Now = Time.Now;
    DateTime DBTime = Now + Config.DBSaveDelay;
    ...
    if (Now >= nextCount)
    {
        if (Now >= DBTime && !Saving)
        {
            DBTime = Time.Now + Config.DBSaveDelay;
            saveTime = Time.Now;

            Save();

            SaveDelay = (Time.Now - saveTime).Ticks / TimeSpan.TicksPerMillisecond;
        }
```

**Save 的双线程结构**（序列化在调用线程，落盘在后台线程，`Saving` 标志防重入）：

```csharp
// ServerLibrary/Envir/SEnvir.cs:1794-1814
private static void Save()
{
    if (Session == null) return;

    Saving = true;
    Session.Save(false);          // 序列化到内存 RawData

    WebServer.Save();             // PayPal 流水并入 HandledPayments

    Thread saveThread = new Thread(CommitChanges) { IsBackground = true };
    saveThread.Start(Session);    // 磁盘 IO 异步
}
private static void CommitChanges(object data)
{
    Session session = (Session)data;
    session?.Commit();            // 写 .TMP / 备份 / 替换

    WebServer.CommitChanges(data);

    Saving = false;
}
```

**关服触发**（主循环退出后）：

```csharp
// ServerLibrary/Envir/SEnvir.cs:1633-1642
WebServer.StopWebServer();
StopNetwork();

while (Saving) Thread.Sleep(1);       // 等在途存档完成
if (Session != null)
    Session.BackUpDelay = 0;          // 关服这次保存不再做 60 分钟取整备份
Save();
while (Saving) Thread.Sleep(1);

StopEnvir();
```

主循环异常（catch 块）会把 `Session = null` 再断线所有连接（SEnvir.cs:1616-1629），此后 `Save()` 直接 return——即崩溃退出**不做**落盘（依赖上一次周期存档 + 备份恢复）。

**管理端触发**：每个 View 的保存按钮都直接 `SMain.Session.Save(true)`（同步完整保存，如 Server/Views/ItemInfoView.cs:36、CurrencyInfoView.cs:219/225）；SMain 关窗时 `Session.BackUpDelay = 0; Session?.Save(true);`（SMain.cs:138-145）。

**账号/角色的写入时机**：没有逐对象落库——注册账号 `AccountInfoList.CreateNewObject()`（SEnvir.cs:3516-3519）、建角色 `CharacterInfoList.CreateNewObject()`（SEnvir.cs:3949-3952）只是内存建对象置脏，**真正的磁盘写入全部等下一次周期 Save**（默认 5 分钟）或关服 Save。游戏内一切数据（物品/BUFF/任务进度等）同理，靠属性 setter 的脏标记累积。

### 6. 首次启动 / 自动建库流程

1. `Session` 构造：`Root` 目录不存在则 `Directory.CreateDirectory`（Session.cs:94-95）。路径默认 `.\Database\`、备份 `.\Backup\`（Session.cs:82-88），`NormalizePath`/`ResolvePath` 把 Windows 反斜杠转平台分隔符（Session.cs:25-28，Linux 可用）。
2. `Initialize`：反射建全部集合；`SystemDatabaseExists = File.Exists(SystemPath)`（Session.cs:151）——文件不存在就得到一个**空内存库**。
3. System 模式下若库存在但无版本号，`SetSystemVersion(GetNextSystemVersion(...))` 补一个 `yyyy.MM.dd.1` 并置 `SystemVersionPending`（Session.cs:177-183）。
4. 首次 `Save(true)`：`HasSystemChanges()` 对空集合——新 `CreateNewObject` 的对象 `IsModified=true`；甚至版本 pending 也会触发 `SaveObjects`；随后写 `.TMP` → 不存在旧文件则跳过备份 → move 成正式库。**新库由此生成**。
5. System.db 的种子数据：管理端启动时 `CurrencyInfoView.AddDefaultCurrencies()` 自动补默认货币（Gold 物品 + CurrencyInfo + 图标分段，Server/Views/CurrencyInfoView.cs:30-93）；其余表全部由 SMain 的 View 手工/导入填写。游戏服首启时 `SEnvir.LoadDatabase()` 只读 System.db（`SessionMode.Users` 模式下系统表 ReadOnly），Users.db 不存在则空库起步，第一个周期 Save 生成它。

## 数据结构 / 协议细节

### 文件布局

```
./Database/System.db        系统库（管理端写、服务端/客户端只读）
./Database/Users.db         用户库（服务端写）
./Database/*.TMP            保存过程的临时文件（move 前的整库新文件）
./Backup/System/System 2026-08-14 15-00.db.gz     每小时的 gzip 备份
./Backup/Users/Users 2026-08-14 15-00.db.gz
```

（常量见 Session.cs:17-20；备份路径 Session.cs:37-44。）

### 库文件二进制格式

```
[Header]
  int32                表数量 N
  N × DBMapping:       string 类型全名（AssemblyQualifiedName 级 FullName）
                       int32 列数
                       列 × (string 列名, string 类型全名)
[每张表]
  int32 长度 L，随后 L 字节表数据:
    int32 Index（该表已分配的最大主键）
    int32 对象数
    对象 × (int32 长度, 字节流 RawData)
```

Header 生成于 `InitializeSystem/InitializeUsers`（Session.cs:138-146,196-204）按当前代码 schema 写内存；`DBMapping.Save`（DBMapping.cs:51-59）、`DBValue.Save`（DBValue.cs:236-240）负责列描述序列化。整个流经 `Library.Encryption.GetWriter/GetReader` 包装（Encryption.cs:39-92）：未 `SetKey` 时是裸 BinaryWriter；`Config.EncryptionEnabled=true` 时 SMain 在启动时 `Encryption.SetKey(SEnvir.CryptoKey)`（SMain.cs:83-95）。**加密是库文件级别的**，备份 gzip 外层再压一层。

### 支持的列类型（DBValue.TypeList，DBValue.cs:20-46）

`Boolean, Byte, Byte[], Char, Color(ARGB int), DateTime(ToBinary long), Decimal, Double, Int16, Int32, Int32[]（前缀 bool hasValue）, Int64, Point(x,y 两个 int), SByte, Single, Size, String(BinaryWriter.ReadString 7bit 长度前缀), TimeSpan(ticks), UInt16, UInt32, UInt64, Point[]（同 Int32[] 风格）, Stats（bool + 自写格式）, BitArray（bool + len + bytes）`。另有两类特殊列：**枚举** → 按底层类型（通常 int32）读写；**DBObject 子类** → 读写 int（对方 Index，0=null）（DBValue.cs:215-234，写侧 DBObject.cs:136-140）。

### 特性语义（Attributes.cs）

| 特性 | 目标 | 语义 |
|---|---|---|
| `[UserObject]`（:14-18） | class | 表进 Users.db；无 → System.db |
| `[IgnoreProperty]`（:20-24） | property | 不建列（运行时/计算属性，如 AccountInfo.Gold） |
| `[Association(identity)] / [Association(identity, aggregate)]`（:26-45） | property | 与另一侧同 Identity 属性配对成双向链接；`aggregate=true` 时父删子删 |
| `[IsIdentity]`（:47-51） | property | 业务键标记（仅文档/UI 层面使用，引擎不强制唯一性） |

配对机制：子侧标量属性 setter → `DBObject.CreateLink` 在目标类型上找「同 Identity 且类型为我（或 `DBBindingList<我>`）」的属性，反向赋值/加列表（DBObject.cs:197-251）；父侧集合 `DBBindingList.CreateLink` 对称（DBBindingList.cs:47-75）。找不到配对会直接抛 `Unable to find Association ...`。

### 双库引用（跨库外键）

Users.db 模型大量引用 System.db 模型（如 `UserItem.Info → ItemInfo`）。两个库各自保存时，这类列写的就是 System.db 里的 `Index`。因此 **System.db 中途插行会使所有 Users.db 外键错位**——管理端 `SMain.InsertObjectAfter` 在系统表插行后会反射找出「被用户模型引用的系统类型」，临时开一个 Users 会话把用户库里对齐重排（SMain.cs:306-367，`GetUserDatabaseReferenceTypes` 枚举所有 `[UserObject]` 类的模型类型属性，SMain.cs:377-404）。**不要手改 System.db 的 Index 顺序。**

## System.db 模型全集（LibraryCore/SystemModels/，40 文件 / 79 类）

77 个持久化类（均 `: DBObject`，全目录无 `[UserObject]`）+ 2 个运行时类（`CurrentQuest`、`EventLog`，不落库）。记号：`⟨ID⟩`=[IsIdentity]；`A:X`=Association("X")；`+agg`=第二参 true（聚合）；「裸」=模型类型属性但无 Association（仅存外键）。

### 内容核心

| 类名 | 文件:声明行 | 用途 | 关键引用 |
|---|---|---|---|
| ItemInfo | ItemInfo.cs:7 | 物品模板（类型/职业/Shape/图片/耐久/价格/重量/堆叠/各类权限/稀有度/部件数） | Set(SetInfo) L434 A:Set；ItemStats L450 A:ItemStats+agg；Drops L454 A:Drops+agg |
| ItemInfoStat | ItemInfoStat.cs:5 | 物品附加属性行 (物品,Stat)→Amount | Item(ItemInfo) L9 ⟨ID⟩ A:ItemStats |
| MagicInfo | MagicInfo.cs:5 | 技能模板（MagicType/职业/学派/威力曲线/耗蓝/三档习得等级与经验） | 无模型引用 |
| MonsterInfo | MonsterInfo.cs:6 | 怪物模板（AI/等级/视野/经验/攻击与移动延迟/IsBoss/Flag） | MonsterInfoStats L255 +agg；Respawns L259 +agg；Drops L263 +agg；Events L267 +agg；QuestDetails L271 +agg |
| MonsterInfoStat | MonsterInfoStat.cs:5 | 怪物属性行 | Monster(MonsterInfo) L9 ⟨ID⟩ A:MonsterInfoStats |
| BaseStat | BaseStat.cs:5 | (职业,等级)→HP/MP/负重/命中/敏捷/AC/MR/DC/MC/SC 曲线 | 无 |
| WeaponCraftStatInfo | WeaponCraftStatsInfo.cs:5 | 武器锻造随机属性权重表 | 无 |
| SetInfo | SetInfo.cs:6 | 装备套装 | Items(ItemInfo) L26 A:Set；SetStats L29 A:SetStats |
| SetInfoStat | SetInfoStat.cs:5 | 套装激活属性行（四重身份：套装/Stat/职业/等级） | Set(SetInfo) L9 ⟨ID⟩ A:SetStats |
| StoreInfo | StoreInfo.cs:5 | 商城条目（金币价/HuntGold 价/上架/限时） | Item(ItemInfo) L8 ⟨ID⟩ 裸 |
| CurrencyInfo | CurrencyInfo.cs:5 | 货币定义（类型/分类/掉落物/汇率） | DropItem(ItemInfo) L68 裸；Images L99 +agg |
| CurrencyInfoImage | CurrencyInfo.cs:114 | 货币图标分段（金额→图标） | Currency(CurrencyInfo) L117 A:Images |
| BundleInfo | BundleInfo.cs:6 | 礼包/物品包（槽位 1–16） | Contents L85 A:Contents+agg |
| BundleItemInfo | BundleInfo.cs:102 | 礼包内容条目 | Bundle L105 A:Contents；Item(ItemInfo) L120 裸 |
| LootBoxInfo | LootBoxInfo.cs:5 | 夺宝箱（SlotSize=15） | Currency L23 裸；Contents L39 +agg |
| LootBoxItemInfo | LootBoxInfo.cs:55 | 夺宝箱内容条目 | LootBox L58 A:Contents；Item L73 裸 |

### 地图 / 区域 / 跳转

| 类名 | 文件:声明行 | 用途 | 关键引用 |
|---|---|---|---|
| MapInfo | MapInfo.cs:7 (partial) | 地图定义（光照/天气/PK 规则/传送许可/技能延迟/骑马/挖矿/等级限制/**地图级倍率**） | ReconnectMap(MapInfo) L258 裸；Instance(InstanceInfo) L458 A:Maps；DungeonMap(DungeonMapInfo) L476 A:DungeonMap；Guards L509 +agg；Regions L513 +agg；Mining L516 +agg；Castles L520 +agg；BuffStats L523 +agg |
| MapInfoStat | MapInfo.cs:560 | 地图 Buff 属性行 | Map L564 ⟨ID⟩ A:MapInfoStats |
| MapRegion | MapRegion.cs:9 | 区域（BitRegion/PointRegion/RegionType/几何计算），地图内容的枢纽 | Map L13 ⟨ID⟩ A:Regions；SourceMovements L29 +agg；DestinationMovements L33 +agg；NPCs L37 +agg；Respawns L41 +agg；SafeZones L45 +agg；BindSafeZones L49 +agg；QuestTasks L53 +agg |
| MovementInfo | MovementInfo.cs:5 | 区域跳转点（含 NeedItem/NeedSpawn/NeedInstance 进入条件） | SourceRegion L9 ⟨ID⟩ A:SourceMovements；DestinationRegion L26 ⟨ID⟩ A:DestinationMovements；NeedItem(ItemInfo) L56 裸；NeedSpawn(RespawnInfo) L71 裸；NeedInstance L101 裸 |
| SafeZoneInfo | SafeZoneInfo.cs:7 | 安全区（+回城绑定区） | Region L11 ⟨ID⟩ A:SafeZoneRegions；BindRegion L27 A:SafeZoneBindRegions |
| GuardInfo | GuardInfo.cs:5 | 地图守卫出生点 | Map L9 ⟨ID⟩ A:Guards；Monster(MonsterInfo) L25 ⟨ID⟩ 裸 |
| MineInfo | MineInfo.cs:6 | 挖矿点（矿石/概率/补货） | Map L10 ⟨ID⟩ A:Mining；Item L26 ⟨ID⟩ 裸；Region L56 裸 |

### 副本 / 地下城 / 城堡

| 类名 | 文件:声明行 | 用途 | 关键引用 |
|---|---|---|---|
| InstanceInfo | InstanceInfo.cs:8 | 副本类型（并发数/重进/传送许可/等级人数上下限/冷却限时） | RequiredItem L191 裸；ConnectRegion L222 裸；ReconnectRegion L237 裸；Maps L299 A:Map+agg；BuffStats L302 +agg |
| InstanceMapInfo | InstanceInfo.cs:342 | 副本-地图行（RespawnIndex 出生组） | Instance L346 ⟨ID⟩ A:Map；Map L362 ⟨ID⟩ 裸 |
| InstanceInfoStat | InstanceInfo.cs:393 | 副本 Buff 属性行 | Instance L397 ⟨ID⟩ A:InstanceInfoStats |
| DungeonInfo | DungeonInfo.cs:8 | 地下城定义（刷怪倍率/平均等级经验计算属性） | Maps L60 A:DungeonMaps+agg |
| DungeonMapInfo | DungeonInfo.cs:105 | 地下城楼层行 | Dungeon L109 ⟨ID⟩ A:DungeonMaps；Map L126 ⟨ID⟩ A:DungeonMap |
| CastleInfo | CastleInfo.cs:6 | 攻城城堡（开战时刻/时长/各区域/城堡物品与怪物） | Map L25 A:Castles+agg；CastleRegion L70 / ObjectiveRegion L85 / AttackSpawnRegion L100（MapRegion，裸）；Item(ItemInfo) L115 裸；Monster(MonsterInfo) L130 裸；Flags L161 +agg；Gates L164 +agg；Guards L167 +agg |
| CastleFlagInfo | CastleFlagInfo.cs:5 | 旗帜摆放 | Castle L9 ⟨ID⟩ A:Flags；Monster L25 ⟨ID⟩ 裸 |
| CastleGateInfo | CastleGateInfo.cs:5 | 城门摆放+维修费 | Castle L9 ⟨ID⟩ A:Gates；Monster L25 ⟨ID⟩ 裸 |
| CastleGuardInfo | CastleGuardInfo.cs:5 | 城堡守卫摆放 | Castle L9 ⟨ID⟩ A:Guards；Monster L25 ⟨ID⟩ 裸 |

### NPC 与对话脚本（NPCInfo.cs 一个文件 10 类）

| 类名 | 声明行 | 用途 | 关键引用 |
|---|---|---|---|
| NPCInfo | NPCInfo.cs:8 | NPC 定义（区域+名称联合身份/形象/货架号/入口页） | Region L12 ⟨ID⟩ A:RegionNPCs；EntryPage(NPCPage) L118 裸；StartQuests L139 / FinishQuests L143（A:StartQuests/FinishQuests）；Requirements L146 +agg |
| CurrentQuest | NPCInfo.cs:158 | 运行时任务图标状态（**非 DBObject**） | — |
| NPCPage | NPCInfo.cs:169 | 对话页（DialogType/Say/成功跳转） | SuccessPage(NPCPage) L217 裸；Currency L247 裸；Checks L263 / Actions L266 / Buttons L269 / Goods L272 / Types L275 / Values L278（均 +agg） |
| NPCGood | NPCInfo.cs:286 | 货架条目（价格倍率/多货币换算） | Page L289 A:Goods；Item L305 裸 |
| NPCType | NPCInfo.cs:445 | 页物品类型过滤 | Page L448 A:Types |
| NPCCheck | NPCInfo.cs:479 | 对话前置检查（等级/职业/金币/物品/婚姻/货币…） | Page L482 A:Checks；ItemParameter1 L572 裸；FailPage L603 裸 |
| NPCAction | NPCInfo.cs:619 | 对话动作（传送/给予/改元素/转生/名望…） | Page L622 A:Actions；ItemParameter1 L697 / MapParameter1 L712 / InstanceParameter1 L728 裸 |
| NPCButton | NPCInfo.cs:759 | 页按钮 → 目标页 | Page L762 A:Buttons；DestinationPage L793 裸 |
| NPCRequirement | NPCInfo.cs:809 | NPC 可见/交互条件 | NPC L812 A:Requirements；QuestParameter L857 裸 |
| NPCValue | NPCInfo.cs:904 | 对话自定义数据槽（DataList/DataValue/Field/RollResult） | Page L907 A:Values |

### 任务（QuestInfo.cs 5 类）

| 类名 | 声明行 | 用途 | 关键引用 |
|---|---|---|---|
| QuestInfo | QuestInfo.cs:5 | 任务（四段文本；OnCreated 自动加 HaveNotCompleted 自引用需求） | Requirements L99 +agg；StartNPC L102 A:StartQuests；FinishNPC L118 A:FinishQuests；Rewards L135 +agg；Tasks L138 +agg |
| QuestReward | QuestInfo.cs:157 | 奖励条目（可选 Choice/绑定/限时/职业） | Quest L160 A:Rewards；Item L175 裸 |
| QuestRequirement | QuestInfo.cs:275 | 接取条件（含前置任务） | Quest L278 A:Requirements；QuestParameter L323 裸 |
| QuestTask | QuestInfo.cs:355 | 目标行（杀怪/收集/区域/物品） | Quest L358 A:Tasks；ItemParameter L388 裸；RegionParameter L404 A:RegionQuestTasks；MonsterDetails L450 +agg |
| QuestTaskMonsterDetails | QuestInfo.cs:453 | 击杀明细（怪物/可限地图/概率/数量/DropSet） | Task L456 A:MonsterDetails；Monster L472 A:QuestDetails；Map L488 裸 |

### 事件系统（EventInfo.cs 14 类：世界/玩家/怪物三组同构）

每组 = `XxxEventInfo`（计数器）+ `XxxEventTrigger`（触发器）+ `XxxEventAction : BaseEventAction`（动作）+ `XxxInfoTriggerStat`（动作附加属性）。

| 类名 | 声明行 | 用途 |
|---|---|---|
| WorldEventInfo / Trigger / Action / InfoTriggerStat | EventInfo.cs:8 / 63 / 127 / 164 | 世界事件（晨/昼/暮/夜触发） |
| PlayerEventInfo / Trigger / Action / InfoTriggerStat | EventInfo.cs:219 / 289 / 413 / 450 | 玩家事件（进出/死亡/命令/计时触发；Trigger 有 Map/Region/Instance 裸参数 L352/367/382） |
| MonsterEventInfo / Trigger / Action / InfoTriggerStat | EventInfo.cs:505 / 575 / 715 / 752 | 怪物击杀事件（Trigger.Monster A:Events L609；Map/Region/Instance 裸参数 L639/654/669） |
| BaseEventAction | EventInfo.cs:807 | 动作公共基类（EventActionType + Monster/Respawn/Map/Region/Instance/Item 裸参数 L869-944） |
| EventLog | EventInfo.cs:1025 | 运行时事件状态（**非 DBObject**） |

### 宠物 / 名望 / 成就 / 杂项

| 类名 | 文件:声明行 | 用途 | 关键引用 |
|---|---|---|---|
| CompanionInfo | CompanionInfo.cs:5 | 宠物商品（MonsterInfo 为身份/价格/货币/解锁物品） | MonsterInfo L8 ⟨ID⟩ 裸；Currency L53 裸；UnlockItem L83 裸；CompanionSpeeches L99 +agg |
| CompanionLevelInfo | CompanionLevelInfo.cs:5 | 宠物等级表（经验/背包格数/负重/饥饿） | 无 |
| CompanionSkillInfo | CompanionSkillInfo.cs:5 | 宠物升级技能池权重 | 无 |
| CompanionSpeech | CompanionSpeech.cs:5 | 宠物台词 | Companion L8 A:CompanionSpeeches |
| DisciplineInfo | DisciplineInfo.cs:5 | 修行等级表 | 无 |
| FameInfo | FameInfo.cs:5 | 称号/名望 | BuffStats L84 +agg；ItemRewards L87 +agg |
| FameInfoStat / FameInfoReward | FameInfo.cs:90 / 141 | 名望属性/奖励物品行 | Fame L94 ⟨ID⟩ A:FameInfoStats；Fame L145 ⟨ID⟩ A:FameInfoRewards + Item L161 ⟨ID⟩ 裸 |
| MilestoneInfo | MilestoneInfo.cs:7 | 里程碑成就（分类/Grade/奖励物品） | Reward(ItemInfo) L115 裸；Tasks L146 A:MilestoneInfoTasks+agg |
| MilestoneInfoTask | MilestoneInfo.cs:179 | 里程碑参数行（七类身份参数槽：职业/物品/怪物/货币/区域/副本/任务/技能） | Milestone L183 ⟨ID⟩ A:MilestoneInfoTasks；Item L231 / Monster L247 / Currency L263 / Region L279 / Instance L295 / Quest L311 / Magic L327（全 ⟨ID⟩ 裸） |
| FishingInfo | FishingInfo.cs:5 | 钓鱼点 | Region L23 裸；Drops L39 +agg |
| FishingDropInfo | FishingInfo.cs:42 | 钓鱼产出（概率/质量/完美收竿） | Fishing L45 A:Drops；Item L60 裸 |
| RespawnInfo | RespawnInfo.cs:6 | 怪物出生点（双父：怪物×区域；延迟/数量/DropSet/Boss 公告） | Monster L10 ⟨ID⟩ A:Respawns；Region L27 ⟨ID⟩ A:RegionRespawns |
| DropInfo | DropInfo.cs:5 | 怪物掉落行（Chance/Amount/DropSet/PartOnly/EasterEvent） | Monster L9 ⟨ID⟩ A:Drops；Item L26 ⟨ID⟩ A:Drops |
| HelpInfo / HelpPageInfo / HelpItemInfo | HelpInfo.cs:5 / 57 / 110 | 帮助三级结构 | Pages L54 A:Pages+agg；Items L107 A:Items+agg |
| SystemDatabaseInfo | SystemDatabaseInfo.cs:5 | System.db 元信息（Name="System" + Version 版本串） | 无 |

### 关键关系链

```
掉落链:  MonsterInfo ──Drops(+agg)──▶ DropInfo ──Item(裸外键)──▶ ItemInfo
                │ MonsterInfoStats(+agg) → MonsterInfoStat      （属性明细）
                │ Respawns(+agg)   → RespawnInfo ←──RegionRespawns── MapRegion
                │ Events(+agg)     → MonsterEventTrigger
                │ QuestDetails(+agg) → QuestTaskMonsterDetails

地图链:  MapInfo ──Regions(+agg)──▶ MapRegion ──┬─ NPCs(+agg)      → NPCInfo ─▶ NPCPage(EntryPage 裸)
                                                ├─ Respawns(+agg)  → RespawnInfo
                                                ├─ SourceMovements/DestinationMovements(+agg) → MovementInfo
                                                ├─ SafeZones / BindSafeZones(+agg) → SafeZoneInfo
                                                └─ QuestTasks(+agg) → QuestTask

任务链:  QuestInfo ──Requirements/Rewards/Tasks(+agg)──▶ QuestRequirement/QuestReward/QuestTask
                │ StartNPC/FinishNPC(A:StartQuests/FinishQuests, 非聚合) ↔ NPCInfo
                └─ QuestTask ──MonsterDetails(+agg)──▶ QuestTaskMonsterDetails ──Monster──▶ MonsterInfo

副本链:  InstanceInfo ──Maps(+agg)──▶ InstanceMapInfo ──Map──▶ MapInfo
         DungeonInfo ──Maps(+agg)──▶ DungeonMapInfo ──Map/ DungeonMap──▶ MapInfo
```

## Users.db 模型全集（ServerLibrary/DBModels/，33 文件 / 35 类）

35 个类**全部** `[UserObject] + : DBObject`。

| 类名 | 文件:声明行 | 用途 | 关键引用（属性名(类型) 行号） |
|---|---|---|---|
| AccountInfo | AccountInfo.cs:12 | 账号聚合根（登录凭证/封禁/权限 Admin L447、TempAdmin L553、Observer L538/IsAdmin L696-699） | Referral(AccountInfo) L105 A:Referrals；GuildMember L402 A:Member；子表 L556-604：Currencies/Items/Referrals/Characters/Buffs/Auctions/Mail/UserDrops/Companions/CompanionUnlocks/BlockingList/BlockedByList/Payments/StoreSales/StoreFavourites/Fortunes/Quests；LastCharacter L606 裸 |
| CharacterInfo | CharacterInfo.cs:12 | 角色聚合根（外观/等级/地图位置/婚姻） | Account L15 A:Characters；CurrentMap(MapInfo) L226 / CurrentInstance L241 / BindPoint L271 裸；Companion L617；Discipline L633；Partner(CharacterInfo) L697 A:Marriage；子表 L649-679（**全部 +agg**）：Items/BeltLinks/AutoPotionLinks/Magics/Buffs/Refines/Quests/Friends/FriendedBy/MilestoneLogs/Milestones |
| UserItem | UserItem.cs:12 | 物品实例（耐久/数量/强化/过期/镶孔/附加属性；八个互斥归属 OnChanged L348-443） | Info(ItemInfo) L14；Character L170 / Account L186 / Guild L202 / Companion L218 / Refine L235 / Auction L251 / Mail L267 / Socket(UserItemSocket) L321（均 A:Items 等）；AddedStats L315 +agg；Sockets L318 +agg |
| UserItemSocket | UserItemSocket.cs:6 | 物品镶孔（宝石） | Item(UserItem) L9 A:Sockets；Gem(UserItem) L25 A:SocketGem |
| UserItemStat | UserItemStat.cs:7 | 物品附加属性行 | Item(UserItem) L10 A:AddedStats |
| UserMagic | UserMagic.cs:10 | 已学技能（等级/经验/快捷键/冷却；角色或武学双归属） | Info(MagicInfo) L12；Character L28 A:Magics；Discipline L150 A:DisciplineMagics |
| UserMilestoneLog | UserMilestone.cs:10 | 里程碑事件日志（8 类上下文引用） | Character L13 A:MilestoneLogs；Player L58/Item L73/Monster L88/Currency L103/Region L118/Instance L133/Quest L148/Magic L163 裸 |
| UserMilestone | UserMilestone.cs:193 | 已达成里程碑 | Info(MilestoneInfo) L195；Character L211 A:Milestones |
| UserQuest | UserQuest.cs:12 | 任务进度（选定奖励/追踪/时间） | QuestInfo L14；Character L30 A:Quests；Account L46 A:Quests；Tasks L142 A:Tasks+agg |
| UserQuestTask | UserQuest.cs:182 | 任务子目标进度 | Quest(UserQuest) L185 A:Tasks；Task(QuestTask) L200 裸 |
| AuctionHistoryInfo | AuctionHistoryInfo.cs:6 | 拍卖成交价格历史（按物品索引统计，Info 是 int 非引用） | 无 |
| AuctionInfo | AuctionInfo.cs:8 | 拍卖在售条目 | Account L11 A:Auctions；Item(UserItem) L27 A:Auction；Character L42 裸（展示卖家名） |
| AutoPotionLink | AutoPotionLink.cs:7 | 自动喝药配置 | Character L10 A:AutoPotionLinks |
| BlockInfo | BlockInfo.cs:7 | 黑名单（双向） | Account L10 A:BlockingList；BlockedAccount L26 A:BlockedByList |
| BuffInfo | BuffInfo.cs:8 | BUFF 实例（角色/账号互斥归属 OnChanged L165-180） | Character L11 A:Buffs；Account L27 A:Buffs |
| CharacterBeltLink | CharacterBeltLink.cs:7 | 快捷栏链接 | Character L10 A:BeltLinks |
| CompanionFilters | CompanionFilters.cs:8 | 宠物背包拾取过滤（三个字符串） | 无 |
| FriendInfo | FriendInfo.cs:7 | 好友双向关系 | Character L10 A:Friends；FriendedCharacter L26 A:FriendedBy |
| GameGoldPayment | GameGoldPayment.cs:6 | GameGold 充值流水（PayPal 报文原文） | Account L69 A:Payments |
| GameNPCList | GameNPCData.cs:6 | NPC 通用键值数据行（注意类名≠文件名） | 无 |
| GameStoreFavourite | GameStoreFavourite.cs:7 | 商城收藏 | Account L10 A:StoreFavourites；StoreInfo L23 裸 |
| GameStoreSale | GameStoreSale.cs:9 | 商城购买记录 | Item(ItemInfo) L11 裸；Account L72 A:StoreSales |
| GuildInfo | GuildInfo.cs:14 | 行会聚合根（资金/等级/公告/税收/城堡占领） | Conquest(UserConquest) L214 A:Conquest+agg；Castle(CastleInfo) L229 裸；Members L277 +agg；Items L280 +agg |
| GuildMemberInfo | GuildMemberInfo.cs:11 | 行会成员（职位/权限/贡献） | Guild L14 A:Members；Account L45 A:Member |
| GuildWarInfo | GuildWarInfo.cs:8 | 行会宣战 | Guild1 L10 / Guild2 L25（裸双向） |
| MailInfo | MailInfo.cs:10 | 邮件（附件为物品列表） | Account L13 A:Mail；Items L121 A:Mail |
| RefineInfo | RefineInfo.cs:9 | 武器精炼任务 | Character L12 A:Refines；Weapon(UserItem) L28 A:Refine |
| UserCompanion | UserCompanion.cs:9 | 账号宠物（等级/饱食/7 档成长属性） | Account L12 A:Companions；Character L28 A:Companion；Info(CompanionInfo) L43 裸；Items L226 +agg |
| UserCompanionUnlock | UserCompanionUnlock.cs:7 | 宠物图鉴解锁 | Account L10 A:CompanionUnlocks；CompanionInfo L25 裸 |
| UserConquest | UserConquest.cs:8 | 行会攻城占领记录 | Guild L11 A:Conquest；Castle(CastleInfo) L26 裸 |
| UserConquestStats | UserConquestStats.cs:8 | 攻城个人战绩快照 | Character L10 裸 |
| UserCurrency | UserCurrency.cs:8 | 账户货币余额 | Info(CurrencyInfo) L10 裸；Account L41 A:Currencies |
| UserDiscipline | UserDiscipline.cs:9 | 角色武学/心法 | Info(DisciplineInfo) L11 裸；Character L27 A:Discipline；Magics L73 A:DisciplineMagics+agg |
| UserDrop | UserDrop.cs:7 | 账号级物品爆率统计 | Account L10 A:UserDrops；Item(ItemInfo) L25 裸 |
| UserFortuneInfo | UserFortuneInfo.cs:10 | 物品掉落运势（Fortune 机制） | Account L13 A:Fortunes；Item(ItemInfo) L28 裸 |

### 玩家数据关系链

```
AccountInfo ──Characters──▶ CharacterInfo ──┬─ Items(+agg) ──▶ UserItem ──AddedStats/Sockets(+agg)──▶ UserItemStat/UserItemSocket
  │  (Accounts 侧子表无 agg：删账号不级联删)    ├─ Magics(+agg) ──▶ UserMagic ──Info──▶ MagicInfo (System)
  │                                          ├─ Quests(+agg) ──▶ UserQuest ──Tasks(+agg)──▶ UserQuestTask ──▶ QuestTask (System)
  ├─ Currencies ──▶ UserCurrency ──Info──▶ CurrencyInfo (System)
  ├─ Items(仓库) ─▶ UserItem
  ├─ Mail ────────▶ MailInfo ──Items──▶ UserItem
  ├─ Auctions ────▶ AuctionInfo ──Item──▶ UserItem
  ├─ Companions ──▶ UserCompanion ──Items(+agg)──▶ UserItem
  ├─ Buffs ───────▶ BuffInfo（角色级 Buff 则挂在 CharacterInfo.Buffs）
  ├─ Payments/StoreSales/StoreFavourites/Fortunes/UserDrops/BlockingList…
  └─ GuildMember(A:Member) ──▶ GuildMemberInfo ──Guild──▶ GuildInfo ──Members/Items(+agg)──▶ GuildMemberInfo/UserItem
```

要点：**Aggregate=true 只出现在 CharacterInfo、GuildInfo、UserItem、UserQuest、UserCompanion、UserDiscipline 的子表上**——删角色/行会/物品/任务记录会级联删子行；而 AccountInfo 侧子表无 agg，删账号只断开引用（`Session.Delete` 对非聚合引用置 null，Session.cs:577）。

## DB 编辑器：LibraryEditor 与 Server/Views 的真实分工

- **`LibraryEditor/` 与数据库无关**：目录里全是贴图库相关（LMain.cs、Mir3Library.cs、WTLLibrary.cs、WeMadeLibrary.cs、CrystalLibraryV1/V2.cs、Astc.cs 等）——编辑的是客户端 `.lib` 资源库。任务里「LibraryEditor 的表元数据」在仓库中**不存在，DB 编辑器实际位于 `Server/Views/`**。
- **`Server/Views/` 共 48 个 View 窗口**（DevExpress Grid），一表一窗：MapInfoView/MonsterInfoView/ItemInfoView/DropInfoView/RespawnInfoView/ItemInfoStatView/MonsterInfoStatView/MovementInfoView/MapRegionView/SafeZoneInfoView/NPCInfoView/NPCListView/NPCPageView/QuestInfoView/InstanceInfoView/DungeonInfoView/CastleInfoView/GuardInfoView/MineInfoView/MagicInfoView/SetInfoView/StoreInfoView/CurrencyInfoView/BundleInfoView/LootBoxInfoView/FishingInfoView/FameInfoView/DisciplineInfoView/HelpInfoView/EventInfoView/CompanionInfoView/MilestoneInfoView/WeaponCraftStatInfoView/BaseStatView/GameStoreSaleView/GameGoldPaymentView 等（System.db），加 AccountView/CharacterView/UserMailView/UserConquestStatsView/UserDropView/ChatLogView/SystemLogView/ConfigView/SyncForm/OrphanDiagnosticView/DiagnosticView/DatabaseEncryptionForm/MapViewer（Users.db 与运维）。
- View 不写任何表元数据：网格直接绑定 `Session.GetCollection<T>().Binding`（`BindingList<T>` 的 DataBinding 能力即 DBCollection 构造函数里创建 BindingList 的原因，DBCollection.cs:37-46），列由反射属性自动生成。公共行为在 `SMain.SetUpView`（多选/粘贴/删行，SMain.cs:297-304）与 `SMain.InsertObjectAfter`（插行 + Users.db 引用重排，SMain.cs:306-367）。
- 典型保存按钮：`SMain.Session.Save(true)` 同步全量落盘（例：ItemInfoView.cs:36、DropInfoView.cs:29、BaseStatView.cs:25）。
- 分发 System.db 到客户端：`ConfigView.SyncronizeLocalButton_Click` 保存后把 `System.db` 直接拷到 `Config.ClientPath\Data\`（ConfigView.cs:37-48）；远程同步走 `SyncForm` + `Config.AllowSystemDBSync`/`SyncRemotePreffix`（Web 通道）。

## GodotClient 现状

先说结论：**MirDB 引擎与 SystemModels 模型被 Godot 客户端原样复用**——`GodotClient/ZirconClient.csproj:12` 以 `<ProjectReference Include="..\LibraryCore\LibraryCore.csproj" />` 引用 LibraryCore，因此 MirDB 全部源码（Session/DBObject/…）与 77 个系统模型直接编译进客户端。逐功能状态：

| 功能 | 状态 | 依据（GodotClient 实际文件） |
|---|---|---|
| System.db 只读加载 | **已移植** | GodotClient/Network/DatabaseLoader.cs:15-70：`new Session(SessionMode.Users, root) { BackUp = false }` + `Initialize(LibraryCore)`，把 22 个集合灌进 `Globals.*`（物品/魔法/地图/货币/副本/NPCPage/怪物/钓鱼/商城/NPC/跳转/任务/QuestTask/宠物×2/修行/名望/礼包/夺宝/帮助/里程碑×2）；加载失败返回 false 有容错（:64-68） |
| System.db 路径解析 | 已移植（Godot 特有） | DatabaseLoader.cs:23-25：`ProjectSettings.GlobalizePath("res://")` 向上拼 `../Debug/Client/Data`，对应 CEnvir 的 `.\Data\`（Client/Envir/CEnvir.cs:386） |
| Users.db 加载 | **未移植**（原版客户端也不加载 Users.db，仅以 SessionMode.Users 打开同一目录读 System 表；CEnvir.cs:386-391 同款） | GodotClient/Network/ 下无任何 Users 模型引用 |
| 客户端专属表（KeyBindInfo/WindowSetting/CastleInfo） | 未移植 | 原版 CEnvir.cs:421-423 额外取 KeyBinds/WindowSettings/CastleInfoList；GodotClient 无对应调用（键位用 Godot 输入映射方案替代）[INFERENCE：未在 GodotClient 中 grep 到 KeyBindInfo/WindowSetting] |
| System.db 版本检查/自动下载 | 未移植 | CEnvir.cs:393-394 记录 `SystemDatabaseVersion/SystemDatabaseExists` 供登录比对；GodotClient DatabaseLoader.cs:32-36 仅检查存在性 |
| 写库/保存调度/备份 | 不适用（客户端只读，`BackUp=false`、系统表 ReadOnly） | DatabaseLoader.cs:29 |
| DB 编辑器（Server/Views 等价物） | 未移植 | GodotClient/ 仅 Network/Scripts/Controls/Formats/Scenes/Shaders/Translations，无任何编辑视图 |
| 加密库读取 | 部分（引擎支持，未配置密钥入口） | MirDB 经 LibraryCore 可用 `Encryption.GetReader`；GodotClient 未见 SetKey 调用（明文库可用） |

## 移植注意事项

1. **Index 即外键**：一切跨表引用（含跨库 Users→System）都是 int Index。任何「重排系统表行序」的操作必须同步修 Users.db（参考 SMain.ShiftUserDatabaseReferencesAfterInsert 的做法，SMain.cs:342-367）。Godot 侧工具如果直接改库，务必复用 `Session.InsertObjectAfter` 而非手改。
2. **整库重写无 WAL**：保存 = 新写 `.TMP` + 删旧库 + move。进程在 move 前崩溃会留下 `.TMP`（不会损坏旧库）；move 后崩溃则新库已生效。做 Godot 版管理工具时保持这个顺序，别引入增量写。
3. **双进程并发**：SMain（System 写）与 SEnvir（Users 写）可同时运行，但**都靠「文件替换」保存**——同时保存同一库会互相覆盖；Zircon 的约定是 System.db 只由 SMain 写。Godot 服务面板若整合管理功能，要保留这个单写者约束。
4. **枚举改名/删值会静默错位**：枚举按 int 存，重编号枚举 = 数据错乱；新增枚举值追加到尾部。
5. **schema 迁移语义**：改属性名/类型 → `IsMatch` 失败 → 整表重写但该列数据丢失（Load 找不到同名属性就跳过，DBObject.cs:67）。新增属性安全（旧数据无此列，保持默认）。删属性后旧列数据在下次保存时被丢弃（`DBValue.Property == null` 跳过）。
6. **`[IgnoreProperty]` 与公共字段不落库**：`AccountInfo.TempAdmin`（L553）、`CharacterInfo.Player`（L785）等运行时字段靠这一点存活——Godot 侧给模型加字段时注意，公共字段（非属性）MirDB 根本扫描不到。
7. **加载顺序与外键解析**：对象图装配靠 `DBRelationship.ConsumeKeys` 在全部表加载完后统一做；如果 Godot 版做异步/分帧加载，必须保留「全表就绪后再解析外键」的两阶段结构，否则前向引用断链。
8. **Stats/BitArray/Point 等自定义列**依赖 LibraryCore 类型本身（`Library.Stats` 有自己的 `Write(BinaryWriter)`），复用 LibraryCore 即免费获得；自研存储时这些类型要单独设计。
9. **加密**：`Config.EncryptionEnabled/EncryptionKey`（Server.ini [System]）开启后库文件不可明文读；Godot 客户端要读加密库需要实现同样的 key 注入（SMain.cs:83-95 的等价物）。
10. **备份目录会无限增长**：每小时一个 gzip（运行中 BackUpDelay=60），无清理逻辑；做运维工具时注意。
