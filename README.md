# AutoSerialPort

一个基于 Avalonia UI 的跨平台多串口数据采集与转发应用程序。

## 项目简介

AutoSerialPort 是一个功能强大的串口数据采集和转发工具，支持同时管理多个串口设备，提供灵活的数据解析策略和多种数据转发方式。适用于工业数据采集、设备监控、条码扫描等场景。

## 主要功能

### 多设备串口管理
- 支持同时连接多个串口设备
- 多种设备识别方式（端口名、USB VID/PID、PnP 设备 ID）
- 可配置串口参数（波特率、校验位、数据位、停止位）
- 实时连接状态监控

### 数据解析
- **行解析器**：按分隔符拆分数据
- **JSON 字段解析器**：从 JSON 数据中提取指定字段
- **电子秤解析器**：处理称重数据
- **条码解析器**：处理条码扫描输入
- **帧解码器**：支持定界符、头尾标识、固定长度等帧格式

### 数据转发
- **TCP 转发**：支持客户端/服务器模式
- **MQTT 转发**：发布到 MQTT 代理，支持 TLS
- **剪贴板转发**：复制数据到系统剪贴板
- **键盘模拟转发**：模拟键盘输入，可配置延迟

### 用户界面
- 现代化 Fluent 设计风格
- 设备列表实时状态显示
- 选项卡式配置界面（串口设置、解析、转发、控制台、日志）
- 实时数据监控（支持十六进制/文本显示）
- 内置串口控制台

## 技术栈

- **.NET 8.0**
- **Avalonia UI 11.0.6** - 跨平台 UI 框架
- **CommunityToolkit.Mvvm** - MVVM 框架
- **SqlSugarCore** - ORM 数据库操作
- **Microsoft.Data.Sqlite** - SQLite 数据库
- **MQTTnet** - MQTT 客户端
- **Serilog** - 结构化日志
- **System.IO.Ports** - 串口通信

## 项目结构

```
AutoSerialPort/
├── src/
│   ├── AutoSerialPort.Host/           # 主程序入口和依赖注入
│   ├── AutoSerialPort.UI/             # Avalonia UI 层（视图、视图模型）
│   ├── AutoSerialPort.Application/    # 应用服务和抽象
│   ├── AutoSerialPort.Infrastructure/ # 基础设施实现
│   └── AutoSerialPort.Domain/         # 领域模型和接口
├── tests/                             # 测试项目
└── docs/                              # 文档
```

## 快速开始

### 环境要求

- .NET 8.0 SDK
- Visual Studio 2022 / JetBrains Rider / VS Code

### 构建运行

```bash
# 克隆项目
git clone <repository-url>
cd AutoSerialPort

# 构建项目
dotnet build AutoSerialPort.sln

# 运行程序
dotnet run --project src/AutoSerialPort.Host
```

### 发布

```bash
# 发布 Windows 独立可执行文件
dotnet publish src/AutoSerialPort.Host -c Release -r win-x64 --self-contained

# 发布 Linux 版本
dotnet publish src/AutoSerialPort.Host -c Release -r linux-x64 --self-contained
```

## 使用说明

1. **添加设备**：点击添加按钮，配置串口参数和设备识别方式
2. **配置解析**：选择合适的数据解析策略
3. **设置转发**：配置数据转发目标（TCP/MQTT/剪贴板/键盘模拟）
4. **启动采集**：连接设备开始数据采集

## 架构设计

项目采用 **Clean Architecture** 架构模式：

- **Domain 层**：核心业务模型和接口定义
- **Application 层**：应用服务和视图模型抽象
- **Infrastructure 层**：串口处理、解析器、转发器的具体实现
- **UI 层**：基于 Avalonia 的用户界面
- **Host 层**：应用程序启动和依赖注入配置
