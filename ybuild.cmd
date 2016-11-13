:: 2016-11-07  Markus Kramer   This is a first attempt to automate the salt-windows-msi project.
::                             The version is hard coded.
msbuild.exe msbuild.proj /t:wix /p:TargetPlatform=amd64 /p:Version=2016.3.11
