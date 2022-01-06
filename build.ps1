# This builds the msi installer and requires a NSIS exe installer. e.g.:
#   Salt-Minion-3002.6-Py3-AMD64-Setup.exe
#   Salt-Minion-3002.6-Py3-x86-Setup.exe
#
Set-PSDebug -Strict
Set-strictmode -version latest


# Until 2022 or executed on each dev box: move old cache to new cache dir
if (Test-Path C:\salt_msi_resources) {
  Move-Item C:\salt_msi_resources\* .\_cache.dir -force
  Remove-Item C:\salt_msi_resources
}


#### #### Verify, download or install resources
#################################################################################################################

function VerifyOrDownload ($local_file, $URL, $SHA256) {
  #### Verify or download file
  $filename = Split-Path $local_file -leaf
  Write-Host -ForegroundColor Green -NoNewline ("{0,-38}" -f  $filename)
  if (Test-Path $local_file) {
    if ((Get-FileHash $local_file).Hash -eq $SHA256) {
        Write-Host -ForegroundColor Green " Verified"
      } else {
        Write-Host -ForegroundColor Red " UNEXPECTED HASH   $((Get-FileHash $local_file).Hash)"
        exit -2
      }
    } else {
      [Net.ServicePointManager]::SecurityProtocol = "tls12, tls11, tls"
      (New-Object System.Net.WebClient).DownloadFile($URL, $local_file)
      Write-Host -ForegroundColor Green " Downloaded"
  }
}

function ProductcodeExists($productCode) {
  # Verify product code in registry
  Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$productCode
}


#### Ensure cache dir exists
$WEBCACHE_DIR = "$pwd\_cache.dir"
((Test-Path -Path $WEBCACHE_DIR) -Or (New-Item -ItemType directory -Path $WEBCACHE_DIR)) | out-null


## Verify or install WiX toolset from https://wixtoolset.org/releases
##   Wix 3.11.2  released Sep 19, 2019

# 64bit: {03368010-193D-4AE2-B275-DD2EB32CD427}
# 32bit: {07188017-A460-4C0D-A386-6B3CEB8E20CD}
if ((ProductcodeExists "{03368010-193D-4AE2-B275-DD2EB32CD427}") -or
    (ProductcodeExists "{07188017-A460-4C0D-A386-6B3CEB8E20CD}")) {
    Write-Host -ForegroundColor Green  ("{0,-38} Installed" -f  "Wix 3.11.2")
} else {
    ## Verify or install dotnet 3
    $dotnet3state = (Get-WindowsOptionalFeature -Online -FeatureName "NetFx3").State
    $dotnet3enabled = $dotnet3state -Eq "Enabled"
    if (-Not ($dotnet3enabled)) {
        Write-Host -ForegroundColor Yellow "    ***  Enabling Feature .Net Framework 3.5 (required for or WiX 3) ***"
        Dism /online /enable-feature /featurename:NetFx3 /all
    }

    $wixInstaller = "$WEBCACHE_DIR/wix3-11-2-Setup.exe"
    VerifyOrDownload $wixInstaller `
        "https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311.exe" `
        "32BB76C478FCB356671D4AAF006AD81CA93EEA32C22A9401B168FC7471FECCD2"
    Write-Host -ForegroundColor Yellow "    *** Installing the Wix toolset ***"
    Start-Process $wixInstaller -ArgumentList "/install","/quiet","/norestart" -Wait -NoNewWindow
}
if ($null -eq $ENV:WIX) {
    Write-Host -ForegroundColor Yellow "    *** Please open a new Shell for the Wix enviornment variable ***"
}



## Build tools 2015
#  https://www.microsoft.com/en-us/download/details.aspx?id=48159
#  There is a bugfix upgrade
#  14.0.23107    from link     {8C918E5B-E238-401F-9F6E-4FB84B024CA2}   Appears in appwiz.cpl
#  14.0.25420    from where?   {79750C81-714E-45F2-B5DE-42DEF00687B8}   Doesn't appear in appwiz.cpl

