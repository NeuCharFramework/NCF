#!/bin/bash

# NCF macOS 启动脚本
# 此脚本会自动处理 macOS 安全权限问题并启动 NCF 应用

# 设置正确的字符编码
export LC_ALL=en_US.UTF-8
export LANG=en_US.UTF-8

echo "🍎 NCF macOS Startup Script"
echo "========================"

# Check if we're in the correct directory (should contain Senparc.Web.dll)
if [ ! -f "Senparc.Web.dll" ]; then
    echo "❌ Error: Please run this script in the NCF release directory"
    echo "   Directory should contain Senparc.Web.dll file"
    echo "   Current directory: $(pwd)"
    echo "   Directory contents:"
    ls -la
    exit 1
fi

# Show current directory
echo "📁 Current directory: $(pwd)"

# Remove quarantine attributes to avoid security warnings
echo "🔓 Removing macOS quarantine attributes..."
if command -v xattr >/dev/null 2>&1; then
    sudo xattr -rd com.apple.quarantine . 2>/dev/null
    echo "✅ Quarantine attributes removed"
else
    echo "⚠️  Warning: xattr command not available, security warnings may appear"
fi

# Ensure database directory exists
echo "📂 Ensuring database directory exists..."
mkdir -p App_Data/Database
chmod 755 App_Data 2>/dev/null
chmod 755 App_Data/Database 2>/dev/null
echo "✅ Database directory prepared"

# Check .NET runtime
echo "🔍 Checking .NET runtime..."
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET version: $DOTNET_VERSION"
else
    echo "❌ Error: .NET runtime not found"
    echo "   Please download and install from https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Set environment variables
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_RUNNING_IN_CONTAINER=false

echo "🚀 Starting NCF application..."
echo "========================"
echo "   HTTP:  http://localhost:5000"
echo "   HTTPS: https://localhost:5001"
echo "========================"
echo "Press Ctrl+C to stop the application"
echo ""

# Start the application (published version uses dotnet Senparc.Web.dll)
dotnet Senparc.Web.dll 