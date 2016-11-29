@echo off
:: Get the version from git if not passed
if [%1]==[] (
    for /f "delims=" %%a in ('git describe') do @set "Version=%%a"
) else (
    set "Version=%~1"
)
@echo %0 :: Version %Version%
@echo.

msiexec /i wix\MinionMSI\bin\Release\Salt-Minion-%version%-amd64-Setup.msi /qn /l*v yinstall.log
