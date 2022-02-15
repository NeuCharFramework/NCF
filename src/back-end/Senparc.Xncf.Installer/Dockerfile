#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj", "Senparc.Xncf.Installer/"]
COPY ["Senparc.Areas.Admin/Senparc.Areas.Admin.csproj", "Senparc.Areas.Admin/"]
RUN dotnet restore "Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj"
COPY . .
WORKDIR "/src/Senparc.Xncf.Installer"
RUN dotnet build "Senparc.Xncf.Installer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Senparc.Xncf.Installer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Senparc.Xncf.Installer.dll"]
