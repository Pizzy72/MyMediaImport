@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Publish.ps1" %*
set "publishExitCode=%ERRORLEVEL%"

if not "%publishExitCode%"=="0" pause
exit /b %publishExitCode%
