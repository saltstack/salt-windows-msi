# Remove salt libraries

# Remove python packages
#         Pythonwin - Python IDE and GUI Framework for Windows.
$tmp = "c:\tmp"
$src = "c:\git"
$sp  = "salt\pkg\windows\buildenv\bin\Lib\site-packages"
if (-not (Test-Path "$tmp\$sp")) {mkdir "$tmp\$sp"}

function SMove ($thing) {
    if (Test-Path "$src\$thing") {Move-Item "$src\$thing" "$tmp\$thing"}
}

SMove "$sp\pythonwin"



