@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% /l*v log-install.log
