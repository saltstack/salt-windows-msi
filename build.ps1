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
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*AMD64*.exe) {$targetplatform="64"}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*x86*.exe)   {$targetplatform="32"}
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


# # # build # # #
# Product related values
$MANUFACTURER   = "Salt Project"
$PRODUCT        = "Salt Minion $displayversion"
$PRODUCTFILE    = "Salt-Minion-$displayversion"
$VERSION        = $internalversion
$DISCOVERFOLDER = "..\salt\pkg\windows\buildenv", "..\salt\pkg\windows\buildenv"

# MSBUild needed to compile C#
If ( (Get-CimInstance Win32_OperatingSystem).OSArchitecture -eq "64-bit" ) {
    $msbuild = "C:\Program Files (x86)\MSBuild\14.0\"
} else {
    $msbuild = "C:\Program Files\MSBuild\14.0\"
}

# MSI related arrays for 64 and 32 bit values, selected by targetplatform
if ($targetplatform -eq "32") {$i = 1} else {$i = 0}
$WIN64        = "yes",                  "no"                   # Used in wxs
$ARCHITECTURE = "x64",                  "x86"                  # WiX dictionary values
$ARCH_AKA     = "AMD64",                "x86"                  # For filename
$PLATFORM     = "x64",                  "Win32"
$PROGRAMFILES = "ProgramFiles64Folder", "ProgramFilesFolder"   # msi dictionary values

function CheckExitCode($txt) {   # Exit on failure
    if ($LastExitCode -ne 0) {
        Write-Host -ForegroundColor Red "$txt failed"
        exit(1)
    }
}


Write-Host -ForegroundColor Yellow "Compiling C# custom actions into *.dll"
Push-Location CustomAction01
# Compiler options are exactly those of a wix msbuild project.
# https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options
& "$($msbuild)bin\csc.exe" /nologo `
    /noconfig /nostdlib+ /errorreport:prompt /warn:4 /define:TRACE /highentropyva- `
    /debug:pdbonly /filealign:512 /optimize+ /target:library /utf8output `
    /reference:"$($ENV:WIX)SDK\Microsoft.Deployment.WindowsInstaller.dll" `
    /reference:"$($ENV:WIX)bin\wix.dll" `
    /reference:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll" `
    /reference:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.dll" `
    /reference:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Xml.dll" `
    /nowarn:"1701,1702" `
    /out:CustomAction01.dll `
    CustomAction01.cs CustomAction01Util.cs Properties\AssemblyInfo.cs
Pop-Location
CheckExitCode "Compiling C#"


Write-Host -ForegroundColor Yellow "Packaging *.dlls into *.CA.dll for running in a sandbox"
# MakeSfxCA creates a self-extracting managed MSI CA DLL because
# the custom action dll will run in a sandbox and needs all dll inside. This adds 700 kB.
# Because MakeSfxCA does not check if Wix references a non existing procedure, you must check.
Write-Host -ForegroundColor Blue "Does this search find all your custom action procedures?"
& "$($ENV:WIX)sdk\MakeSfxCA.exe" `
    "$pwd\CustomAction01\CustomAction01.CA.dll" `
    "$($ENV:WIX)sdk\x86\SfxCA.dll" `
    "$pwd\CustomAction01\CustomAction01.dll" `
    "$($ENV:WIX)SDK\Microsoft.Deployment.WindowsInstaller.dll" `
    "$($ENV:WIX)bin\wix.dll" `
    "$($ENV:WIX)bin\Microsoft.Deployment.Resources.dll" `
    "$pwd\CustomAction01\CustomAction.config"
CheckExitCode "Packaging"


Write-Host -ForegroundColor Yellow "Harvesting files from $($DISCOVERFOLDER[$i]) to $($ARCHITECTURE[$i])"
# https://wixtoolset.org/documentation/manual/v3/overview/heat.html
# -cg <ComponentGroupName> Component group name (cannot contain spaces e.g -cg MyComponentGroup).
# -sfrag   Suppress generation of fragments for directories and components.
# -var     WiX variable for SourceDir
# -gg      Generate guids now. All components are given a guid when heat is run.
# -sfrag   Suppress generation of fragments for directories and components.
# -sreg    Suppress registry harvesting.
# -suid    Suppress SILLY unique identifiers for files, components, & directories.
# -srd     Suppress harvesting the root directory as an element.
# -ke      Keep empty directories.
# -dr <DirectoryName>   Directory reference to root directories (cannot contains spaces e.g. -dr MyAppDirRef).
# -t <xsl> Transform harvested output with XSL file.
& "$($ENV:WIX)bin\heat" dir "$($DISCOVERFOLDER[$i])" -out "Product-$($ARCHITECTURE[$i])-discovered-files.wxs" `
   -cg DiscoveredFiles -var var.DISCOVERFOLDER `
   -dr INSTALLFOLDER -t Product-discover-files.xsl `
   -nologo -indent 1 -gg -sfrag -sreg -suid -srd -ke -template fragment


Write-Host -ForegroundColor Yellow "Compiling wxs to $($ARCHITECTURE[$i]) wixobj"
# Options see "%wix%bin\candle"
& "$($ENV:WIX)bin\candle.exe" -nologo -sw1150 `
    -arch $ARCHITECTURE[$i] `
    -dWIN64="$($WIN64[$i])" `
    -dPROGRAMFILES="$($PROGRAMFILES[$i])" `
    -ddist="$($DISCOVERFOLDER[$i])" `
    -dMANUFACTURER="$MANUFACTURER" `
    -dPRODUCT="$PRODUCT" `
    -dDisplayVersion="$displayversion" `
    -dInternalVersion="$internalversion" `
    -dDISCOVERFOLDER="$($DISCOVERFOLDER[$i])" `
    -ext "$($ENV:WIX)bin\WixUtilExtension.dll" `
    -ext "$($ENV:WIX)bin\WixUIExtension.dll" `
    -ext "$($ENV:WIX)bin\WixNetFxExtension.dll" `
    "Product.wxs" "Product-$($ARCHITECTURE[$i])-discovered-files.wxs"
CheckExitCode "candle"

Write-Host -ForegroundColor Yellow "Linking $($ARCHITECTURE[$i]) wixobj to $PRODUCTFILE-Py$pythonversion-$($ARCH_AKA[$i]).msi"
# Options https://wixtoolset.org/documentation/manual/v3/overview/light.html
& "$($ENV:WIX)bin\light"  -nologo `
    -out "$pwd\$PRODUCTFILE-Py$pythonversion-$($ARCH_AKA[$i]).msi" `
    -dDISCOVERFOLDER="$($DISCOVERFOLDER[$i])" `
    -ext "$($ENV:WIX)bin\WixUtilExtension.dll" `
    -ext "$($ENV:WIX)bin\WixUIExtension.dll" `
    -ext "$($ENV:WIX)bin\WixNetFxExtension.dll" `
    -spdb `
    -sw1076 `
    -sice:ICE03 `
    -cultures:en-us `
    "Product.wixobj" "Product-$($ARCHITECTURE[$i])-discovered-files.wixobj"
CheckExitCode "light"

Write-Host -ForegroundColor Green "Done "
