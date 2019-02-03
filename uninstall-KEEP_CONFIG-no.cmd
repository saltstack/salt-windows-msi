@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /x wix.d\MinionMSI\bin\Release\%msi% /qb! KEEP_CONFIG=0
@echo off

dir "C:\salt" >nul 2>&1
if %errorLevel%==0 (
  dir /s /b c:\salt
)