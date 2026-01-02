# DonkeyWork Device Manager - Installation Guide

This guide covers installing the DonkeyWork Device Manager Client on your device.

## One-Liner Installation (Recommended)

The easiest way to install the device client is using our one-liner installer, which automatically:
- Detects your platform and architecture
- Downloads the latest release from GitHub
- Installs the client as a system service
- Sets up logging
- Installs OSQuery for hardware inventory

### Linux

```bash
curl -sfL https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.sh | sudo sh -
```

**Supported Distributions:**
- Ubuntu 18.04+ / Debian 10+
- Rocky Linux 8+ / RHEL 8+ / CentOS 8+ / Fedora 32+

**Supported Architecture:** x86_64 (amd64)

### macOS

```bash
curl -sfL https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.sh | sudo sh -
```

**Supported:** Apple Silicon (ARM64)

### Windows

Open PowerShell as Administrator and run:

```powershell
iwr -useb https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.ps1 | iex
```

**Supported:** x86_64 (amd64)

## Custom Installation Options

### Install Specific Version

**Linux/macOS:**
```bash
curl -sfL https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.sh | sudo INSTALL_VERSION=v1.0.0 sh -
```

**Windows:**
```powershell
$env:INSTALL_VERSION = "v1.0.0"; iwr -useb https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.ps1 | iex
```

### Custom API URL

If you're running your own instance of the Device Manager API:

**Linux/macOS:**
```bash
curl -sfL https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.sh | sudo DEVICE_MANAGER_API_URL=https://your-api.example.com sh -
```

**Windows:**
```powershell
$env:DEVICE_MANAGER_API_URL = "https://your-api.example.com"; iwr -useb https://raw.githubusercontent.com/andyjmorgan/DonkeyWork-DeviceManager/main/install.ps1 | iex
```

## Manual Installation

If you prefer to install manually:

