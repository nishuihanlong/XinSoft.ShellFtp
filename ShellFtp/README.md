# ShellFtp（XinSoft · C# WinForms + SSH.NET）

集成 **SFTP 文件传输** 与 **SSH 交互式终端** 的桌面客户端，界面与代码按 WinForms 设计器模式分离。

## 基本信息

- 目标框架：**.NET 10**（`net10.0-windows`）
- 根命名空间：`XinSoft`
- 程序集 / 项目名：`ShellFtp`
- 传输与终端底层：SSH.NET（Renci.SshNet）直连，不调用外部命令行进程

## 目录结构

```
ShellFtp/
├─ ShellFtp.csproj              项目文件（net10.0-windows、SSH.NET 包引用）
├─ Program.cs                  程序入口
├─ MainForm.Designer.cs        主窗体界面：工具栏、双栏列表、状态栏、事件挂接
├─ MainForm.cs                  主窗体逻辑：连接、导航、传输队列、上传下载、拖拽、打开终端
├─ ConnectDialog.Designer.cs    连接/站点对话框界面
├─ ConnectDialog.cs             连接/站点对话框逻辑
├─ ShellForm.Designer.cs        SSH 终端界面：输出区、命令输入框
├─ ShellForm.cs                终端逻辑：建连、ANSI 渲染、尺寸自适应、复制粘贴、发送命令
├─ Models/
│  └─ SiteProfile.cs           站点配置模型
└─ Services/
   ├─ SshAuth.cs                认证方式（密码/私钥）与连接信息工厂，SFTP/终端复用
   ├─ SftpService.cs            SFTP：列目录、上传下载、删除重命名
   ├─ ShellService.cs           SSH 终端：SshClient + ShellStream，读输出/写命令/单命令执行
   ├─ TerminalEmulator.cs        网格缓冲区：ANSI SGR 颜色、光标序列解析与自适应行列
   ├─ PuttyLauncher.cs         外部 PuTTY 路径解析与进程启动
   └─ SiteManager.cs            站点配置读写（sites.json，DPAPI 加密落盘）
```

## 运行 / 编译

```powershell
dotnet build ShellFtp.csproj -c Release
```

输出：`bin\Release\net10.0-windows\ShellFtp.exe`，双击运行。

## 功能

### 文件传输（SFTP）

- 账号密码 / 私钥文件登录，站点保存、选择、删除
- 本地、远程双栏浏览，双击进目录、返回上级
- 单文件 / 多文件 / 整目录递归上传、下载
- 双栏互相拖拽，进度条 + 状态栏百分比
- 队列顺序传输，完成后自动刷新；远程新建目录、删除、重命名
- 列表按类型显示系统外壳文件夹 / 文件图标

### SSH 终端（外部 PuTTY）

- 工具栏「终端」用当前站点信息拉起外部 `putty.exe` 独立窗口，命令行：`-ssh -P 端口 -l 用户名 [-pw 密码] [-i 私钥.ppk] 主机 [远程命令]`
- 默认进入主窗体当前打开的远程目录：生成临时脚本（`cd 目录` 后 `exec ${SHELL:-/bin/sh}`），用 `-m` 传给 PuTTY，并加 `-t` 分配伪终端；脚本在 PuTTY 退出后自动清理
- PuTTY 路径解析顺序：程序同目录 → Downloads → PATH；`putty.exe` 已配置随输出目录打包
- 内置 SSH.NET 终端模拟器（`ShellForm` / `ShellService` / `TerminalEmulator`）代码保留在工程中，但不再作为终端入口
- `ShellService.RunCommand` 另提供一次性单命令执行接口（返回 `Result` / `ExitStatus` / `Error`）

## 认证与私钥

- 密码登录：用户名 + 密码。
- 私钥登录：SFTP 侧 SSH.NET 需 OpenSSH / PEM 格式私钥；外部 PuTTY 原生认 `.ppk`，SFTP 用的 `.ppk` 需先用 `PUTTYGEN.EXE` 导出 OpenSSH 格式。
- 认证逻辑统一在 `SshAuth`，SFTP 客户端与终端客户端共用同一套凭据。

## 已知限制（生产化前需补）

1. `sites.json` 已用 Windows DPAPI 整份加密（账号、密码不落明文）；密文绑定当前 Windows 用户 + 本机，换账户/机器无法解密，未接入可漫游的凭据管理器。
2. 终端走外部 PuTTY 独立窗口；内置 SSH.NET 模拟器代码仅保留未接入。PuTTY `-pw` 密码会出现在命令行参数中（进程列表可见），生产环境建议改接 Pageant 代理免密。
3. 传输为整文件覆盖写，无断点续传、限速、重名冲突策略。
4. 队列单线程顺序执行，无并发与失败重试。
5. 私钥口令（passphrase）未提供输入框；加密私钥需后续补充。
