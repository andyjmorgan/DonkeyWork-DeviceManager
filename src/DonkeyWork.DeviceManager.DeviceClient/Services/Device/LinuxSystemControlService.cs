using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// Linux implementation of system control operations.
/// Uses systemctl for modern Linux systems with systemd.
/// </summary>
public class LinuxSystemControlService : ISystemControlService
{
    private readonly ILogger<LinuxSystemControlService> _logger;

    public LinuxSystemControlService(ILogger<LinuxSystemControlService> logger)
    {
        _logger = logger;
    }

    public Task RestartAsync()
    {
        _logger.LogInformation("Initiating Linux system restart");

        try
        {
            // systemctl reboot - standard way to restart on systemd-based Linux
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/systemctl",
                Arguments = "reboot",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(5000);
                _logger.LogInformation("Linux restart command executed successfully (exit code: {ExitCode})", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart Linux system");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger.LogInformation("Initiating Linux system shutdown");

        try
        {
            // systemctl poweroff - standard way to shutdown on systemd-based Linux
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/systemctl",
                Arguments = "poweroff",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(5000);
                _logger.LogInformation("Linux shutdown command executed successfully (exit code: {ExitCode})", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown Linux system");
            throw;
        }

        return Task.CompletedTask;
    }
}
