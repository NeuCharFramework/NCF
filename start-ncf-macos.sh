#!/bin/bash

# NCF macOS 启动脚本
# 此脚本会自动处理 macOS 安全权限问题并启动 NCF 应用

echo "🍎 NCF macOS 启动脚本"
echo "========================"

# 检查是否在正确的目录（发布后的根目录应包含 Senparc.Web.dll）
if [ ! -f "Senparc.Web.dll" ]; then
    echo "❌ 错误：请在 NCF 发布目录中运行此脚本"
    echo "   目录应包含 Senparc.Web.dll 文件"
    echo "   当前目录: $(pwd)"
    echo "   目录内容:"
    ls -la
    exit 1
fi

# 显示当前目录
echo "📁 当前目录: $(pwd)"

# 移除隔离属性以避免安全警告
echo "🔓 正在移除 macOS 隔离属性..."
if command -v xattr >/dev/null 2>&1; then
    sudo xattr -rd com.apple.quarantine . 2>/dev/null
    echo "✅ 隔离属性已移除"
else
    echo "⚠️  警告：xattr 命令不可用，可能会出现安全警告"
fi

# 确保数据库目录存在
echo "📂 确保数据库目录存在..."
mkdir -p App_Data/Database
chmod 755 App_Data 2>/dev/null
chmod 755 App_Data/Database 2>/dev/null
echo "✅ 数据库目录已准备"

# 检查 .NET 运行时
echo "🔍 检查 .NET 运行时..."
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET 版本: $DOTNET_VERSION"
else
    echo "❌ 错误：未找到 .NET 运行时"
    echo "   请从 https://dotnet.microsoft.com/download/dotnet/8.0 下载安装"
    exit 1
fi

# 设置环境变量
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_RUNNING_IN_CONTAINER=false

echo "🚀 启动 NCF 应用..."
echo "========================"
echo "   HTTP:  http://localhost:5000"
echo "   HTTPS: https://localhost:5001"
echo "========================"
echo "按 Ctrl+C 停止应用"
echo ""

# 启动应用（发布版本使用 dotnet Senparc.Web.dll）
dotnet Senparc.Web.dll 