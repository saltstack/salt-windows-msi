:: 2016-11-07  Markus Kramer   This is a first attempt to automate the salt-windows-msi project.
::                             The version is hard coded.
@echo off
:: Get the version from git if not passed
if [%1]==[] (
    for /f "delims=" %%a in ('git describe') do @set "Version=%%a"
) else (
    set "Version=%~1"
)
@echo %0 :: Version %Version%
@echo.

c:\windows\microsoft.net\framework\v4.0.30319\msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:Version=%Version%
