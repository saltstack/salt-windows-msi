@echo off


:: Get the versions from salt/version.py
for /f "delims=" %%a in ('python versionDisplayOrInternal.py i') do @set "iii=%%a"
for /f "delims=" %%a in ('python versionDisplayOrInternal.py')   do @set "ddd=%%a"

set iii=20.1.1.100
set ddd=2020.1.1

@echo %0 :: DisplayVersion  = %ddd%
@echo %0 :: InternalVersion = %iii%
@echo.

::c:\windows\microsoft.net\framework\v4.0.30319
"%ProgramFiles(x86)%"\MSBuild\12.0\Bin\msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:DisplayVersion=%ddd% /p:InternalVersion=%iii%
