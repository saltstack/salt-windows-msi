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


$msis = Get-ChildItem ..\..\*.msi

$nof_msis = ($msis | Measure-Object).Count

if ($nof_msis -eq 0) {
  Write-Host -ForegroundColor Red *.msi must exist
  exit 1
}

if ($nof_msis -gt 1) {
  Write-Host -ForegroundColor Red Only one *.msi must exist
  exit 1
}


(New-Item -ItemType directory -Path "C:\ProgramData\Salt Project\Salt\conf") | out-null

$msi = $msis[0]
Write-Host -ForegroundColor Yellow Testing ([System.IO.Path]::GetFileName($msi))
Copy-Item -Path $msi -Destination "test.msi"

$array_allowed_test_words = "dormant", "properties"
foreach ($testfilename in Get-ChildItem *.test){
  $dormant = $false
  $test_name = $testfilename.basename
  $batchfile = $test_name + ".bat"
  $config_input = $test_name + ".input"
  $minion_id = $test_name + ".minion_id"
  Write-Host -ForegroundColor Yellow -NoNewline ("{0,-55}" -f $test_name)

  foreach($line in Get-Content $testfilename) {
  if ($line.Length -eq 0) {continue}
  $words = $line -split " " , 2
  $head = $words[0]
  if ($words.length -eq 2){
    $tail = $words[1]
  } else {
    $tail = ""
  }
  if($array_allowed_test_words.Contains($head)){
    if ($head -eq "dormant") {
      $dormant = $true
    }
    if ($head -eq "properties") {
      Set-Content -Path $batchfile -Value "msiexec /i $msi $tail /l*v $test_name.install.log /qb"
    }
  } else {
    Write-Host -ForegroundColor Red $testfilename must not contain $head
    exit 1
    }
}

  if(Test-Path $config_input){
    Copy-Item -Path $config_input -Destination "C:\ProgramData\Salt Project\Salt\conf\minion"
  }
  if(Test-Path $minion_id){
    Copy-Item -Path $minion_id -Destination "C:\ProgramData\Salt Project\Salt\conf\minion_id"
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
    Write-Host -ForegroundColor Green -NoNewline content Pass
  } else {
    Write-Host -ForegroundColor Red -NoNewline content Fail
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

#  Write-Host "    config exists after Uninstall $dormant  " (Test-Path "C:\ProgramData\Salt Project\Salt\conf\minion")
  if($dormant -eq (Test-Path "C:\ProgramData\Salt Project\Salt\conf\minion")){
    Write-Host -ForegroundColor Green " dormancy Pass"
  } else {
    Write-Host -ForegroundColor Red " dormancy Fail"
  }


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
