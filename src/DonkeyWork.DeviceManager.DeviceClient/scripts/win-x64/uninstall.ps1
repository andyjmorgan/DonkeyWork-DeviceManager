# DonkeyWork Device Manager Client - Windows Uninstallation Script
# Removes the device client Windows Service

#Requires -RunAsAdministrator

param(
    [string]$ServiceName = "DonkeyWorkDeviceClient",
    [string]$InstallPath = "C:\Program Files\DonkeyWork\DeviceClient",
    [switch]$KeepConfig,
    [switch]$Help
)

function Show-Help {
    Write-Host @"
DonkeyWork Device Manager Client - Windows Uninstallation

USAGE:
    .\uninstall.ps1 [-ServiceName <name>] [-InstallPath <path>] [-KeepConfig]

OPTIONS:
    -ServiceName    Windows service name (default: DonkeyWorkDeviceClient)
    -InstallPath    Installation directory (default: C:\Program Files\DonkeyWork\DeviceClient)
    -KeepConfig     Keep configuration and token files
    -Help           Show this help message

EXAMPLES:
    .\uninstall.ps1
    .\uninstall.ps1 -KeepConfig
    .\uninstall.ps1 -ServiceName "MyDeviceClient" -InstallPath "D:\Apps\DeviceClient"

"@
    exit 0
}

if ($Help) {
    Show-Help
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DonkeyWork Device Manager Client" -ForegroundColor Cyan
Write-Host "Windows Uninstallation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop and remove service
Write-Host "[1/2] Stopping and removing Windows Service..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq "Running") {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }

    Write-Host "Removing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service removed successfully." -ForegroundColor Green
    } else {
        Write-Host "WARNING: Failed to remove service completely." -ForegroundColor Yellow
    }
} else {
    Write-Host "Service not found. Skipping..." -ForegroundColor Yellow
}

# Step 2: Remove installation directory
Write-Host "[2/2] Removing installation files..." -ForegroundColor Yellow
if (Test-Path $InstallPath) {
    if ($KeepConfig) {
        Write-Host "Keeping configuration files as requested." -ForegroundColor Yellow

        # Backup config files
        $configBackupPath = Join-Path $env:TEMP "DonkeyWorkDeviceClient_Backup"
        New-Item -Path $configBackupPath -ItemType Directory -Force | Out-Null

        $configFiles = @("appsettings.json", "device-tokens.json")
        foreach ($configFile in $configFiles) {
            $sourcePath = Join-Path $InstallPath $configFile
            if (Test-Path $sourcePath) {
                Copy-Item -Path $sourcePath -Destination $configBackupPath -Force
                Write-Host "Backed up: $configFile to $configBackupPath" -ForegroundColor Green
            }
        }
    }

    Remove-Item -Path $InstallPath -Recurse -Force
    Write-Host "Installation directory removed." -ForegroundColor Green
} else {
    Write-Host "Installation directory not found. Skipping..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($KeepConfig) {
    Write-Host "Configuration files backed up to:" -ForegroundColor Yellow
    Write-Host "$configBackupPath" -ForegroundColor White
    Write-Host ""
}

Write-Host "The DonkeyWork Device Manager Client has been removed from your system." -ForegroundColor White
