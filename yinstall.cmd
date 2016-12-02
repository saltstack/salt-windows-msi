@echo off
:: Get the versions from salt/version.py
for /f "delims=" %%a in ('python versionDisplayOrInternal.py') do @set "ddd=%%a"

@echo %0 :: DisplayVersion  = %ddd%
@echo.

msiexec /i wix\MinionMSI\bin\Release\Salt-Minion-%ddd%-amd64-Setup.msi /qn /l*v yinstall.log
