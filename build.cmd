PowerShell Set-MpPreference -DisableRealtimeMonitoring $true
PowerShell -ExecutionPolicy RemoteSigned -File build.cmd.ps1
PowerShell Set-MpPreference -DisableRealtimeMonitoring $false
