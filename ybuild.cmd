@echo off

if '%1'=='' (
  set targetplatformvalue=amd64
  goto :ok
)

if '%1'=='32' (
  set targetplatformvalue=win32
  goto :ok
)

echo wrong argument 
echo    ybuild         == 64bit
echo    ybuild 32      == 32bit
goto :eof

:ok

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


:: decoy version values to understand the relationship between msbuild and WiX...
@echo on
call "%ProgramFiles(x86)%"\MSBuild\14.0\Bin\msbuild.exe msbuild.proj /nologo /t:wix /p:TargetPlatform=%targetplatformvalue% /p:DisplayVersion=2020.1.1 /p:InternalVersion=20.1.1.100
@echo off

dir                          wix.d\MinionMSI\bin\Release\*.msi

if '%1'=='32' (
  echo ****** WARNING *******
  echo  ****** WARNING *******     The 32bit installer is untested
  echo   ****** WARNING *******
)


:eof