# 64bit (23107): {8C918E5B-E238-401F-9F6E-4FB84B024CA2}
# 32bit (23107): Add it here and to the if statement once we find out what it is
# 64bit (25420): {79750C81-714E-45F2-B5DE-42DEF00687B8}
# 32bit (25420): {6BF8837D-67E1-4359-89FB-C08BFD6F2138}
if ((ProductcodeExists "{8C918E5B-E238-401F-9F6E-4FB84B024CA2}") -or
    (ProductcodeExists "{79750C81-714E-45F2-B5DE-42DEF00687B8}") -or
    (ProductcodeExists "{6BF8837D-67E1-4359-89FB-C08BFD6F2138}")) {
      Write-Host -ForegroundColor Green ("{0,-38} Installed" -f  "Build Tools 2015")
} else {
    $BuildToolsInstaller = "$WEBCACHE_DIR/BuildTools_Full.exe"
    VerifyOrDownload $BuildToolsInstaller `
        "https://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe" `
        "92CFB3DE1721066FF5A93F14A224CC26F839969706248B8B52371A8C40A9445B"

    Write-Host -ForegroundColor Yellow "    *** Installing Microsoft Build Tools 2015 (and wait up to 40 seconds for all processes to end) ***"
    #Start-Process $BuildToolsInstaller -Wait -NoNewWindow   // waits forever (for the "process group"?)
    $p = Start-Process -FilePath $BuildToolsInstaller -ArgumentList "/quiet","/norestart" -PassThru
    $p.WaitForExit()
}


## See Product.md for which Microsoft Visual C++ compiler to use for what

## VC++ Runtime 2013
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC120_CRT_x64.msm" `
    "http://repo.saltproject.io/windows/dependencies/64/Microsoft_VC120_CRT_x64.msm" `
    "15FD10A495287505184B8913DF8D6A9CA461F44F78BC74115A0C14A5EDD1C9A7"
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC120_CRT_x86.msm" `
    "http://repo.saltproject.io/windows/dependencies/32/Microsoft_VC120_CRT_x86.msm" `
    "26340B393F52888B908AC3E67B935A80D390E1728A31FF38EBCEC01117EB2579"

## VC++ Runtime 2015
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC140_CRT_x64.msm" `
    "http://repo.saltproject.io/windows/dependencies/64/Microsoft_VC140_CRT_x64.msm" `
    "E1344D5943FB2BBB7A56470ED0B7E2B9B212CD9210D3CC6FA82BC3DA8F11EDA8"

VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC140_CRT_x86.msm" `
    "http://repo.saltproject.io/windows/dependencies/32/Microsoft_VC140_CRT_x86.msm" `
    "0D36CFE6E9ABD7F530DBAA4A83841CDBEF9B2ADCB625614AF18208FDCD6B92A4"


#### Detecting Salt version from Git
#################################################################################################################
[string]$gitexe = where.exe git
if ($gitexe.length -eq 0) {
  Write-Host -ForegroundColor Red "Please install git"
  exit -1
}

if (-Not (Test-Path ..\salt)) {
  Write-Host -ForegroundColor Red "No directory ..\salt"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
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
Write-Host -ForegroundColor Green "Display version   $displayversion"
Write-Host -ForegroundColor Green "Internal version  $internalversion"

#### Detecting target platform from NSIS exe
$targetplatform = 0
if (Test-Path ..\salt-windows-nsis\build\Salt-Minion*AMD64*.exe) {$targetplatform="64"}
if (Test-Path ..\salt-windows-nsis\build\Salt-Minion*x86*.exe)   {$targetplatform="32"}
if ($targetplatform -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine target platform from ..\salt-windows-nsis\build\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}
Write-Host -ForegroundColor Green "Architecture      $targetplatform"

#### Detecting Python version from NSIS exe
$pythonversion = 0
if (Test-Path ..\salt-windows-nsis\build\Salt-Minion*Py3*.exe) {$pythonversion=3}
if ($pythonversion -eq 0) {
  Write-Host -ForegroundColor Red "Cannot determine Python version from ..\salt-windows-nsis\build\Salt-Minion*.exe"
  Write-Host -ForegroundColor Red "Have you build the NSIS Nullsoft exe installer?"
  exit(1)
}


#### #### Build
#################################################################################################################
# Product related values
$MANUFACTURER   = "Salt Project"
$PRODUCT        = "Salt Minion"
$PRODUCTFILE    = "Salt-Minion-$displayversion"
$PRODUCTDIR     = "Salt"
$VERSION        = $internalversion
$DISCOVER_INSTALLDIR = "..\salt-windows-nsis\scripts\buildenv", "..\salt-windows-nsis\scripts\buildenv"
$DISCOVER_CONFDIR    = "..\salt-windows-nsis\scripts\buildenv\configs"

# MSBuild needed to compile C#
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
$PLATFORM     = "x64",                  "Win32"                # Unused
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


Write-Host -ForegroundColor Yellow "Packaging    *.dll's to *.CA.dll (because InstallService runs in a sandbox)"
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


Write-Host -ForegroundColor Yellow "Discovering  INSTALLDIR from $($DISCOVER_INSTALLDIR[$i]) to *$($ARCHITECTURE[$i])*.wxs"
# move conf folder up one dir because it must not be discoverd twice and xslt is difficult
Move-Item $DISCOVER_CONFDIR $DISCOVER_CONFDIR\..\..\temporarily_moved_conf_folder
# https://wixtoolset.org/documentation/manual/v3/overview/heat.html
# -cg <ComponentGroupName> Component group name (cannot contain spaces e.g -cg MyComponentGroup).
# -sfrag   Suppress generation of fragments for directories and components.
# -var     WiX variable for SourceDir
# -gg      Generate guids now. All components are given a guid when heat is run.
# -sfrag   Suppress generation of fragments for directories and components.
# -sreg    Suppress registry harvesting.
# -srd     Suppress harvesting the root directory as an element.
# -ke      Keep empty directories.
# -dr <DirectoryName>   Directory reference to root directories (cannot contains spaces e.g. -dr MyAppDirRef).
# -t <xsl> Transform harvested output with XSL file.
# Selectively delete Guid ,so files remain on uninstall.
& "$($ENV:WIX)bin\heat" dir "$($DISCOVER_INSTALLDIR[$i])" -out "Product-discovered-files-$($ARCHITECTURE[$i]).wxs" `
   -cg DiscoveredBinaryFiles -var var.DISCOVER_INSTALLDIR `
   -dr INSTALLDIR -t Product-discover-files.xsl `
   -nologo -indent 1 -gg -sfrag -sreg -srd -ke -template fragment
