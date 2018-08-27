
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


function VerifyOrDownload ($local_file, $URL, $SHA256) {
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
$ingnore_bool = (Test-Path -Path $salt_msi_resources) -Or (New-Item -ItemType directory -Path $salt_msi_resources)


#### Ensure resources are present
####

## Python 2.7 requires VC++ Runtime merge module from Visual Studio 2008
$f = "c:/salt_msi_resources/Microsoft_VC90_CRT_x86_x64.msm"
$h = "D5B4E8B100D2A9A6A756BB6D70DF67203E3B611F49866F84944607DC096E4AFE"
Verify $f $h



#### Ensure resources are installed or build environment
###

## Wix 3.11
## http://wixtoolset.org/releases/
if (Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{AA06E868-267F-47FB-86BC-D3D62305D7F4}") {
    Write-Host -ForegroundColor Green "Found Wix"
} else {
    $dotnet3state = (Get-WindowsOptionalFeature -Online -FeatureName "NetFx3").State
    $dotnet3enabled = $dotnet3state -Eq "Enabled"
    if (-Not ($dotnet3enabled)) {
	"To use WiX 3.11 on Windows 10, you need to enable .Net Framework 3.5"
	Start-Process optionalfeatures -Wait -NoNewWindow
    }
    
    $wixInstaller = "c:/salt_msi_resources/wix311.exe"
    $u = "https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe"
    $h = "7CAECC9FFDCDECA09E211AA20C8DD2153DA12A1647F8B077836B858C7B4CA265"
    VerifyOrDownload $WixInstaller $u $h
    
    Start-Process $wixInstaller -Wait -NoNewWindow
}


## Build tools 2015
## https://www.microsoft.com/en-US/download/confirmation.aspx?id=48159
if (Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{8C918E5B-E238-401F-9F6E-4FB84B024CA2}") {
    Write-Host -ForegroundColor Green "Found Build Tools"
} else {
    $BuildToolsInstaller = "c:/salt_msi_resources/BuildTools_Full.exe"
    $u = "https://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe"
    $h = "92CFB3DE1721066FF5A93F14A224CC26F839969706248B8B52371A8C40A9445B"
    VerifyOrDownload $BuildToolsInstaller $u $h
    
    Start-Process $BuildToolsInstaller -Wait -NoNewWindow
}
