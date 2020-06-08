# This msi installer requires an NSIS exe. e.g.:
#   Salt-Minion-3000-Py3-AMD64-Setup.exe
#   Salt-Minion-3000-Py2-AMD64-Setup.exe
#   Salt-Minion-3000-Py3-x86-Setup.exe
#
Set-PSDebug -Strict
Set-strictmode -version latest

# # # Detecting Salt version from Git # # #
if (-Not (Test-Path ..\salt)) {
  Write-Host -ForegroundColor Red No directory ..\salt
  Write-Host -ForegroundColor Red Have you build the NSIS Nullsoft exe installer?
  exit(1)
}
Push-Location ..\salt
$displayversion = & git describe
Pop-Location
[regex]$tagRE = '(?:[^\d]+)?(?<major>[\d]{1,4})(?:\.(?<minor>[\d]{1,2}))?(?:\.(?<bugfix>[\d]{0,2}))?'
$tagREM = $tagRE.Match($displayversion)
$major  = $tagREM.groups["major"].ToString()
$minor  = $tagREM.groups["minor"]
$bugfix = $tagREM.groups["bugfix"]
# Remove leading v from Git tag for display version (releases or release candidates).
$displayversion = $displayversion -replace '^v(.+)$', '$1'
if ([string]::IsNullOrEmpty($minor)) {$minor = 0}
if ([string]::IsNullOrEmpty($bugfix)) {$bugfix = 0}
# Assumption: major is a number
if ([convert]::ToInt32($major, 10) -ge 3000) {      # 3000 scheme
  # Assumption: major has 4 digits, get first and second half
  $major1 = $major.substring(0, 2)
  $major2 = $major.substring(2)
  $internalversion = "$major1.$major2.$minor"
} else {    # Year.Month.Bugfix scheme
  $year  = $major.substring(2)
  $month = $minor
  $internalversion = "$year.$month.$bugfix"
}
Write-Host -ForegroundColor Green "Found Salt $displayversion (msi $internalversion)"

# # # Detecting target platform from NSIS exe # # #
$targetplatform = 0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*AMD64*.exe) {$targetplatform="amd64"; $platform="amd64"}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*x86*.exe)   {$targetplatform="win32"; $platform="win32"}
if ($targetplatform -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine target platform"
  Write-Host -ForegroundColor Red "No file ..\salt\pkg\windows\installer\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}
Write-Host -ForegroundColor Green "Found target platform $targetplatform"

# # # Detecting Python version from NSIS exe # # #
$pythonversion = 0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*Py2*.exe) {$pythonversion=2}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*Py3*.exe) {$pythonversion=3}
if ($pythonversion -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine Python version"
  Write-Host -ForegroundColor Red "No file ..\salt\pkg\windows\installer\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}
Write-Host -ForegroundColor Green "Found Python $pythonversion"

# # # Call msbuid # # #
# Determine Architecture (32 or 64 bit) and designate msbuildexe
If ([System.IntPtr]::Size -ne 4) {
  # 64 bit
  $msbuildexe = 'C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe'
} Else {
  # 32 bit
  $msbuildexe = 'C:\Program Files\MSBuild\14.0\Bin\msbuild.exe'
}
$args = "msbuild.proj /nologo /t:wix `
 /nodeReuse:false `
 /p:TargetPlatform=$targetplatform `
 /p:Platform=$platform `
 /p:DisplayVersion=$displayversion `
 /p:InternalVersion=$internalversion `
 /p:PythonVersion=$pythonversion"
Start-Process $msbuildexe -ArgumentList "$args" -Wait -NoNewWindow -PassThru

if (-not ($?)) {exit(1)}

dir wix.d\MinionMSI\bin\Release\*.msi
