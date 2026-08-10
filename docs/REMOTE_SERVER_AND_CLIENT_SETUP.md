# 82 远程服务端与本机客户端使用手册

本文说明如何在 82 服务器上运行 Zircon 的 ServerCore，以及如何让本机的
Godot 客户端或原 Windows 客户端连接它。

## 1. 当前部署结构

82 的正式源码仓库：

    /home/tetsuya/development/zircon

82 的编译输出和运行目录：

    /home/tetsuya/development/Debug/ServerCore

运行目录中的重要内容：

    ServerCore.dll       无头游戏服务端
    Server.ini           服务端配置
    Database/System.db   游戏系统数据库
    Database/Users.db    账号、角色等用户数据
    Map/                 地图文件
    server.log           服务端标准输出
    server-error.log     服务端错误输出

服务由 systemd 管理，服务名为 zircon-server.service。

当前网络配置：

    服务器地址：192.168.3.82
    游戏端口：7000
    用户统计端口：3000

客户端只需要连接 7000。3000 是服务端内部的用户数量监听端口。

## 2. 查看服务器状态

登录 82：

    ssh 82

查看服务状态：

    sudo systemctl status zircon-server
    sudo systemctl is-active zircon-server
    sudo systemctl is-enabled zircon-server

正常结果应该是 active 和 enabled。

查看端口：

    ss -ltnp | grep -E ':(7000|3000)\b'

查看最近日志：

    tail -50 /home/tetsuya/development/Debug/ServerCore/server.log

实时查看日志：

    tail -f /home/tetsuya/development/Debug/ServerCore/server.log

也可以通过 systemd 查看：

    sudo journalctl -u zircon-server -f

正常启动时，日志末尾应包含：

    Network Started. Listen: 0.0.0.0:7000
    Loading Time: 3 Seconds

## 3. 启动、停止和重启服务端

    sudo systemctl start zircon-server
    sudo systemctl stop zircon-server
    sudo systemctl restart zircon-server

修改 systemd 配置后：

    sudo systemctl daemon-reload
    sudo systemctl restart zircon-server

服务配置文件：

    /etc/systemd/system/zircon-server.service

当前服务具有以下特性：

- 82 开机自动启动；
- 服务异常退出后 5 秒自动重启；
- 工作目录固定为 /home/tetsuya/development/Debug/ServerCore；
- 运行用户为 tetsuya；
- 日志写入 server.log 和 server-error.log。

自动重启只能处理程序退出，不能解决服务器断电、网络中断、磁盘损坏或数据库损坏。

## 4. Server.ini 配置

配置文件：

    /home/tetsuya/development/Debug/ServerCore/Server.ini

当前有效内容：

    [Network]
    IPAddress=0.0.0.0
    Port=7000

    [System]
    CheckVersion=False
    MapPath=Map/

    [Control]
    AllowStartGame=True
    RelogDelay=00:00:02

配置说明：

| 配置 | 作用 |
|---|---|
| IPAddress=0.0.0.0 | 允许 82 的局域网地址接收客户端连接 |
| Port=7000 | 游戏 TCP 端口 |
| CheckVersion=False | 开发阶段不强制客户端 DLL 哈希一致 |
| MapPath=Map/ | 相对于运行目录的地图目录 |
| AllowStartGame=True | 允许角色进入游戏 |
| RelogDelay | 重新登录保护时间 |

不要把 MapPath 写成 Debug/ServerCore/Map/。因为服务端工作目录已经是
Debug/ServerCore，正确写法是 Map/。

修改后执行：

    sudo systemctl restart zircon-server

## 5. 服务端资源要求

服务端至少需要：

    Database/System.db
    Database/Users.db
    Map/*.map

Users.db 是运行中的用户数据，包含账号和角色。不要在服务端运行时用本机旧版
Users.db 覆盖它。

如果地图或数据库不完整，服务端可能仍然监听端口，但角色进入地图、加载 NPC
或读取系统数据时会出现错误。应检查日志中的 Map loaded 和 Network Started。

## 6. 82 上更新正式程序

正式源码更新后，在 82 执行：

    cd /home/tetsuya/development/zircon
    git pull --ff-only
    dotnet restore ServerCore/ServerCore.csproj
    dotnet build ServerCore/ServerCore.csproj -c Debug
    sudo systemctl restart zircon-server

当前 ServerCore 项目的 Debug 输出目录是：

    /home/tetsuya/development/Debug/ServerCore

更新程序时通常不需要重新复制 Database/ 和 Map/。特别是不要随意覆盖
Database/Users.db。

## 7. 本机 Godot 客户端配置

### 7.1 使用图形界面配置

启动 Godot .NET 客户端后，打开客户端设置中的网络设置，填写：

    使用网络配置：开启
    服务器地址：192.168.3.82
    服务器端口：7000

Godot 客户端只有在“使用网络配置”开启时，才会使用填写的地址；关闭时会强制
连接 127.0.0.1:7000。

设置保存后重新连接或重启客户端。

### 7.2 Godot 配置文件

Godot 客户端使用 Godot 的 user://Zircon.ini，而不是项目目录里的普通文件。
配置界面保存后会写入：

    [Network]
    UseNetworkConfig=true
    IPAddress=192.168.3.82
    Port=7000

如果客户端一直连接 127.0.0.1，优先检查 UseNetworkConfig 是否为 true。

### 7.3 启动命令

    godot-mono --path GodotClient/

或者：

    ~/.local/bin/godot-mono --path GodotClient/

## 8. 原 Windows 客户端配置

原 Windows 客户端使用运行目录中的 Zircon.ini。在 [Network] 中设置：

    [Network]
    UseNetworkConfig=True
    IPAddress=192.168.3.82
    Port=7000

关闭 UseNetworkConfig 时，原客户端会回退到默认地址 127.0.0.1:7000。
也可以从原客户端的网络设置界面修改 IP 和端口。

## 9. 连接故障排查

本机测试游戏端口：

    ncat -vz 192.168.3.82 7000

如果显示 Connected，说明网络和端口正常，继续检查客户端配置。

如果连接失败，在 82 执行：

    sudo systemctl is-active zircon-server
    ss -ltnp | grep ':7000'

能登录但无法进入游戏时，检查：

- Server.ini 中 AllowStartGame=True；
- Map/ 中是否存在对应地图；
- Database/System.db 是否存在且未被截断；
- Database/Users.db 是否可读写；
- 客户端和服务端协议版本是否匹配。

修改代码后没有变化时，确认重新编译并重启：

    cd /home/tetsuya/development/zircon
    dotnet build ServerCore/ServerCore.csproj -c Debug
    sudo systemctl restart zircon-server

不要只修改源码后直接重启，因为 systemd 运行的是
/home/tetsuya/development/Debug/ServerCore/ServerCore.dll。

## 10. 研究工具与正式服务端的区别

82 上还运行着地图查看器、WIL/UI 查看器和逆向分析任务。这些属于
Mir3-Research，不是游戏服务端：

    8765  WIL/UI 研究工具
    8899  地图查看器
    7000  正式游戏服务端

重启 zircon-server 不会重启研究工具；停止研究工具也不会停止正式游戏服务端。

## 11. 推荐的日常操作顺序

    ssh 82 'sudo systemctl is-active zircon-server'
    ncat -vz 192.168.3.82 7000
    godot-mono --path GodotClient/

启动客户端前，在网络设置中确认：

    使用网络配置：开启
    地址：192.168.3.82
    端口：7000

如果服务端代码刚更新，则先在 82 编译和重启，再启动本机客户端。
