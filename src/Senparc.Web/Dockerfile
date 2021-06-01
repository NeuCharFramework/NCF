# https://hub.docker.com/_/microsoft-dotnet-sdk/
FROM  mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY NCF.sln ./
COPY Senparc.Web/Senparc.Web.csproj Senparc.Web/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Senparc.Web
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app --no-restore


# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/aspnet:3.1  AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Senparc.Web.dll"]
