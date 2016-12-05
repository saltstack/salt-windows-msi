@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo %0 :: msi  = %msi%
@echo.

msiexec /x wix\MinionMSI\bin\Release\%msi% /qn /l*v yinstall.log
