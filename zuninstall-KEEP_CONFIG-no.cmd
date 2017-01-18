@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on

msiexec /x wix\MinionMSI\bin\Release\%msi% /qb! /l*v log-uninstall-KEEP_CONFIG=0.log KEEP_CONFIG=0
