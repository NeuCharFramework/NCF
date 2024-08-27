@echo off  
setlocal  
  
REM 变量设置  
set "PROJECT_PATH=X:\SENP~TV1\NEUC~C4M\NCF\src"  
set "OUTPUT_DIR=X:\SENP~TV1\NEUC~C4M\NCF\src\BUIL~ETV"  
set NUGET_SOURCE=https://www.nuget.org/api/v2/package  

REM 从文件读取Nuget API Key  
set /p NUGET_API_KEY=<nuget_key.txt    

REM 切换到项目目录  
cd /d "%PROJECT_PATH%"  
if %errorlevel% neq 0 (  
    echo Failed to change directory to %PROJECT_PATH%  
    pause  
    exit /b 1  
)  
  
REM 确认当前目录  
echo Current directory: %cd%  
  
REM 清理项目  
echo Cleaning the project...  
dotnet clean  
if %errorlevel% neq 0 (  
    echo Cleaning the project failed.  
    pause  
    exit /b 1  
)  
  
REM 还原项目依赖  
echo Restoring project dependencies...  
dotnet restore  
if %errorlevel% neq 0 (  
    echo Restoring project dependencies failed.  
    pause  
    exit /b 1  
)  
  
REM 编译项目  
echo Building the project...  
dotnet build -c Release  
if %errorlevel% neq 0 (  
    echo Building the project failed.  
    pause  
    exit /b 1  
)  
  
REM 创建 NuGet 包  
echo Packing the project into a NuGet package...  
dotnet pack -c Release -o "%OUTPUT_DIR%"  
if %errorlevel% neq 0 (  
    echo Packing the project into a NuGet package failed.  
    pause  
    exit /b 1  
)  
  
REM 列出生成的文件以进行调试  
echo Listing files in %OUTPUT_DIR%...  
dir "%OUTPUT_DIR%"  
if %errorlevel% neq 0 (  
    echo Listing files failed.  
    pause  
    exit /b 1  
)  
  
REM 查找最新生成的 NuGet 包  
set "LATEST_PACKAGE="  
for /f "delims=" %%i in ('dir /b /a-d /o-d "%OUTPUT_DIR%\*.nupkg" 2^>nul') do (  
    set "LATEST_PACKAGE=%OUTPUT_DIR%\%%i"  
    goto :found  
)  
  
:found  
if defined LATEST_PACKAGE (  
    echo Found latest package: "%LATEST_PACKAGE%"  
    if exist "%LATEST_PACKAGE%" (  
        echo Publishing "%LATEST_PACKAGE%" to NuGet...  
REM        "%~dp0nuget.exe" push "%LATEST_PACKAGE%" -Source "%NUGET_SOURCE%" -ApiKey "%NUGET_API_KEY%"  
	dotnet nuget push "%LATEST_PACKAGE%" --source "%NUGET_SOURCE%" --api-key "%NUGET_API_KEY%"  --timeout 3600
        if %errorlevel% neq 0 (  
            echo Publishing failed.  
            pause  
            exit /b 1  
        )  
    ) else (  
        echo File does not exist: "%LATEST_PACKAGE%"  
        pause  
        exit /b 1  
    )  
) else (  
    echo NuGet package not found!  
    pause  
    exit /b 1  
)  
  
endlocal  
echo Done.  
pause