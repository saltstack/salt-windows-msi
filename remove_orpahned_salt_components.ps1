# The msi must remove all components.
# But the msi uninstall log may contain
#               disallowing uninstallation of component:   ......     since another client exists
# After msi uninstall, run this to remove
#
# https://stackoverflow.com/questions/26739524/wix-toolset-complete-cleanup-after-disallowing-uninstallation-of-component-sin

# S-1-5-18	Local System	A service account that is used by the operating system.
$components = Get-ChildItem -Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\
$removed = 0
$unremoved = 0
$each50 = 0
foreach ($c in $components) {
    foreach($p in $c.Property) {
        $propValue = (Get-ItemProperty "Registry::$($c.Name)" -Name "$($p)")."$($p)"
        if ($propValue -match '^C:\\salt\\') {
            if ($each50++ -eq 50) {
                Write-Output $propValue
                $each50 = 0
            }
            Remove-Item "Registry::$($c.Name)" -Recurse
            $removed++
        } else {
            $unremoved++
        }
    }
}

Write-Host "$($removed) Local System component(s) pointed to (manually removed) C:\salt files and removed, left $($unremoved) unremoved"

