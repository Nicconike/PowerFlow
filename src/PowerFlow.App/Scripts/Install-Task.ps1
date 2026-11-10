# Install-Task.ps1
# Registers PowerFlow to run automatically with Highest Privileges at logon.

[CmdletBinding()]
param(
    [string]$TaskName = "PowerFlow"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$appDir = Split-Path -Parent $scriptDir
$exePath = Join-Path $appDir "PowerFlow.exe"

if (-not (Test-Path $exePath)) {
    # Check if running from published or bin folder
    $candidate = Join-Path $scriptDir "PowerFlow.exe"
    if (Test-Path $candidate) {
        $exePath = $candidate
        $appDir = $scriptDir
    } else {
        Write-Error "Could not find PowerFlow.exe at $exePath"
        exit 1
    }
}

Write-Host "Registering Scheduled Task '$TaskName' for PowerFlow..." -ForegroundColor Cyan
Write-Host "Target: $exePath"

$action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory $appDir
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit 0

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "PowerFlow successfully registered in Windows Task Scheduler with Highest Privileges." -ForegroundColor Green
Write-Host "PowerFlow will now start automatically at user logon without UAC prompts."
