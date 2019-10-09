@echo off
:: Get the most recent file
for /f "delims=" %%a in ('dir %temp%\msi*.log /B /O:D')   do @set "msi=%%a"
if "%msi%" == "" (echo no msi*.log files in %temp%) else (code %temp%\%msi%)
