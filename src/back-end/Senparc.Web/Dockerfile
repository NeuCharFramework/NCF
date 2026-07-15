# 此阶段用于在生产中使用，或在常规模式下从 VS 运行时使用
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# 设置时区和安装证书，然后清理缓存
RUN apt-get update && apt-get install -y --no-install-recommends \
    tzdata ca-certificates && \
    cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime && \
    echo Asia/Shanghai > /etc/timezone && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 将项目文件单独复制，便于缓存
COPY ["tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj", "tools/NcfSimulatedSite/Senparc.Web/"]
COPY ["tools/NcfSimulatedSite/Senparc.Areas.Admin/Senparc.Areas.Admin.csproj", "tools/NcfSimulatedSite/Senparc.Areas.Admin/"]
COPY ["tools/NcfSimulatedSite/Senparc.Xncf.Accounts/Senparc.Xncf.Accounts.csproj", "tools/NcfSimulatedSite/Senparc.Xncf.Accounts/"]
COPY ["tools/NcfSimulatedSite/Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj", "tools/NcfSimulatedSite/Senparc.Xncf.Installer/"]
COPY ["tools/NcfSimulatedSite/Senparc.Xncf.Installer.Interface/Senparc.Xncf.Installer.Interface.csproj", "tools/NcfSimulatedSite/Senparc.Xncf.Installer.Interface/"]
COPY ["tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj", "tools/NcfSimulatedSite/Senparc.Aspire.AppHost/"]
COPY ["tools/NcfSimulatedSite/Senparc.Aspire.ServiceDefaults/Senparc.Aspire.ServiceDefaults.csproj", "tools/NcfSimulatedSite/Senparc.Aspire.ServiceDefaults/"]

# 复制所有 src 目录下的项目文件（用于依赖恢复）
# .dockerignore 文件已配置忽略 bin、obj 等不必要的文件
COPY ["src/", "src/"]

# 恢复依赖项
RUN dotnet restore "tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj"

# 复制其余文件并构建
# .dockerignore 文件确保不会复制 bin、obj 等编译生成的文件
COPY . .
WORKDIR "/src/tools/NcfSimulatedSite/Senparc.Web"
RUN dotnet build "Senparc.Web.csproj" -c Release -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
# 在发布前创建必要的目录结构
RUN mkdir -p /app/publish/App_Data/Database /app/publish/App_Data/SenparcTraceLog
RUN dotnet publish "Senparc.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 最终阶段，准备运行应用
FROM base AS final
WORKDIR /app

# 创建所需目录并设置权限
RUN mkdir -p /app/App_Data/SenparcTraceLog /app/App_Data/Database && \
    chmod -R 755 /app/App_Data

# 切换到应用程序用户（ASP.NET Core 8.0 基础镜像中的默认用户）
USER app

COPY --from=publish /app/publish .

# 设置运行环境为开发环境，可选值为 Production、Development、Staging 默认值为Production
ENV ASPNETCORE_ENVIRONMENT Development

ENTRYPOINT ["dotnet", "Senparc.Web.dll"]
