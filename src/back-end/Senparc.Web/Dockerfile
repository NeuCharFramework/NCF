# 此阶段用于在生产中使用，或在常规模式下从 VS 运行时使用
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# 设置时区和安装证书
RUN apt-get update && apt-get install -y --no-install-recommends \
    tzdata ca-certificates && \
    cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime && \
    echo Asia/Shanghai > /etc/timezone && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 将项目文件单独复制，便于缓存
COPY ["src/back-end/Senparc.Web/Senparc.Web.csproj", "Senparc.Web/"]
COPY ["src/back-end/Senparc.Areas.Admin/Senparc.Areas.Admin.csproj", "Senparc.Areas.Admin/"]
COPY ["src/back-end/Senparc.Xncf.Accounts/Senparc.Xncf.Accounts.csproj", "Senparc.Xncf.Accounts/"]
COPY ["src/back-end/Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj", "Senparc.Xncf.Installer/"]
COPY ["src/back-end/Senparc.Xncf.Installer.Interface/Senparc.Xncf.Installer.Interface.csproj", "Senparc.Xncf.Installer.Interface/"]
COPY ["src/back-end/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj", "Senparc.Aspire.AppHost/"]
COPY ["src/back-end/Senparc.Aspire.ServiceDefaults/Senparc.Aspire.ServiceDefaults.csproj", "Senparc.Aspire.ServiceDefaults/"]

# 恢复依赖项
RUN dotnet restore "Senparc.Web/Senparc.Web.csproj"

# 复制文件并构建
COPY . .
WORKDIR "/src/Senparc.Web"
RUN dotnet build "Senparc.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Senparc.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最终阶段，准备运行应用
FROM base AS final
WORKDIR /app

# 创建所需目录并设置权限
RUN mkdir -p /app/App_Data/SenparcTraceLog && \
    chmod -R 755 /app/App_Data/SenparcTraceLog

# 切换到应用程序用户
USER $APP_UID

COPY --from=publish /app/publish .

# 设置运行环境为开发环境，可选值为 Production、Development、Staging 默认值为Production
ENV ASPNETCORE_ENVIRONMENT Development

ENTRYPOINT ["dotnet", "Senparc.Web.dll"]
