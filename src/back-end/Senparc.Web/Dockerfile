# 此阶段用于在生产中使用，或在常规模式下从 VS 运行时使用
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
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
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# 恢复依赖项
RUN dotnet restore "Senparc.Web/Senparc.Web.csproj"

# 构建项目
WORKDIR "/src/Senparc.Web"
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
