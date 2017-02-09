@echo off

call "%ProgramFiles(x86)%"\MSBuild\14.0\Bin\msbuild.exe msbuild.proj /t:setVersionProperties
