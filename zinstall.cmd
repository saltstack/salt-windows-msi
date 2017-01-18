@echo off
:: Get the file name
for /f "delims=" %%a in ('dir /b wix\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix\MinionMSI\bin\Release\%msi% /qb! /l*v log-install.log START_MINION_SERVICE=1
