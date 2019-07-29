:: 2016-11-07  Markus Kramer   version required to clean. This is strange.

"%ProgramFiles(x86)%"\MSBuild\14.0\Bin\msbuild.exe msbuild.proj /nologo /clp:ErrorsOnly /t:clean /p:Version=2000.0.0
rmdir /s /q c:\git\salt-windows-msi\wix.d\MinionMSI\obj
rmdir /s /q c:\git\salt-windows-msi\wix.d\MinionMSI\bin
