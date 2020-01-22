###
###  Downloads software to c:\salt_msi_resources
###
Set-PSDebug -Strict
Set-strictmode -version latest

function Verify ($local_file, $SHA256) {
    if (-Not (Test-Path $local_file)) {
        Write-Host  -ForegroundColor Red "$local_file   MISSING"
        exit -2
    }
    if ((Get-FileHash $local_file).Hash -eq $SHA256) {
        Write-Host -ForegroundColor Green "Found $local_file"
        } else {
        Write-Host  -ForegroundColor Red "$local_file   UNEXPECTED HASH   $((Get-FileHash $local_file).Hash)"
        exit -2
    }
}


function OptionallyDownloadAndVerify ($local_file, $URL, $SHA256) {
    if (-Not (Test-Path $local_file)) {
        "Downloading...  $URL"
        [Net.ServicePointManager]::SecurityProtocol = "tls12, tls11, tls"
        (New-Object System.Net.WebClient).DownloadFile($URL, $local_file)
    }
    Verify $local_file $SHA256
}


#### Ensure path exists
####
$salt_msi_resources = "c:/salt_msi_resources"
# mkdir must be guarded, and I don't want to send the result of is-dir to console
$ignore_bool = (Test-Path -Path $salt_msi_resources) -Or (New-Item -ItemType directory -Path $salt_msi_resources)


#### Ensure resources are installed or get them
####

## Which Microsoft Visual C++ compiler to use with a specific Python version?
## See https://wiki.python.org/moin/WindowsCompilers

## VC++ Runtime merge module from Visual Studio 2008 SP2. Required by Python 2.7 
$f = "c:/salt_msi_resources/Microsoft_VC90_CRT_x86_x64.msm"
$u = "http://repo.saltstack.com/windows/dependencies/Microsoft_VC90_CRT_x86_x64.msm"
$h = "A3CE9F8B524E8EEE31CD0487DEAD3A89BFA9721D660FDCE6AC56B59819E17917"
OptionallyDownloadAndVerify $f $u $h

## VC++ Runtime merge module from Visual Studio 2015 SP?. Required by Python 3.5, 3.6, 3.7, 3.8 
$f = "c:/salt_msi_resources/Microsoft_VC140_CRT_x64.msm"
$u = "http://repo.saltstack.com/windows/dependencies/64/Microsoft_VC140_CRT_x64.msm"
$h = "E1344D5943FB2BBB7A56470ED0B7E2B9B212CD9210D3CC6FA82BC3DA8F11EDA8"
OptionallyDownloadAndVerify $f $u $h

$f = "c:/salt_msi_resources/Microsoft_VC140_CRT_x86.msm"
$u = "http://repo.saltstack.com/windows/dependencies/32/Microsoft_VC140_CRT_x86.msm"
$h = "0D36CFE6E9ABD7F530DBAA4A83841CDBEF9B2ADCB625614AF18208FDCD6B92A4"
OptionallyDownloadAndVerify $f $u $h


## You get WiX from https://wixtoolset.org/releases
##
## Wix 3.11.1  released Dec 31, 2017
## Wix 3.11.2  released Sep 19, 2019 (Unneeded protection against maliciously crafted cabinet or zip files) 

##
## You must edit the hard reference to the WiX version in file
##          wix.d/MinionConfigurationExtension/MinionConfigurationExtension.csproj
##    <Reference Include="wix">
##      <HintPath>c:\Program Files (x86)\WiX Toolset v3.11\bin\wix.dll</HintPath>
##    </Reference>
##
if (Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{AA06E868-267F-47FB-86BC-D3D62305D7F4}") {
    Write-Host -ForegroundColor Green "Wix 3.11.1 is installed"
} else {
    $dotnet3state = (Get-WindowsOptionalFeature -Online -FeatureName "NetFx3").State
    $dotnet3enabled = $dotnet3state -Eq "Enabled"
    if (-Not ($dotnet3enabled)) {
        Write-Host -ForegroundColor Red "To use WiX 3.11 on Windows 10, you need to enable .Net Framework 3.5"
        Start-Process optionalfeatures -Wait -NoNewWindow
    }
    
    $wixInstaller = "c:/salt_msi_resources/wix311.exe"
    $u = "https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe"
    $h = "7CAECC9FFDCDECA09E211AA20C8DD2153DA12A1647F8B077836B858C7B4CA265"
    OptionallyDownloadAndVerify $WixInstaller $u $h
    
    Start-Process $wixInstaller -Wait -NoNewWindow
}


## Build tools 2015  
#  Account for Build tools 2015 bugfix upgrade 
#  14.0.23107    from link     {8C918E5B-E238-401F-9F6E-4FB84B024CA2}   Appears in appwiz.cpl
#  14.0.25420    from where?   {79750C81-714E-45F2-B5DE-42DEF00687B8}   Doesn't appear in appwiz.cpl
if ((Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{8C918E5B-E238-401F-9F6E-4FB84B024CA2}") -or  
    (Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{79750C81-714E-45F2-B5DE-42DEF00687B8}")) {
    Write-Host -ForegroundColor Green "Build Tools are installed"
} else {
    $BuildToolsInstaller = "c:/salt_msi_resources/BuildTools_Full.exe"
    $u = "https://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe"
    $h = "92CFB3DE1721066FF5A93F14A224CC26F839969706248B8B52371A8C40A9445B"
    OptionallyDownloadAndVerify $BuildToolsInstaller $u $h
    Write-Host -ForegroundColor Yellow "------------------------------------"
    Write-Host -ForegroundColor Yellow "-- Please install the Build Tools --"
    Write-Host -ForegroundColor Yellow "------------------------------------"
    Start-Process $BuildToolsInstaller -Wait -NoNewWindow
}
