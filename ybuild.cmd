@echo off

:: just decoys
@echo %0 :: DisplayVersion  = 2020.1.1
@echo %0 :: InternalVersion = 20.1.1.100
@echo.

::c:\windows\microsoft.net\framework\v4.0.30319
"%ProgramFiles(x86)%"\MSBuild\12.0\Bin\msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:DisplayVersion=%ddd% /p:InternalVersion=%iii%

@echo %0 :: dir wix\MinionMSI\bin\Release\
dir wix\MinionMSI\bin\Release\
