:: 2016-11-07  Markus Kramer   This file is a first attempt to run the salt-windows-msi project.
:: 2016-11-07  Markus Kramer   version required to clean. This is strange.

@echo off
:: Get the version from git if not passed
if [%1]==[] (
    for /f "delims=" %%a in ('git describe') do @set "Version=%%a"
) else (
    set "Version=%~1"
)
@echo %0 :: Version %Version%
@echo.


c:\windows\microsoft.net\framework\v4.0.30319\msbuild.exe msbuild.proj /t:clean /p:Version=%Version%
