@echo off

:: decoy values to understand the relationship between msbuild and WiX
set ddd=2020.1.1
set iii=20.1.1.100
@echo %0 :: DisplayVersion  = %ddd%
@echo %0 :: InternalVersion = %iii%
@echo.

call "%ProgramFiles(x86)%"\MSBuild\12.0\Bin\msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:DisplayVersion=%ddd% /p:InternalVersion=%iii%

@echo %0 :: result is in     wix\MinionMSI\bin\Release\
dir wix\MinionMSI\bin\Release\
