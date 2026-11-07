@echo off
setlocal

set "NSIS=makensis.exe"
where %NSIS% >nul 2>nul
if errorlevel 1 set "NSIS=C:\Program Files (x86)\NSIS\makensis.exe"

git -C "%~dp0." status --porcelain --untracked-files=no | findstr . >nul
if not errorlevel 1 (
    echo.
    echo WARNING: There are uncommitted changes. The installer then matches no committed state.
    echo.
    choice /c YN /m "Build anyway"
    if errorlevel 2 goto :aborted
)

echo === Publishing AntiBayer ===
if exist "%~dp0publish" rmdir /s /q "%~dp0publish"
dotnet publish "%~dp0src\AntiBayer.App\AntiBayer.App.csproj" -c Release -r win-x64 --self-contained true -o "%~dp0publish"
if errorlevel 1 goto :error

set "VERSION="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "$v = (Get-Item '%~dp0publish\AntiBayer.exe').VersionInfo; '{0}.{1}.{2}' -f $v.FileMajorPart, $v.FileMinorPart, $v.FileBuildPart"`) do set "VERSION=%%v"
if "%VERSION%"=="" goto :error

echo === Building installer (version %VERSION%) ===
"%NSIS%" /V2 /DPRODUCT_VERSION=%VERSION% "%~dp0installer\antibayer.nsi"
if errorlevel 1 goto :error

echo.
echo Done: installer\AntiBayer-Setup.exe
pause
goto :eof

:aborted
echo Aborted, nothing was built.
pause
exit /b 1

:error
echo.
echo Build failed.
pause
exit /b 1
