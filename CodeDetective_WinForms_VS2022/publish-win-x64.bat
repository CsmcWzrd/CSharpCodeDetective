@echo off
setlocal
cd /d "%~dp0"
dotnet publish CodeDetective.WinForms\CodeDetective.WinForms.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o publish\win-x64
exit /b %errorlevel%
