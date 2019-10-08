@echo off

:: WISE STUDIO perceives the 64bit msi as 32bit msi.
:: Explicitly adding Platform
:: Platform should be x64 but this causes msbuild errror, therefor amd64
::     C:\git\salt-windows-msi\wix.sln.metaproj : error MSB4126: Die angegebene Projektmappenkonfiguration "Release|x64" ist ung�ltig. [C:\git\salt-windows-msi\wix.sln]
:: SuperOrca shows no difference between msi without Platformm and msi with Platform=amd64

if '%1'=='' (
  set salt_targetplatform=amd64
  set salt_platform=amd64
  set salt_bitness=64
  goto :ok
)

if '%1'=='32' (
  set salt_targetplatform=win32
  set salt_platform=x86
  set salt_bitness=32
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

:: Checking if we can determine Salt Version...
echo We are going to build version...
C:\Python27\python \git\salt\salt\version.py 
if not %errorLevel%==0 (
  echo FATAL failure: This script needs to execute C:\Python27\python \git\salt\salt\version.py
  goto eof
)
echo We are going to build msi internal version...
C:\Python27\python \git\salt\salt\version.py msi
if not %errorLevel%==0 (
  echo FATAL failure: This script needs to execute C:\Python27\python \git\salt\salt\version.py
  goto eof
)

:: Detecting NSIS build...
set saltpythonexe=..\salt\pkg\windows\buildenv\bin\python.exe
dir %saltpythonexe% >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL Missing %saltpythonexe%
  echo       Have you build NSIS?  try  "cd ..\salt\pkg\windows"  and  "build.bat"
  goto eof
)

:: msbuild
set msbuildpath="%ProgramFiles(x86)%"\MSBuild\14.0\Bin
dir %msbuildpath% >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL Requires MSBuild 2015 from https://www.microsoft.com/en-in/download/details.aspx?id=48159
  goto eof
)


:: decoy version values to understand the relationship between msbuild and WiX...
@echo on
call %msbuildpath%\msbuild.exe msbuild.proj /nologo /t:wix /p:TargetPlatform=%salt_targetplatform% /p:Platform=%salt_platform% /p:DisplayVersion=2020.1.1 /p:InternalVersion=20.1.1.100
@echo off

dir                          wix.d\MinionMSI\bin\Release\*.msi

if '%1'=='32' (
  echo ****** WARNING *******
  echo  ****** WARNING *******     The 32bit installer is untested
  echo   ****** WARNING *******
)


:eof
