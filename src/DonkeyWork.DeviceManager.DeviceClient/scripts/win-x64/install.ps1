# DonkeyWork Device Manager Client - Windows Installation Script
# Installs the device client as a Windows Service

#Requires -RunAsAdministrator

param(
    [string]$ServiceName = "DonkeyWorkDeviceClient",
    [string]$DisplayName = "DonkeyWork Device Manager Client",
    [string]$Description = "IoT device client for DonkeyWork Device Manager",
    [string]$InstallPath = "C:\Program Files\DonkeyWork\DeviceClient",
    [switch]$Help
)

# Hardcoded API URL
$ApiBaseUrl = "https://devicemanager.donkeywork.dev"

function Show-Help {
    Write-Host @"
DonkeyWork Device Manager Client - Windows Installation

USAGE:
    .\install.ps1 [-InstallPath <path>] [-ServiceName <name>]

OPTIONS:
    -InstallPath    Installation directory (default: C:\Program Files\DonkeyWork\DeviceClient)
    -ServiceName    Windows service name (default: DonkeyWorkDeviceClient)
    -Help           Show this help message

EXAMPLES:
    .\install.ps1
    .\install.ps1 -InstallPath "D:\Apps\DeviceClient"

NOTES:
    - This script must be run as Administrator
    - Self-contained binary includes all dependencies
    - The service will be configured to start automatically
    - API endpoint: https://devicemanager.donkeywork.dev
    - Configuration file will be created at: <InstallPath>\appsettings.json

"@
    exit 0
}

if ($Help) {
    Show-Help
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DonkeyWork Device Manager Client" -ForegroundColor Cyan
Write-Host "Windows Installation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if service already exists
Write-Host "[1/6] Checking for existing service..." -ForegroundColor Yellow
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service '$ServiceName' already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
    Write-Host "Existing service removed." -ForegroundColor Green
}

# Step 2: Backup device tokens (if upgrading)
Write-Host "[2/7] Backing up device tokens (if present)..." -ForegroundColor Yellow
$tokensFile = Join-Path $InstallPath "device-tokens.json"
$tokensBackup = $null
if (Test-Path $tokensFile) {
    $tokensBackup = New-TemporaryFile
    Copy-Item -Path $tokensFile -Destination $tokensBackup.FullName -Force
    Write-Host "Device tokens backed up to temporary location" -ForegroundColor Green
} else {
    Write-Host "No existing device tokens found (fresh install)" -ForegroundColor Yellow
}

# Step 3: Create installation directory
Write-Host "[3/7] Creating installation directory..." -ForegroundColor Yellow
if (Test-Path $InstallPath) {
    Write-Host "Installation directory already exists. Removing old files..." -ForegroundColor Yellow
    Remove-Item -Path $InstallPath -Recurse -Force
}
New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null
Write-Host "Installation directory created at: $InstallPath" -ForegroundColor Green

