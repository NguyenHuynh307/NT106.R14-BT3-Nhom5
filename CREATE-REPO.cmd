@echo off
setlocal
cd /d "%~dp0"
echo ==============================================
echo  Tao repo GitHub: NT106.R14-BT3-Nhom5
echo ==============================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0CREATE-REPO.ps1"
echo.
pause
