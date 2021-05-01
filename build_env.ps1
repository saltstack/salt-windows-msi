###
###  Downloads software to c:\salt_msi_resources
###
Set-PSDebug -Strict
Set-strictmode -version latest

Import-Module $PSScriptRoot\setupUtil.psm1


#### Ensure path exists
####
$salt_msi_resources = "c:/salt_msi_resources"
# mkdir must be guarded, and I don't want to send the result of is-dir to console
$ignore_bool = (Test-Path -Path $salt_msi_resources) -Or (New-Item -ItemType directory -Path $salt_msi_resources)


#### Ensure resources are installed or get them
####

## Which Microsoft Visual C++ compiler to use with a specific Python version?
## See Product.md

## VC++ Runtime 2015
VerifyOrDownload "c:/salt_msi_resources/Microsoft_VC140_CRT_x64.msm" `
    "http://repo.saltstack.com/windows/dependencies/64/Microsoft_VC140_CRT_x64.msm" `
    "E1344D5943FB2BBB7A56470ED0B7E2B9B212CD9210D3CC6FA82BC3DA8F11EDA8"

VerifyOrDownload "c:/salt_msi_resources/Microsoft_VC140_CRT_x86.msm" `
    "http://repo.saltstack.com/windows/dependencies/32/Microsoft_VC140_CRT_x86.msm" `
    "0D36CFE6E9ABD7F530DBAA4A83841CDBEF9B2ADCB625614AF18208FDCD6B92A4"
