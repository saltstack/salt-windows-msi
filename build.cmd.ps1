# The msi installer requires an NSIS exe. e.g.:
#   Salt-Minion-3000-Py3-AMD64-Setup.exe
#   Salt-Minion-3000-Py2-AMD64-Setup.exe
#   Salt-Minion-3000-Py3-x86-Setup.exe
#

Set-PSDebug -Strict
Set-strictmode -version latest

# Detecting Salt version...
$pythonexe = "..\salt\pkg\windows\buildenv\bin\python.exe"

if (-Not (Test-Path $pythonexe -PathType leaf)) {
    Write-Host -ForegroundColor Red No file $pythonexe
    Write-Host -ForegroundColor Red Have you build the NSIS Nullsoft exe installer?
    exit(1)
}

$displayversion  = & $pythonexe ..\salt\salt\version.py
if (-not ($?)) {exit(1)}

$internalversion = & $pythonexe ..\salt\salt\version.py msi
if (-not ($?)) {exit(1)}

if (-not ($displayversion -match '^[\d\.]{6,18}$')) {
  Write-Host -ForegroundColor Red $displayversion is not a version
  exit(1)
}
if (-not ($internalversion -match '^\d\d\.[\d]{1,3}\.[\d]{1,3}\.*[\d]{0,3}$')) {
  Write-Host -ForegroundColor Red $internalversion is not a valid msi version
  exit(1)
}
Write-Host -ForegroundColor Green "Found Salt $displayversion (msi $internalversion)"


# Detecting target platform from NSIS exe ...
$salt_targetplatform = 0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*AMD64*.exe) {$salt_targetplatform="amd64"; $salt_platform="amd64"}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*x86*.exe)   {$salt_targetplatform="x86";   $salt_platform="x86"}
if ($salt_targetplatform -eq 0) {
  Write-Host -ForegroundColor Red Cannot determine target platform
  Write-Host -ForegroundColor Red No file ..\salt\pkg\windows\installer\Salt-Minion*.exe
  Write-Host -ForegroundColor Red Have you build the NSIS Nullsoft exe installer?
exit(1)
}
Write-Host -ForegroundColor Green "Found target platform $salt_targetplatform"


# Detecting Python 2 or 3 from NSIS exe ...
$saltpythonversion=0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*Py2*.exe) {$saltpythonversion=2}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*Py3*.exe) {$saltpythonversion=3}
if ($saltpythonversion -eq 0) {
  $saltpythonversion = 2
  Write-Host -ForegroundColor Red "Cannot determine Python 2 or 3"
  Write-Host -ForegroundColor Red No file ..\salt\pkg\windows\installer\Salt-Minion*.exe
  Write-Host -ForegroundColor Red Have you build the NSIS Nullsoft exe installer?
  exit(1)
}
Write-Host -ForegroundColor Green "Found Python $saltpythonversion"


# Call msbuid
$msbuildexe = 'C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe'
& "$msbuildexe" msbuild.proj /nologo /t:wix `
 /p:TargetPlatform=$salt_targetplatform `
 /p:Platform=$salt_platform `
 /p:DisplayVersion=$displayversion `
 /p:InternalVersion=$internalversion `
 /p:PythonVersion=$saltpythonversion
 if (-not ($?)) {exit(1)}

dir wix.d\MinionMSI\bin\Release\*.msi
