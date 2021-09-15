###
###  Downloads software
###
Set-PSDebug -Strict
Set-strictmode -version latest

Import-Module $PSScriptRoot\setupUtil.psm1


#### Ensure path exists
####
$WEBCACHE_DIR = "$pwd\_cache.dir"
$ignore_bool = (Test-Path -Path $WEBCACHE_DIR) -Or (New-Item -ItemType directory -Path $WEBCACHE_DIR)


#### Ensure resources are installed or get them
####

## Which Microsoft Visual C++ compiler to use with a specific Python version?
## See Product.md

## VC++ Runtime 2015
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC140_CRT_x64.msm" `
    "http://repo.saltstack.com/windows/dependencies/64/Microsoft_VC140_CRT_x64.msm" `
    "E1344D5943FB2BBB7A56470ED0B7E2B9B212CD9210D3CC6FA82BC3DA8F11EDA8"

VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC140_CRT_x86.msm" `
    "http://repo.saltstack.com/windows/dependencies/32/Microsoft_VC140_CRT_x86.msm" `
    "0D36CFE6E9ABD7F530DBAA4A83841CDBEF9B2ADCB625614AF18208FDCD6B92A4"

## VC++ Runtime 2013
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC120_CRT_x64.msm" `
    "http://repo.saltstack.com/windows/dependencies/64/Microsoft_VC120_CRT_x64.msm" `
    "15FD10A495287505184B8913DF8D6A9CA461F44F78BC74115A0C14A5EDD1C9A7"
VerifyOrDownload "$pwd\_cache.dir\Microsoft_VC120_CRT_x86.msm" `
    "http://repo.saltstack.com/windows/dependencies/32/Microsoft_VC120_CRT_x86.msm" `
    "26340B393F52888B908AC3E67B935A80D390E1728A31FF38EBCEC01117EB2579"
