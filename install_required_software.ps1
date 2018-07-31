
function VerifyOrDownload ($local_file, $URL, $SHA256) {
  if (-Not (Test-Path $local_file)) {
    "Downloading...  $URL"
    [Net.ServicePointManager]::SecurityProtocol = "tls12, tls11, tls"
    (New-Object System.Net.WebClient).DownloadFile($URL, $local_file)
  }
  if ((Get-FileHash $local_file).Hash -eq $SHA256) {
    Write-Host -ForegroundColor Green "Founded $local_file"
  } else {
    Write-Host  -ForegroundColor Red "$local_file   UNEXPECTED HASH   $((Get-FileHash $local_file).Hash)"
    exit -2
  }
}

#### Path
if(Test-Path -Path "c:/msi" ){
  Write-Host -ForegroundColor Green  "Found c:/msi"
} else {
  New-Item -ItemType directory -Path "c:/msi"
}

#### Resources

### Merge module VC Runtime for Python 2.7
$f = "c:/saltrepo_local_cache/64/Microsoft_VC90_CRT_x86_x64.msm"
$u = "https://repo.saltstack.com/windows/dependencies/64/Microsoft_VC90_CRT_x86_x64.msm"
$h = "D5B4E8B100D2A9A6A756BB6D70DF67203E3B611F49866F84944607DC096E4AFE"
VerifyOrDownload $f $u $h


#### Build environment

## Wix 3.11
## http://wixtoolset.org/releases/
if (Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\"{AA06E868-267F-47FB-86BC-D3D62305D7F4}") {
  Write-Host -ForegroundColor Green "Found Wix"
} else {
  "For WiX 3.11 on Windows 10: is .Net 3.5 enabled?"
  Start-Process optionalfeatures -Wait -NoNewWindow 

  $wixInstaller = "c:/saltrepo_local_cache/64/wix311.exe"
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
  $BuildToolsInstaller = "c:/msi/BuildTools_Full.exe"
  $u = "https://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe"
  $h = "92CFB3DE1721066FF5A93F14A224CC26F839969706248B8B52371A8C40A9445B"
  VerifyOrDownload $BuildToolsInstaller $u $h

  Start-Process $BuildToolsInstaller -Wait -NoNewWindow
}
