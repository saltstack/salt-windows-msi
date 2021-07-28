# This msi installer requires an Py3 NSIS exe. e.g.:
#   Salt-Minion-3002.6-Py3-AMD64-Setup.exe
#   Salt-Minion-3002.6-Py3-x86-Setup.exe
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
Write-Host -ForegroundColor Yellow "Display version   $displayversion"
Write-Host -ForegroundColor Yellow "Internal version  $internalversion"

# # # Detecting target platform from NSIS exe # # #
$targetplatform = 0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*AMD64*.exe) {$targetplatform="64"}
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*x86*.exe)   {$targetplatform="32"}
if ($targetplatform -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine target platform from ..\salt\pkg\windows\installer\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}
Write-Host -ForegroundColor Yellow "Architecture      $targetplatform"

# # # Detecting Python version from NSIS exe # # #
$pythonversion = 0
if (Test-Path ..\salt\pkg\windows\installer\Salt-Minion*Py3*.exe) {$pythonversion=3}
if ($pythonversion -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine Python version from ..\salt\pkg\windows\installer\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}


# # # build # # #
# Product related values
$MANUFACTURER   = "Salt Project"
$PRODUCT        = "Salt Minion"
$PRODUCTFILE    = "Salt-Minion-$displayversion"
$PRODUCTDIR     = "Salt"
$VERSION        = $internalversion
$DISCOVER_INSTALLDIR = "..\salt\pkg\windows\buildenv", "..\salt\pkg\windows\buildenv"
$DISCOVER_CONFIGDIR  = "..\salt\pkg\windows\buildenv\conf"

$msbuild = "C:\Program Files (x86)\MSBuild\14.0\"    # MSBuild only needed to compile C#

# MSI related arrays for 64 and 32 bit values, selected by targetplatform
if ($targetplatform -eq "32") {$i = 1} else {$i = 0}
$WIN64        = "yes",                  "no"                   # Used in wxs
$ARCHITECTURE = "x64",                  "x86"                  # WiX dictionary values
$ARCH_AKA     = "AMD64",                "x86"                  # For filename
$PLATFORM     = "x64",                  "Win32"
$PROGRAMFILES = "ProgramFiles64Folder", "ProgramFilesFolder"   # msi dictionary values

function CheckExitCode() {   # Exit on failure
    if ($LastExitCode -ne 0) {
        if (Test-Path build.tmp -PathType Leaf) {
            Get-Content build.tmp
            Remove-Item build.tmp
        }
        Write-Host -ForegroundColor Red "Failed"
        exit(1)
    }
    if (Test-Path build.tmp -PathType Leaf) {
        Remove-Item build.tmp
    }
}


Write-Host -ForegroundColor Yellow "Compiling    *.cs to *.dll"
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
    /reference:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.ServiceProcess.dll" `
    /reference:"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Management.dll" `
    /nowarn:"1701,1702" `
    /out:CustomAction01.dll `
    CustomAction01.cs CustomAction01Util.cs Properties\AssemblyInfo.cs
Pop-Location
CheckExitCode


Write-Host -ForegroundColor Yellow "Packaging    *.dll's to *.CA.dll"
# MakeSfxCA creates a self-extracting managed MSI CA DLL because
# the custom action dll will run in a sandbox and needs all dll inside. This adds 700 kB.
# Because MakeSfxCA cannot check if Wix will reference a non existing procedure, you must double check yourself.
# Usage: MakeSfxCA <outputca.dll> SfxCA.dll <inputca.dll> [support files ...]
& "$($ENV:WIX)sdk\MakeSfxCA.exe" `
    "$pwd\CustomAction01\CustomAction01.CA.dll" `
    "$($ENV:WIX)sdk\x86\SfxCA.dll" `
    "$pwd\CustomAction01\CustomAction01.dll" `
    "$($ENV:WIX)SDK\Microsoft.Deployment.WindowsInstaller.dll" `
    "$($ENV:WIX)bin\wix.dll" `
    "$($ENV:WIX)bin\Microsoft.Deployment.Resources.dll" `
    "$pwd\CustomAction01\CustomAction.config" > build.tmp
CheckExitCode


