@echo off
setlocal

set "PKG_DIR=%~dp0packages"
set "CSC_PATH=%PKG_DIR%\Microsoft.Net.Compilers.Toolset\tasks\net472\csc.exe"

if not exist "%CSC_PATH%" (
    echo [Setup] Modern C# Compiler not found. Downloading Roslyn toolset via NuGet...
    if not exist "%~dp0nuget.exe" (
        powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe"
    )
    .\nuget.exe install Microsoft.Net.Compilers.Toolset -Version 4.8.0 -ExcludeVersion -OutputDirectory "%PKG_DIR%"
)

echo.
echo Compressing engine tools... (This will drastically reduce EXE size)
if exist "tools.zip" del /q "tools.zip"
powershell -Command "Compress-Archive -Path tools\* -DestinationPath tools.zip -Force"

echo Compiling NexTube (Standalone)...
if not exist "Release" mkdir Release
"%CSC_PATH%" /nologo /target:winexe /win32icon:Icon.ico /out:Release\NexTube.exe /resource:tools.zip,tools.zip /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /warn:2 /optimize+ /recurse:src\*.cs

if %errorlevel% equ 0 (
    echo.
    echo Build successful! Executable is NexTube.exe
) else (
    echo.
    echo Build failed! See errors above.
)
