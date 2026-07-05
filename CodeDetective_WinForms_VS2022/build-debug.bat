@echo off
setlocal
cd /d "%~dp0"
dotnet restore CodeDetective.WinForms.sln
if errorlevel 1 exit /b 1
dotnet build CodeDetective.WinForms.sln -c Debug --no-restore
exit /b %errorlevel%
