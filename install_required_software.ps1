
function VerifyOrDownload ($local_file, $URL, $SHA256) {
  if (Test-Path $local_file) {
    if ((Get-FileHash $local_file).Hash -eq $SHA256) {
      "Downloaded      $local_file"
    } else {
      Write-Host "$local_file   UNEXPECTED HASH   $((Get-FileHash $local_file).Hash)" -ForegroundColor Red
      exit -2
    }
  } else {
    "Downloading...  $URL"
    [Net.ServicePointManager]::SecurityProtocol = "tls12, tls11, tls"
    (New-Object System.Net.WebClient).DownloadFile($URL, $local_file)
  }
}


#### Download

# Wix
# http://wixtoolset.org/releases/
$wixInstaller = "c:/msi/wix311.exe"
$u = "https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe"
$h = "7CAECC9FFDCDECA09E211AA20C8DD2153DA12A1647F8B077836B858C7B4CA265"
VerifyOrDownload $WixInstaller $u $h


# Build tools 2015
# https://www.microsoft.com/en-US/download/confirmation.aspx?id=48159
$BuildToolsInstaller = "c:/msi/BuildTools_Full.exe"
$u = "https://download.microsoft.com/download/E/E/D/EEDF18A8-4AED-4CE0-BEBE-70A83094FC5A/BuildTools_Full.exe"
$h = "92CFB3DE1721066FF5A93F14A224CC26F839969706248B8B52371A8C40A9445B"
VerifyOrDownload $BuildToolsInstaller $u $h



#### Execute
"Is .Net 3.5 enabled on Windows 10 for WiX 3.11?"
Start-Process optionalfeatures -Wait -NoNewWindow 

#$p = Start-Process msiexec.exe -ArgumentList "/i $file /quiet ALLUSERS=1" -Wait -NoNewWindow -PassThru
$p = Start-Process $wixInstaller         -Wait -NoNewWindow -PassThru
"Wix Installer returned $($p.ExitCode)"

$p = Start-Process $BuildToolsInstaller  -Wait -NoNewWindow -PassThru
"Build Tools Installer returned $($p.ExitCode)"

