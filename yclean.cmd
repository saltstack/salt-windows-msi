:: 2016-11-07  Markus Kramer   This file is a first attempt to run the salt-windows-msi project.
:: 2016-11-07  Markus Kramer   version required to clean. This is strange.

msbuild.exe msbuild.proj /t:clean /p:Version=v2016.3.1 