1. **Download the release** for your platform from the [Releases page](https://github.com/andyjmorgan/DonkeyWork-DeviceManager/releases)

2. **Extract the archive**

3. **Run the platform-specific install script:**

   **Linux:**
   ```bash
   cd DonkeyWorkDeviceManager-DeviceClient-Linux-vX.X.X
   sudo ./scripts/install.sh
   ```

   **macOS:**
   ```bash
   cd DonkeyWorkDeviceManager-DeviceClient-macOS-vX.X.X
   sudo ./scripts/install.sh
   ```

   **Windows (PowerShell as Administrator):**
   ```powershell
   cd DonkeyWorkDeviceManager-DeviceClient-Windows-vX.X.X
   .\scripts\install.ps1
   ```

## What Gets Installed

### Linux
- **Binary location:** `/opt/donkeywork/device-client/`
- **Service:** `donkeywork-device-client.service` (systemd)
- **Service user:** `donkeywork`
- **Configuration:** `/opt/donkeywork/device-client/appsettings.json`
- **Logs:** `journalctl -u donkeywork-device-client -f`
- **OSQuery:** Installed from official repositories

### macOS
- **Binary location:** `/usr/local/opt/donkeywork/device-client/`
- **Service:** `com.donkeywork.device-client` (launchd)
- **Configuration:** `/usr/local/opt/donkeywork/device-client/appsettings.json`
- **Logs:** `/var/log/donkeywork-device-client.log`
- **OSQuery:** Installed via Homebrew (if available)

### Windows
- **Binary location:** `C:\Program Files\DonkeyWork\DeviceClient\`
- **Service:** `DonkeyWorkDeviceClient` (Windows Service)
- **Configuration:** `C:\Program Files\DonkeyWork\DeviceClient\appsettings.json`
- **Logs:** Windows Event Viewer → Application log
- **OSQuery:** Downloaded and installed automatically

## Managing the Service

### Linux (systemd)

```bash
# Check status
sudo systemctl status donkeywork-device-client

# View logs (follow mode)
sudo journalctl -u donkeywork-device-client -f

# View recent logs
sudo journalctl -u donkeywork-device-client -n 100

# Restart service
sudo systemctl restart donkeywork-device-client

# Stop service
sudo systemctl stop donkeywork-device-client

# Start service
sudo systemctl start donkeywork-device-client

# Disable service (prevent auto-start)
sudo systemctl disable donkeywork-device-client

# Re-enable service
sudo systemctl enable donkeywork-device-client
```

### macOS (launchd)

```bash
# Check status
sudo launchctl print system/com.donkeywork.device-client

# View logs (follow mode)
tail -f /var/log/donkeywork-device-client.log

# View error logs
tail -f /var/log/donkeywork-device-client-error.log

# Stop service
sudo launchctl bootout system /Library/LaunchDaemons/com.donkeywork.device-client.plist

# Start service
sudo launchctl bootstrap system /Library/LaunchDaemons/com.donkeywork.device-client.plist

# Restart service (stop then start)
sudo launchctl bootout system /Library/LaunchDaemons/com.donkeywork.device-client.plist
sudo launchctl bootstrap system /Library/LaunchDaemons/com.donkeywork.device-client.plist
```

### Windows (Windows Service)

```powershell
# Check status (PowerShell)
Get-Service DonkeyWorkDeviceClient

# Check status (Command Prompt)
sc query DonkeyWorkDeviceClient

# View logs
# Open Event Viewer → Windows Logs → Application
# Filter by source: DonkeyWorkDeviceClient

# Restart service (PowerShell)
Restart-Service DonkeyWorkDeviceClient

# Stop service
Stop-Service DonkeyWorkDeviceClient

# Start service
Start-Service DonkeyWorkDeviceClient

# Disable service
Set-Service DonkeyWorkDeviceClient -StartupType Disabled

# Re-enable service (automatic start)
Set-Service DonkeyWorkDeviceClient -StartupType Automatic
```

## Uninstalling

Uninstall scripts are included with each platform release.

### Linux

```bash
cd /opt/donkeywork/device-client
sudo ./scripts/uninstall.sh
```

Or from the extracted download package:
```bash
sudo ./scripts/uninstall.sh
```

### macOS

```bash
cd /usr/local/opt/donkeywork/device-client
sudo ./scripts/uninstall.sh
```

Or from the extracted download package:
```bash
sudo ./scripts/uninstall.sh
```

### Windows

```powershell
cd "C:\Program Files\DonkeyWork\DeviceClient"
.\scripts\uninstall.ps1
```

Or from the extracted download package:
```powershell
.\scripts\uninstall.ps1
```

## Configuration

The configuration file (`appsettings.json`) is automatically created during installation. You can modify it to customize the behavior:

```json
{
  "DeviceManagerConfiguration": {
    "ApiBaseUrl": "https://devicemanager.donkeywork.dev"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

After modifying the configuration, restart the service for changes to take effect.

## Troubleshooting

### Service won't start

**Check logs:**
- **Linux:** `sudo journalctl -u donkeywork-device-client -n 50`
- **macOS:** `tail -n 50 /var/log/donkeywork-device-client-error.log`
- **Windows:** Event Viewer → Application log

**Common issues:**
1. **Port conflicts:** Another service may be using the required port
2. **Permissions:** Ensure the service user has access to the installation directory
3. **Configuration errors:** Check `appsettings.json` for syntax errors
4. **.NET Runtime:** Ensure .NET runtime is available (should be bundled)

### Can't connect to API

1. **Check API URL** in `appsettings.json`
2. **Network connectivity:** Ensure the device can reach the API endpoint
3. **Firewall:** Check firewall rules aren't blocking outbound connections
4. **Certificate issues:** Ensure SSL certificates are valid

### OSQuery not working

OSQuery is required for hardware inventory queries. If it's not installed:

- **Linux:** `sudo apt install osquery` or `sudo yum install osquery`
- **macOS:** `brew install --cask osquery`
- **Windows:** Download from https://osquery.io/downloads/official

## Requirements

### Linux
- **Ubuntu** 18.04+ / Debian 10+
- **Rocky Linux** 8+ / RHEL 8+ / CentOS 8+ / Fedora 32+
- x86_64 architecture
- `curl` for downloading
- `unzip` for extraction
- `systemd` for service management
- Root access (sudo)

### macOS
- macOS 11.0 (Big Sur) or later
- Apple Silicon (ARM64)
- `curl` for downloading
- `unzip` for extraction
- Root access (sudo)

### Windows
- Windows 10 / Windows Server 2016 or later
- x86_64 architecture
- PowerShell 5.1 or later
- Administrator access

## Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/andyjmorgan/DonkeyWork-DeviceManager/issues
- Documentation: https://github.com/andyjmorgan/DonkeyWork-DeviceManager

## Security

The installer:
- Downloads releases over HTTPS
- Verifies downloads using GitHub's infrastructure
- Runs with minimal required permissions
- Creates dedicated service users (Linux/macOS)
- Follows security best practices for service installation

For security issues, please contact: security@donkeywork.dev
