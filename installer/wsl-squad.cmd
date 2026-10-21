@echo off
setlocal
"%~dp0cli\wsl-squad.cli.exe" %*
exit /b %ERRORLEVEL%
