# macOS 使用指南

## 🍎 NCF 在 macOS 上的快速启动

### 两步启动流程

下载并解压 NCF 发布（如：`ncf-macos-x64-v0.29.7.zip`）包后，在终端中执行以下三个命令：

#### 第一步：给启动脚本添加执行权限
```bash
chmod +x start-ncf-macos.sh
```

#### 第二步：运行启动脚本
```bash
./start-ncf-macos.sh
```

### 启动成功

脚本运行后，你会看到类似以下输出：
```
🍎 NCF macOS Startup Script
========================
📁 Current directory: /Users/yourname/Downloads/NCF Release X.X.X
🔓 Removing macOS quarantine attributes...
✅ Quarantine attributes removed
📂 Ensuring database directory exists...
✅ Database directory prepared
🔍 Checking .NET runtime...
✅ .NET version: X.X.XXX
🚀 Starting NCF application...
========================
   HTTP:  http://localhost:5000
   HTTPS: https://localhost:5001
========================
Press Ctrl+C to stop the application
```

### 访问应用

打开浏览器访问：
- **HTTP**: http://localhost:5000（默认启动） 
- **HTTPS**: https://localhost:5001（默认不启动） 

### 停止应用

在终端中按 `Ctrl+C` 停止应用。

---

## 📋 系统要求

- macOS 10.15+ (Catalina 或更高版本)
- .NET 8.0 运行时（如果没有安装，请从 https://dotnet.microsoft.com/download/dotnet/8.0 下载）
- 支持 Intel 和 Apple Silicon 处理器

---

## 📝 后续启动

当执行过上述指令并完成初始化安装后，后续启动只需要执行以下命令：

```bash
./start-ncf-macos.sh
```

或使用 .NET CLI 启动：

```bash
dotnet Senparc.Web.dll
```
此方法的优势是可以选择 https 等其他配置启动，如：

```bash
dotnet Senparc.Web.dll --urls "https://*:5001"
```
> 启动后可以使用 https://localhost:5001 访问，更多启动参数请参考：https://docs.microsoft.com/zh-cn/dotnet/core/tools/dotnet-run


**注意：** 此指南适用于 NCF v0.29.7+ 版本。 