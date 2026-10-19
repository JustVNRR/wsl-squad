@echo off
setlocal
"%~dp0wsl-squad.cli.exe" %*
exit /b %ERRORLEVEL%
