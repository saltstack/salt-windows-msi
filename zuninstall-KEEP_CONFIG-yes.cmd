@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on

msiexec /x wix\MinionMSI\bin\Release\%msi% /qn /l*v log-uninstall.log KEEP_CONFIG=1
