Set-PSDebug -Strict
Set-strictmode -version latest

$scrambled_salt_upgradecode = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\2A3BF6CFED569A14DA191DA004B26D14'
if (Test-Path $scrambled_salt_upgradecode) {
  Write-Host -ForegroundColor Red Salt must not be installed
  exit 1
}

if (Test-Path "C:\ProgramData\Salt Project\Salt") {
  Write-Host -ForegroundColor Red C:\ProgramData\Salt Project\Salt must not exist
  exit 1
}

if (Test-Path "C:\Salt") {
  Write-Host -ForegroundColor Red C:\Salt must not exist
  exit 1
}

if (Test-Path *.output) {
  Write-Host -ForegroundColor Red *.output must not exist
  exit 1
}


(New-Item -ItemType directory -Path "C:\ProgramData\Salt Project\Salt\conf") | out-null

$msis = Get-ChildItem ..\..\*.msi
$msi = $msis[0]
Write-Host -ForegroundColor Yellow Testing ([System.IO.Path]::GetFileName($msi))
Copy-Item -Path $msi -Destination "test.msi"

foreach ($batchfile in Get-ChildItem *.bat){
  $test_name = $batchfile.basename
  $config_input = $test_name + ".input"
  Write-Host -ForegroundColor Yellow -NoNewline ("{0,-55}" -f $test_name)
  if(Test-Path $config_input){
    Copy-Item -Path $config_input -Destination "C:\ProgramData\Salt Project\Salt\conf\minion"
  }

  $params = @{
    "FilePath" = "$Env:SystemRoot\system32\cmd.exe"
    "ArgumentList" = @(
      "/C"
      "$batchfile"
    )
    "Verb" = "runas"
    "PassThru" = $true
  }
  $exe_handling = start-process @params -WindowStyle hidden
  $exe_handling.WaitForExit()
  if (-not $?) {
    Write-Host -ForegroundColor Red "Install failed"
    exit 1
  }

  $expected = $test_name + ".expected"
  $output = $test_name + ".output"
  Copy-Item -Path "C:\ProgramData\Salt Project\Salt\conf\minion" -Destination $output

   if((Get-Content -Raw $expected) -eq (Get-Content -Raw $output)){
    Remove-Item $output
    Write-Host -ForegroundColor Green Config as expected
  } else {
    Write-Host -ForegroundColor Red Config is not as expected
  }


  $params = @{
    "FilePath" = "$Env:SystemRoot\system32\msiexec.exe"
    "ArgumentList" = @(
      "/X"
      "test.msi"
      "/qb"
      "/l*v"
      "$test_name.uninstall.log"
    )
    "Verb" = "runas"
    "PassThru" = $true
  }
  $exe_handling = start-process @params
  $exe_handling.WaitForExit()
  if (-not $?) {
    Write-Host -ForegroundColor Red "Uninstall failed"
    exit 1
  }

  Write-Host "    config exists after Uninstall " (Test-Path "C:\ProgramData\Salt Project\Salt\conf\minion")

  # Clean up system from the last test config and create an empty dir
  if (Test-Path "C:\ProgramData\Salt Project\Salt") {
    Remove-Item "C:\ProgramData\Salt Project\Salt" -Recurse -Force
  }
  (New-Item -ItemType directory -Path "C:\ProgramData\Salt Project\Salt\conf") | out-null

}

# Clean up system
if (Test-Path "C:\ProgramData\Salt Project\Salt") {
  Remove-Item "C:\ProgramData\Salt Project\Salt" -Recurse -Force
}
Remove-Item test.msi