Write-Host -ForegroundColor Yellow "Discovering  $($DISCOVER_INSTALLDIR[$i]) for INSTALLDIR to *$($ARCHITECTURE[$i])*.wxs"
# move conf folder up one dir because it must not be discoverd twice and xslt is difficult
Move-Item $DISCOVER_CONFIGDIR $DISCOVER_CONFIGDIR\..\..\temporarily_moved_conf_folder
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
# Selectively delete Guid ,so files remain on uninstall.
& "$($ENV:WIX)bin\heat" dir "$($DISCOVER_INSTALLDIR[$i])" -out "Product-discovered-files-$($ARCHITECTURE[$i]).wxs" `
   -cg DiscoveredBinaryFiles -var var.DISCOVER_INSTALLDIR `
   -dr INSTALLDIR -t Product-discover-files.xsl `
   -nologo -indent 1 -gg -sfrag -sreg -suid -srd -ke -template fragment
Move-Item $DISCOVER_CONFIGDIR\..\..\temporarily_moved_conf_folder $DISCOVER_CONFIGDIR
CheckExitCode

# Config shall remain, so delete all Guid (TODO)
#  workaround remove -suid because heat cannot keep id's unique from previous run
Write-Host -ForegroundColor Yellow "Discovering  $DISCOVER_CONFIGDIR for CONFDIR to *.wxs"
& "$($ENV:WIX)bin\heat" dir "$DISCOVER_CONFIGDIR" -out "Product-discovered-files-config.wxs" `
   -cg DiscoveredConfigFiles -var var.DISCOVER_CONFIGDIR `
   -dr CONFDIR -t Product-discover-files-config.xsl `
   -nologo -indent 1 -gg -sfrag -sreg -srd -ke -template fragment
CheckExitCode

Write-Host -ForegroundColor Yellow "Compiling    *.wxs to $($ARCHITECTURE[$i]) *.wixobj"
# Options see "%wix%bin\candle"
& "$($ENV:WIX)bin\candle.exe" -nologo -sw1150 `
    -arch $ARCHITECTURE[$i] `
    -dWIN64="$($WIN64[$i])" `
    -dPROGRAMFILES="$($PROGRAMFILES[$i])" `
    -dMANUFACTURER="$MANUFACTURER" `
    -dPRODUCT="$PRODUCT" `
    -dPRODUCTDIR="$PRODUCTDIR" `
    -dDisplayVersion="$displayversion" `
    -dInternalVersion="$internalversion" `
    -dDISCOVER_INSTALLDIR="$($DISCOVER_INSTALLDIR[$i])" `
    -dDISCOVER_CONFIGDIR="$DISCOVER_CONFIGDIR" `
    -ext "$($ENV:WIX)bin\WixUtilExtension.dll" `
    -ext "$($ENV:WIX)bin\WixUIExtension.dll" `
    -ext "$($ENV:WIX)bin\WixNetFxExtension.dll" `
    "Product.wxs" "Product-discovered-files-$($ARCHITECTURE[$i]).wxs" "Product-discovered-files-config.wxs" > build.tmp
CheckExitCode

Write-Host -ForegroundColor Yellow "Linking      *.wixobj and *.CA.dll to $PRODUCT-$VERSION-$($ARCH_AKA[$i]).msi"
# Options https://wixtoolset.org/documentation/manual/v3/overview/light.html
& "$($ENV:WIX)bin\light"  -nologo `
    -out "$pwd\$PRODUCTFILE-Py$pythonversion-$($ARCH_AKA[$i]).msi" `
    -dDISCOVER_INSTALLDIR="$($DISCOVER_INSTALLDIR[$i])" `
    -dDISCOVER_CONFIGDIR="$DISCOVER_CONFIGDIR" `
    -ext "$($ENV:WIX)bin\WixUtilExtension.dll" `
    -ext "$($ENV:WIX)bin\WixUIExtension.dll" `
    -ext "$($ENV:WIX)bin\WixNetFxExtension.dll" `
    -spdb `
    -sw1076 `
    -sice:ICE03 `
    -cultures:en-us `
    "Product.wixobj" "Product-discovered-files-$($ARCHITECTURE[$i]).wixobj" "Product-discovered-files-config.wixobj"
CheckExitCode

Remove-Item *.wixobj

Write-Host -ForegroundColor Green "Done "