Move-Item $DISCOVER_CONFDIR\..\..\temporarily_moved_conf_folder $DISCOVER_CONFDIR
CheckExitCode

# Config shall remain, so delete all Guid (TODO)
Write-Host -ForegroundColor Yellow "Discovering  CONFDIR    from $DISCOVER_CONFDIR to *.wxs"
& "$($ENV:WIX)bin\heat" dir "$DISCOVER_CONFDIR" -out "Product-discovered-files-config.wxs" `
   -cg DiscoveredConfigFiles -var var.DISCOVER_CONFDIR `
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
    -dWEBCACHE_DIR="$WEBCACHE_DIR" `
    -dDISCOVER_CONFDIR="$DISCOVER_CONFDIR" `
    -ext "$($ENV:WIX)bin\WixUtilExtension.dll" `
    -ext "$($ENV:WIX)bin\WixUIExtension.dll" `
    -ext "$($ENV:WIX)bin\WixNetFxExtension.dll" `
    "Product.wxs" "Product-discovered-files-$($ARCHITECTURE[$i]).wxs" "Product-discovered-files-config.wxs" > build.tmp
CheckExitCode

Write-Host -ForegroundColor Yellow "Linking      $PRODUCT-$VERSION-$($ARCH_AKA[$i]).msi"
# Options https://wixtoolset.org/documentation/manual/v3/overview/light.html
# Supress LGHT1076 ICE82 warnings caused by the VC++ Runtime merge modules
#     https://sourceforge.net/p/wix/mailman/message/22945366/
& "$($ENV:WIX)bin\light"  -nologo `
    -out "$pwd\$PRODUCTFILE-Py$pythonversion-$($ARCH_AKA[$i]).msi" `
    -dDISCOVER_INSTALLDIR="$($DISCOVER_INSTALLDIR[$i])" `
    -dDISCOVER_CONFDIR="$DISCOVER_CONFDIR" `
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
