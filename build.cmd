@echo off

:: WISE STUDIO perceives the 64bit msi as 32bit msi.
:: Explicitly adding Platform
:: Platform should be x64 but this causes msbuild errror, therefor amd64
::     C:\git\salt-windows-msi\wix.sln.metaproj : error MSB4126: Die angegebene Projektmappenkonfiguration "Release|x64" ist ungültig. [C:\git\salt-windows-msi\wix.sln]
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

echo FATAL wrong argument. Only allowed value is 32
echo    build         == 64bit
echo    build 32      == 32bit
goto :eof

:ok


:: Detecting NSIS build...
set saltpythonexe=..\salt\pkg\windows\buildenv\bin\python.exe
dir %saltpythonexe% >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL Missing %saltpythonexe%
  echo       Have you build NSIS?  Try  "cd ..\salt\pkg\windows"  and  "build.bat"
  goto :eof
)


:: Detecting Salt Version...
%saltpythonexe% ..\salt\salt\version.py >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL cannot execute %saltpythonexe% ..\salt\salt\version.py
  goto :eof
)
for /f "delims=" %%A in ('%saltpythonexe% ..\salt\salt\version.py') do set "SaltDisplayVersion=%%A"

%saltpythonexe% ..\salt\salt\version.py msi >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL cannot execute %saltpythonexe% ..\salt\salt\version.py msi
  goto :eof
)
for /f "delims=" %%A in ('%saltpythonexe% ..\salt\salt\version.py msi') do set "SaltInternalVersion=%%A"
echo Found Salt   %SaltDisplayVersion% (msi %SaltInternalVersion%)


:: Detecting Python 2 or 3 from NSIS exe ...
set saltpythonversion=0
dir ..\salt\pkg\windows\installer\Salt-Minion*Py2*.exe >nul 2>&1
if %errorLevel%==0 (
  set saltpythonversion=2
)
dir ..\salt\pkg\windows\installer\Salt-Minion*Py3*.exe >nul 2>&1
if %errorLevel%==0 (
  set saltpythonversion=3
)
if %saltpythonversion%==0 (
  echo FATAL Cannot determine Python 2 or 3
  echo       There is neither ..\salt\pkg\windows\installer\Salt-Minion*Py2*.exe 
  echo                nor     ..\salt\pkg\windows\installer\Salt-Minion*Py3*.exe
  goto :eof
) else (
  echo Found Python %saltpythonversion%
)

:: Double check msbuild
set msbuildpath="%ProgramFiles(x86)%"\MSBuild\14.0\Bin
dir %msbuildpath% >nul 2>&1
if not %errorLevel%==0 (
  echo FATAL Requires MSBuild 2015 from https://www.microsoft.com/en-in/download/details.aspx?id=48159
  goto :eof
)


:: Call msbuid
@echo on
call %msbuildpath%\msbuild.exe msbuild.proj /nologo /t:wix /p:TargetPlatform=%salt_targetplatform% /p:Platform=%salt_platform% /p:DisplayVersion=%SaltDisplayVersion% /p:InternalVersion=%SaltInternalVersion% /p:PythonVersion=%saltpythonversion%
@echo off

dir                          wix.d\MinionMSI\bin\Release\*.msi

if '%1'=='32' (
  echo ****** WARNING *******
  echo  ****** WARNING *******     The 32bit installer is untested
  echo   ****** WARNING *******
)

set "salt_targetplatform="
set "salt_platform="
set "salt_bitness="
set "saltpythonexe="
set "msbuildpath="
set "SaltDisplayVersion%="
set "SaltInternalVersion="
set "saltpythonversion="


:eof
