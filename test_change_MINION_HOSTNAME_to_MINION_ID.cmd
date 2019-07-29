@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% MINION_HOSTNAME=this_will_fail

