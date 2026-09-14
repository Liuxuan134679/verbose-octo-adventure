@echo off
setlocal
cd /d "%~dp0"

where python >nul 2>nul
if errorlevel 1 (
  echo Python was not found. Use Unity: Boss Clicker ^> Build And Run WebGL Prototype.
  pause
  exit /b 1
)

python "%~dp0Tools\serve_webgl.py" "%~dp0Builds\WebGL" --open
if errorlevel 1 pause
