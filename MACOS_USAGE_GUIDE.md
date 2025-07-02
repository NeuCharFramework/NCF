# macOS 使用指南

## 🍎 NCF 在 macOS 上的使用指南

### 常见问题与解决方案

#### 1. SQLite 动态库安全警告

**问题：** macOS 显示"未打开 'libe_sqlite3.dylib'"，Apple 无法验证此文件。

**问题原因：** 
- `libe_sqlite3.dylib` 是 SQLite 数据库的原生动态库
- 当应用启动时，.NET 运行时会自动提取/加载这个库文件
- macOS Gatekeeper 阻止未签名的原生库运行

**解决方案：**

##### 方法 A：系统偏好设置（推荐）
1. 关闭警告对话框
2. 打开 **系统偏好设置** → **安全性与隐私** → **通用**
3. 看到被阻止的应用信息后，点击 **"仍要打开"**
4. 重新运行应用

##### 方法 B：命令行允许
```bash
# 临时禁用 Gatekeeper
sudo spctl --master-disable

# 运行应用后重新启用
sudo spctl --master-enable
```

##### 方法 C：允许特定目录
```bash
# 允许整个应用目录
sudo xattr -rd com.apple.quarantine /path/to/ncf-app

# 例如：
sudo xattr -rd com.apple.quarantine ./ncf-macos-arm64-v0.29.7-build1234
```

#### 2. 数据库权限问题

**确保数据库目录有写权限：**

```bash
# 检查权限
ls -la App_Data/Database/

# 如果需要，修改权限
chmod 755 App_Data/
chmod 755 App_Data/Database/
chmod 666 App_Data/Database/*.db
```

#### 3. 运行时依赖

**确保安装了 .NET 8 运行时：**

```bash
# 检查 .NET 版本
dotnet --version

# 如果没有安装，下载安装：
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 🚀 启动应用

#### 方法1：使用自动化脚本（推荐）

1. 将 `start-ncf-macos.sh` 脚本复制到你的 NCF 应用目录
2. 在终端中运行：

```bash
cd ncf-macos-x64-v*    # 或 ncf-macos-arm64-v*

# 给脚本添加执行权限
chmod +x start-ncf-macos.sh

# 运行脚本（注意：macOS 使用 ./ 不是 .\）
./start-ncf-macos.sh
```

**脚本会自动：**
- ✅ 移除 macOS 隔离属性
- ✅ 创建数据库目录
- ✅ 检查 .NET 运行时
- ✅ 设置最佳环境变量
- ✅ 启动应用

> **注意**：从 v0.29.7+ 版本开始，脚本文件已在CI/CD中自动转换为正确的Unix格式，不会再出现 `bad interpreter` 或行结束符问题。

#### 方法2：手动启动

##### Intel Mac (x64)
```bash
cd ncf-macos-x64-v*

# 首次运行前，移除隔离属性（避免安全警告）
sudo xattr -rd com.apple.quarantine .

# 启动应用
dotnet Senparc.Web.dll
```

##### Apple Silicon Mac (ARM64)
```bash
cd ncf-macos-arm64-v*

# 首次运行前，移除隔离属性（避免安全警告）
sudo xattr -rd com.apple.quarantine .

# 启动应用
dotnet Senparc.Web.dll
```

### 🔧 配置建议

#### 1. 防火墙设置
- 首次运行时允许网络连接
- 默认端口：5000 (HTTP) 和 5001 (HTTPS)

#### 2. 性能优化
```bash
# 设置环境变量以获得更好性能
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_RUNNING_IN_CONTAINER=false
```

#### 3. 日志查看
```bash
# 查看应用日志
tail -f App_Data/SenparcTraceLog/*.log
```

### ⚠️ 注意事项

1. **首次运行**：macOS 可能需要几分钟来验证所有依赖项
2. **网络权限**：应用需要网络访问权限来下载包和连接服务
3. **文件权限**：确保应用目录有读写权限
4. **系统兼容性**：
   - macOS 10.15+ (Catalina 或更高版本)
   - 支持 Intel 和 Apple Silicon 处理器

### 🆘 故障排除

#### 问题：脚本执行错误 (`bad interpreter`)
如果遇到 `./start-ncf-macos.sh: bad interpreter: /bin/bash^M: no such file or directory` 错误：

```bash
# 这是行结束符问题，转换文件格式
sed -i '' 's/\r$//' start-ncf-macos.sh

# 然后重新运行
./start-ncf-macos.sh
```

> **注意**：v0.29.7+ 版本的官方发布包已修复此问题，仅在使用旧版本或自定义构建时可能遇到。

#### 问题：应用无法启动
```bash
# 检查 .NET 运行时
dotnet --version

# 检查主要文件是否存在
ls -la Senparc.Web.dll

# 检查目录权限
ls -la App_Data/
```

#### 问题：数据库连接失败
1. 检查 `App_Data/Database/` 目录是否存在
2. 检查 SQLite 文件权限
3. 查看 `SenparcConfig.config` 配置

#### 问题：端口占用
```bash
# 查看端口占用
lsof -i :5000
lsof -i :5001

# 终止占用进程
kill -9 <PID>
```

### 📞 技术支持

如果遇到其他问题，请提供以下信息：
- macOS 版本：`sw_vers`
- .NET 版本：`dotnet --version`
- 处理器类型：`uname -m`
- 错误日志：`App_Data/SenparcTraceLog/` 中的最新日志

---

**注意：** 此指南适用于 NCF v0.29.7+ 版本。 