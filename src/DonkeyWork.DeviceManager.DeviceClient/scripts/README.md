# DonkeyWork Device Manager Client - Installation Scripts

Platform-specific installation and uninstallation scripts for the DonkeyWork Device Manager Client.

## Overview

These scripts automate the installation of the Device Manager Client as a system service on Windows, Linux, and macOS. The client will:

- Run automatically on system startup
- Restart automatically if it crashes
- Log to the system's native logging infrastructure
- Run with appropriate permissions

## Prerequisites

### All Platforms
- **.NET 10.0 SDK** installed ([Download](https://dotnet.microsoft.com/download))
- **Administrator/root privileges** to install services

**Note:** The API endpoint is preconfigured to `https://devicemanager.donkeywork.dev`

### Windows
- Windows 10/11 or Windows Server 2019+
- PowerShell 5.1 or later

### Linux
- systemd-based distribution (Ubuntu 20.04+, Debian 11+, RHEL 8+, etc.)
- Bash shell

### macOS
- macOS 11 (Big Sur) or later
- For Apple Silicon: ARM64 architecture
- For Intel: x64 architecture

## Installation

### Windows (win-x64)

1. **Open PowerShell as Administrator**

2. **Navigate to the scripts directory:**
   ```powershell
   cd .\src\DonkeyWork.DeviceManager.DeviceClient\scripts\win-x64\
   ```

3. **Run the installation script:**
   ```powershell
   .\install.ps1
   ```

4. **Optional parameters:**
   ```powershell
   .\install.ps1 `
       -InstallPath "D:\Apps\DeviceClient" `
       -ServiceName "CustomDeviceClient"
   ```

5. **View help:**
   ```powershell
   .\install.ps1 -Help
   ```

**Installation location:** `C:\Program Files\DonkeyWork\DeviceClient` (customizable)

### Linux (linux-x64)

1. **Navigate to the scripts directory:**
   ```bash
   cd src/DonkeyWork.DeviceManager.DeviceClient/scripts/linux-x64/
   ```

2. **Make scripts executable (if needed):**
   ```bash
   chmod +x install.sh uninstall.sh
   ```

3. **Run the installation script:**
   ```bash
   sudo ./install.sh
   ```

4. **Optional parameters:**
   ```bash
   sudo ./install.sh \
       --service-name "custom-device-client" \
       --service-user "deviceuser"
   ```

5. **View help:**
   ```bash
   ./install.sh --help
   ```

**Installation location:** `/opt/donkeywork/device-client` (fixed)

### macOS (osx-arm64)

1. **Navigate to the scripts directory:**
   ```bash
   cd src/DonkeyWork.DeviceManager.DeviceClient/scripts/osx-arm64/
   ```

2. **Make scripts executable (if needed):**
   ```bash
   chmod +x install.sh uninstall.sh
   ```

3. **Run the installation script:**
   ```bash
   sudo ./install.sh
   ```

4. **Optional parameters:**
   ```bash
   sudo ./install.sh --service-label "com.example.device-client"
   ```

5. **View help:**
   ```bash
   ./install.sh --help
   ```

**Installation location:** `/usr/local/opt/donkeywork/device-client` (fixed)

## Post-Installation

### Windows

**View service status:**
```powershell
Get-Service DonkeyWorkDeviceClient
```

**View logs:**
- Open Event Viewer → Windows Logs → Application
- Filter by source: DonkeyWorkDeviceClient

**Manage service:**
```powershell
# Stop service
Stop-Service DonkeyWorkDeviceClient

# Start service
Start-Service DonkeyWorkDeviceClient

# Restart service
Restart-Service DonkeyWorkDeviceClient
```

### Linux

**View service status:**
```bash
systemctl status donkeywork-device-client
```

**View logs:**
```bash
# Real-time logs
journalctl -u donkeywork-device-client -f

# Recent logs
journalctl -u donkeywork-device-client -n 100
```

**Manage service:**
```bash
# Stop service
sudo systemctl stop donkeywork-device-client

# Start service
sudo systemctl start donkeywork-device-client

# Restart service
sudo systemctl restart donkeywork-device-client
```

### macOS

**View service status:**
```bash
sudo launchctl print system/com.donkeywork.device-client
```

**View logs:**
```bash
# Real-time logs
tail -f /var/log/donkeywork-device-client.log

# Error logs
tail -f /var/log/donkeywork-device-client-error.log
```

**Manage service:**
```bash
# Stop service
sudo launchctl bootout system /Library/LaunchDaemons/com.donkeywork.device-client.plist

# Start service
sudo launchctl bootstrap system /Library/LaunchDaemons/com.donkeywork.device-client.plist
```

## Uninstallation

### Windows

```powershell
cd .\src\DonkeyWork.DeviceManager.DeviceClient\scripts\win-x64\
.\uninstall.ps1
```

**Keep configuration files:**
```powershell
.\uninstall.ps1 -KeepConfig
```

### Linux

```bash
cd src/DonkeyWork.DeviceManager.DeviceClient/scripts/linux-x64/
sudo ./uninstall.sh
```

**Keep configuration files:**
```bash
sudo ./uninstall.sh --keep-config
```

### macOS

```bash
cd src/DonkeyWork.DeviceManager.DeviceClient/scripts/osx-arm64/
sudo ./uninstall.sh
```

**Keep configuration files:**
```bash
sudo ./uninstall.sh --keep-config
```

## Configuration

The client reads its configuration from `appsettings.json` in the installation directory.

**Default locations:**
- **Windows:** `C:\Program Files\DonkeyWork\DeviceClient\appsettings.json`
- **Linux:** `/opt/donkeywork/device-client/appsettings.json`
- **macOS:** `/usr/local/opt/donkeywork/device-client/appsettings.json`

**Example configuration:**
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

**Note:** The API endpoint is preconfigured during installation and should not require manual changes.

After modifying the configuration, restart the service:
- **Windows:** `Restart-Service DonkeyWorkDeviceClient`
- **Linux:** `sudo systemctl restart donkeywork-device-client`
- **macOS:** Unload and reload the service

## Troubleshooting

### Service won't start

1. **Check logs** for error messages
2. **Verify API is accessible:**
   ```bash
   curl https://devicemanager.donkeywork.dev/healthz
   ```
3. **Check permissions** on installation directory
4. **Verify .NET runtime** is properly installed

### Device not registering

1. **Verify service is running**
2. **Check network connectivity** to API server
3. **View logs** for registration errors
4. **Ensure registration code** was not already used

### Service crashes repeatedly

1. **View error logs** immediately after crash
2. **Check disk space** in installation directory
3. **Verify .NET runtime** is properly installed
4. **Check for firewall rules** blocking connections

### Configuration changes not taking effect

1. **Restart the service** after configuration changes
2. **Verify JSON syntax** in appsettings.json
3. **Check file permissions** on configuration file

## Security Considerations

### Windows
- Service runs as **Local System** by default
- Consider creating a dedicated service account with minimal permissions

### Linux
- Service runs as dedicated **donkeywork** user (created by installer)
- Limited permissions, no shell access

### macOS
- Service runs as **root** (required for launchd daemons)
- Consider sandboxing for additional security

### All Platforms
- **Protect API credentials** stored in configuration
- **Use HTTPS** for API communication
- **Keep token files secure** (device-tokens.json)
- **Regularly update** the client software

## Architecture-Specific Notes

### Windows
- Compiled for **win-x64** (64-bit Windows)
- Self-contained deployment includes .NET runtime

### Linux
- Compiled for **linux-x64** (64-bit Linux)
- Self-contained deployment includes .NET runtime
- Requires systemd for service management

### macOS
- **osx-arm64** for Apple Silicon (M1/M2/M3)
- **osx-x64** for Intel Macs (use linux-x64 script with --runtime osx-x64)
- Self-contained deployment includes .NET runtime
- Uses launchd for service management

## Support

For issues, questions, or contributions:
- **GitHub:** https://github.com/donkeywork/device-manager
- **Documentation:** https://docs.donkeywork.dev
- **Issues:** https://github.com/donkeywork/device-manager/issues

## License

See the main project LICENSE file for licensing information.
