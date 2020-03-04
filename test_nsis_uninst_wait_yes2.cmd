@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% NSIS_UNINSTALLSTRING="C:\abc\uninst.exe" NSIS_DISPLAYVERSION="v2017.9"


:: expected results:
::  Skipping action: Setnsis_uninst_waits_not (condition is false)
::  PROPERTY CHANGE: Adding nsis_uninst_waits_yes property. Its value is '1'.
