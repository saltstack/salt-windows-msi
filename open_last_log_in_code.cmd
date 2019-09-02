@echo off
:: Get the most recent file
for /f "delims=" %%a in ('dir %temp%\msi*.log /B /O:D')   do @set "msi=%%a"
code %temp%\%msi%
