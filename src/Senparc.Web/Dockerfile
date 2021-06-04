# https://hub.docker.com/_/microsoft-dotnet-sdk/
FROM  mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source
COPY NCF.sln .
COPY Senparc.Web/Senparc.Web.csproj											./Senparc.Web/
COPY Senparc.Core/Senparc.Core.csproj 										./Senparc.Core/ 
COPY Senparc.Utility/Senparc.Utility.csproj 								./Senparc.Utility/ 
COPY Senparc.Log/Senparc.Log.csproj 										./Senparc.Log/ 
COPY Senparc.ImageUtility/Senparc.ImageUtility.csproj 						./Senparc.ImageUtility/ 
COPY Senparc.Service/Senparc.Service.csproj 								./Senparc.Service/ 
COPY Senparc.Threads/Senparc.Threads.csproj 								./Senparc.Threads/ 
COPY Senparc.Web.UI/Senparc.Web.UI.csproj 									./Senparc.Web.UI/ 
COPY Senparc.Office/Senparc.Office.csproj 									./Senparc.Office/ 
COPY Senparc.File/Senparc.File.csproj 										./Senparc.File/ 
COPY Senparc.Respository/Senparc.Respository.csproj 						./Senparc.Respository/ 
COPY Senparc.SMS/Senparc.SMS.csproj 										./Senparc.SMS/ 
COPY Senparc.Services.Tests/Senparc.Services.Tests.csproj 					./Senparc.Services.Tests/ 
COPY Senparc.SMS.Tests/Senparc.SMS.Tests.csproj 							./Senparc.SMS.Tests/ 
COPY Senparc.Areas.Admin/Senparc.Areas.Admin.csproj 						./Senparc.Areas.Admin/ 
COPY XncfExtensionSample/MyExtensionCode/MyExtensionCode.csproj 			./XncfExtensionSample/MyExtensionCode/ 
COPY Senparc.Web.FirefoxDriverTest/Senparc.Web.FirefoxDriverTest.csproj 	./Senparc.Web.FirefoxDriverTest/ 
COPY Senparc.Web.DatabasePlant/Senparc.Web.DatabasePlant.csproj 			./Senparc.Web.DatabasePlant/ 
RUN dotnet restore Senparc.Web/Senparc.Web.csproj

COPY . .
WORKDIR "/source/Senparc.Web"
RUN dotnet publish "Senparc.Web.csproj" -c Release -o /app --no-restore


# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/aspnet:3.1  AS final
EXPOSE 8012
EXPOSE 1443
EXPOSE 80

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Senparc.Web.dll"]


