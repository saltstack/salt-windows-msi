@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% NSIS_UNINSTALLSTRING="C:\abc\uninst.exe" NSIS_DISPLAYVERSION="v2016.4"


:: expected results:
::  PROPERTY CHANGE: Adding nsis_uninst_waits_not property. Its value is '1'.
::  Skipping action: Setnsis_uninst_waits_yes (condition is false)
