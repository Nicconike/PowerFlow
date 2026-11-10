# Uninstall-Task.ps1
# Unregisters the PowerFlow scheduled task.

[CmdletBinding()]
param(
    [string]$TaskName = "PowerFlow"
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "Stopping and removing Scheduled Task '$TaskName'..." -ForegroundColor Cyan

Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue | Out-Null
Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null

Write-Host "PowerFlow task unregistered successfully." -ForegroundColor Green
