@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
PowerShell Set-MpPreference -DisableRealtimeMonitoring $true
msiexec /i wix.d\MinionMSI\bin\Release\%msi%
PowerShell Set-MpPreference -DisableRealtimeMonitoring $false
