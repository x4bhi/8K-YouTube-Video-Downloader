@echo off
echo Cleaning project...

REM Delete the Release output folder
if exist "Release" (
    rmdir /s /q "Release"
    echo [OK] Deleted Release folder.
) else (
    echo [INFO] Release folder does not exist.
)

REM Ask if they want to wipe the hidden AppData engines to test fresh extraction
echo.
set /p wipe_appdata="Do you want to delete the extracted engines in AppData as well to test a fresh install? (Y/N): "
if /I "%wipe_appdata%"=="Y" (
    if exist "%LocalAppData%\Microsoft\MediaServices" (
        rmdir /s /q "%LocalAppData%\Microsoft\MediaServices"
        echo [OK] Deleted hidden MediaServices folder.
    ) else (
        echo [INFO] Hidden engine folder does not exist.
    )
)

echo.
echo Clean complete!
pause
