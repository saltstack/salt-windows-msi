function VerifyFileContentByHashsum ($local_file, $SHA256) {
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
        Write-Host -ForegroundColor Green "  Downloading $URL"
        Write-Host -ForegroundColor Green "  To          $local_file"
        [Net.ServicePointManager]::SecurityProtocol = "tls12, tls11, tls"
        (New-Object System.Net.WebClient).DownloadFile($URL, $local_file)
    }
    VerifyFileContentByHashsum $local_file $SHA256
}

function ProductcodeExists($productCode) {
    Test-Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$productCode
}
