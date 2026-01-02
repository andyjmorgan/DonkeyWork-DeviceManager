# DonkeyWork Device Manager Client - One-Liner Installer for Windows
# Downloads the latest release and runs the platform-specific install script
#
# Usage:
#   iwr -useb https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.ps1 | iex
#
# With custom API URL:
#   $env:DEVICE_MANAGER_API_URL = "https://your-api.example.com"; iwr -useb https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.ps1 | iex
#
# Install specific version:
#   $env:INSTALL_VERSION = "v1.0.0"; iwr -useb https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.ps1 | iex

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

# Configuration
$GitHubRepo = if ($env:GITHUB_REPO) { $env:GITHUB_REPO } else { "YOUR_ORG/DonkeyWork-DeviceManager" }
$InstallVersion = if ($env:INSTALL_VERSION) { $env:INSTALL_VERSION } else { "latest" }
$ApiBaseUrl = if ($env:DEVICE_MANAGER_API_URL) { $env:DEVICE_MANAGER_API_URL } else { "https://devicemanager.donkeywork.dev" }

# Helper functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    exit 1
}

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator"
}

# Detect architecture
function Get-Platform {
    $arch = $env:PROCESSOR_ARCHITECTURE

    switch ($arch) {
        "AMD64" {
            return @{
                ArchName = "x64"
                PlatformName = "Windows"
                ScriptName = "install.ps1"
                Runtime = "win-x64"
            }
        }
        "ARM64" {
            Write-Error "Windows ARM64 is not currently supported. Supported: linux-x64, osx-arm64, win-x64"
        }
        default {
            Write-Error "Unsupported Windows architecture: $arch"
        }
    }
}

# Get download URL for release
function Get-DownloadUrl {
    param(
        [string]$Repo,
        [string]$Version,
        [string]$PlatformName
    )

    if ($Version -eq "latest") {
        Write-Info "Fetching latest release information..."
        $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
    }
    else {
        Write-Info "Fetching release information for $Version..."
        $apiUrl = "https://api.github.com/repos/$Repo/releases/tags/$Version"
    }

    try {
        $release = Invoke-RestMethod -Uri $apiUrl -ErrorAction Stop
    }
    catch {
        Write-Error "Failed to fetch release from GitHub: $_"
    }

    $tagName = $release.tag_name
    if (-not $tagName) {
        Write-Error "Could not parse version from GitHub API response"
    }

    # Build expected filename based on CI/CD workflow naming
    $expectedFilename = "DonkeyWorkDeviceManager-DeviceClient-$PlatformName-$tagName.zip"

    # Find matching asset
    $asset = $release.assets | Where-Object { $_.name -eq $expectedFilename } | Select-Object -First 1

    if (-not $asset) {
        Write-Error "Could not find release asset. Expected: $expectedFilename"
    }

    Write-Info "Found version: $tagName"
    Write-Info "Download URL: $($asset.browser_download_url)"

    return @{
        Version = $tagName
        Url = $asset.browser_download_url
    }
}

# Download and extract release
function Get-Release {
    param(
        [string]$Url,
        [string]$Version
    )

    Write-Info "Downloading release..."

    # Create temp directory
    $tempDir = Join-Path $env:TEMP "donkeywork-install-$(Get-Random)"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    $downloadFile = Join-Path $tempDir "device-client.zip"

    try {
        Invoke-WebRequest -Uri $Url -OutFile $downloadFile -UseBasicParsing
        Write-Success "Downloaded release $Version"
    }
    catch {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Error "Failed to download release: $_"
    }

    Write-Info "Extracting archive..."

    $extractDir = Join-Path $tempDir "extracted"
    try {
        Expand-Archive -Path $downloadFile -DestinationPath $extractDir -Force
        Write-Success "Extracted archive"
    }
    catch {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Error "Failed to extract archive: $_"
    }

    return @{
        TempDir = $tempDir
        ExtractDir = $extractDir
    }
}

# Run platform install script
function Invoke-InstallScript {
    param(
        [string]$ExtractDir,
        [string]$ScriptName,
        [string]$TempDir
    )

    Write-Info "Running platform install script..."

    $installScript = Join-Path $ExtractDir "scripts\$ScriptName"

    if (-not (Test-Path $installScript)) {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Error "Install script not found: $installScript"
    }

    # Set API URL for install script
    $env:API_BASE_URL = $ApiBaseUrl

    try {
        & $installScript
    }
    catch {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Error "Installation failed: $_"
    }
}

# Main installation flow
function Main {
    Write-Host @"
========================================
DonkeyWork Device Manager Client
One-Liner Installer
========================================

"@ -ForegroundColor Cyan

    # Detect platform
    $platform = Get-Platform
    Write-Info "Detected platform: $($platform.PlatformName) ($($platform.Runtime))"

    # Get download URL
    $release = Get-DownloadUrl -Repo $GitHubRepo -Version $InstallVersion -PlatformName $platform.PlatformName

    # Download and extract
    $paths = Get-Release -Url $release.Url -Version $release.Version

    # Run platform-specific installer
    Invoke-InstallScript -ExtractDir $paths.ExtractDir -ScriptName $platform.ScriptName -TempDir $paths.TempDir

    # Cleanup
    Write-Info "Cleaning up temporary files..."
    Remove-Item -Path $paths.TempDir -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host @"

========================================
Installation Complete!
========================================

"@ -ForegroundColor Cyan

    Write-Success "Device client has been installed and started as a Windows Service"
    Write-Host ""
    Write-Host "View logs in Windows Event Viewer (Application log)" -ForegroundColor Yellow
    Write-Host ""
}

# Run with error handling
try {
    Main
}
catch {
    Write-Error "Installation failed: $_"
}
