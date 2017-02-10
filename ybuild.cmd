@echo off

:: Detecting git...
dir "C:\Program Files\Git\cmd\git.exe" >nul 2>&1
if not %errorLevel%==0 (
    echo FATAL failure: This script needs git in "C:\Program Files\Git\cmd\git.exe"
    goto eof
)

:: Detecting Python...
dir "C:\python27\python.exe" >nul 2>&1
if not %errorLevel%==0 (
    echo FATAL failure: This script needs Python in "C:\python27\python.exe"
    goto eof
)

:: Detecting Salt repository...
dir "C:\git\salt\.git" >nul 2>&1
if not %errorLevel%==0 (
    echo FATAL failure: This script needs the Salt repository  in "C:\git\salt\"
    goto eof
)


:: decoy values to understand the relationship between msbuild and WiX
set ddd=2020.1.1
set iii=20.1.1.100
@echo %0 :: DisplayVersion  = %ddd%
@echo %0 :: InternalVersion = %iii%
@echo.

call "%ProgramFiles(x86)%"\MSBuild\14.0\Bin\msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:DisplayVersion=%ddd% /p:InternalVersion=%iii%

@echo %0 :: result is in     wix.d\MinionMSI\bin\Release\
dir                          wix.d\MinionMSI\bin\Release\


:eof