# Step 4: Install OSQuery (if not already installed)
Write-Host "[4/7] Checking OSQuery installation..." -ForegroundColor Yellow
$osqueryPath = "C:\Program Files\osquery\osqueryi.exe"
if (-not (Test-Path $osqueryPath)) {
    Write-Host "OSQuery not found. Downloading installer..." -ForegroundColor Yellow

    $osqueryInstallerUrl = "https://pkg.osquery.io/windows/osquery-5.11.0.msi"
    $installerPath = Join-Path $env:TEMP "osquery-installer.msi"

    try {
        Invoke-WebRequest -Uri $osqueryInstallerUrl -OutFile $installerPath -UseBasicParsing
        Write-Host "Installing OSQuery..." -ForegroundColor Yellow
        Start-Process msiexec.exe -ArgumentList "/i `"$installerPath`" /quiet /qn /norestart" -Wait -NoNewWindow
        Remove-Item $installerPath -Force

        if (Test-Path $osqueryPath) {
            Write-Host "OSQuery installed successfully." -ForegroundColor Green
        } else {
            Write-Host "WARNING: OSQuery installation completed but executable not found. Query features may not work." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "WARNING: Failed to install OSQuery: $_" -ForegroundColor Yellow
        Write-Host "Please install manually from: https://osquery.io/downloads/official" -ForegroundColor Yellow
    }
}
else {
    Write-Host "OSQuery is already installed." -ForegroundColor Green
}

# Step 5: Copy application files to installation directory
Write-Host "[5/7] Copying application files to installation directory..." -ForegroundColor Yellow
$packageDir = Split-Path -Parent $PSScriptRoot

# Copy the pre-built binary and supporting files (excluding scripts directory and device-tokens.json)
Copy-Item -Path "$packageDir\*" -Destination $InstallPath -Recurse -Force -Exclude "scripts","device-tokens.json"

# Restore device tokens if they were backed up
if ($tokensBackup -and (Test-Path $tokensBackup.FullName)) {
    try {
        Copy-Item -Path $tokensBackup.FullName -Destination $tokensFile -Force -ErrorAction Stop

        # Verify the file was restored and has content
        if ((Test-Path $tokensFile) -and ((Get-Item $tokensFile).Length -gt 0)) {
            Remove-Item -Path $tokensBackup.FullName -Force
            Write-Host "Device tokens restored from backup" -ForegroundColor Green
        } else {
            Write-Host "WARNING: Device tokens restore verification failed. Backup kept at: $($tokensBackup.FullName)" -ForegroundColor Yellow
            Write-Host "Manual intervention required: copy $($tokensBackup.FullName) to $tokensFile" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "ERROR: Failed to restore device tokens. Backup preserved at: $($tokensBackup.FullName)" -ForegroundColor Red
        Write-Host "Manual intervention required: copy $($tokensBackup.FullName) to $tokensFile" -ForegroundColor Red
        Write-Host "Error details: $_" -ForegroundColor Red
    }
}

# Update API URL in appsettings.json if specified
if ($ApiBaseUrl -ne "https://devicemanager.donkeywork.dev") {
    Write-Host "[6/7] Updating API URL in configuration..." -ForegroundColor Yellow
    $configPath = Join-Path $InstallPath "appsettings.json"
    $config = Get-Content $configPath -Raw
    $config = $config -replace 'http://devicemanager.donkeywork.dev', $ApiBaseUrl
    $config = $config -replace 'https://devicemanager.donkeywork.dev', $ApiBaseUrl
    Set-Content -Path $configPath -Value $config -Force
    Write-Host "API URL updated to: $ApiBaseUrl" -ForegroundColor Green
} else {
    Write-Host "[6/7] Configuration file already present" -ForegroundColor Yellow
    Write-Host "Using default API URL: $ApiBaseUrl" -ForegroundColor Green
}

Write-Host "Files copied successfully." -ForegroundColor Green

# Step 7: Install and start Windows Service
Write-Host "[7/7] Installing Windows Service..." -ForegroundColor Yellow
$exePath = Join-Path $InstallPath "DonkeyWork.DeviceManager.DeviceClient.exe"

# Create the service
sc.exe create $ServiceName `
    binPath= "`"$exePath`"" `
    start= auto `
    DisplayName= "$DisplayName"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create Windows Service" -ForegroundColor Red
    exit 1
}

# Set service description
sc.exe description $ServiceName "$Description"

# Configure service recovery options (restart on failure)
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Start the service
Write-Host "Starting service..." -ForegroundColor Yellow
Start-Service -Name $ServiceName

# Wait a moment and check service status
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName
if ($service.Status -eq "Running") {
    Write-Host "Service started successfully!" -ForegroundColor Green
} else {
    Write-Host "WARNING: Service was created but is not running. Status: $($service.Status)" -ForegroundColor Yellow
    Write-Host "Check Windows Event Viewer for error details." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Name:       $ServiceName" -ForegroundColor White
Write-Host "Installation Path:  $InstallPath" -ForegroundColor White
Write-Host "Configuration:      $configPath" -ForegroundColor White
Write-Host "API Base URL:       $ApiBaseUrl" -ForegroundColor White
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. The device client service is now running" -ForegroundColor White
Write-Host "2. View logs in Windows Event Viewer (Application log)" -ForegroundColor White
Write-Host "3. Manage service: services.msc or 'sc.exe query $ServiceName'" -ForegroundColor White
Write-Host "4. To uninstall: Run .\uninstall.ps1" -ForegroundColor White
Write-Host ""
Write-Host "For more information, visit: https://github.com/donkeywork/device-manager" -ForegroundColor Cyan
