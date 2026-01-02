using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// macOS implementation of system control operations.
/// Uses shutdown command available on macOS.
/// </summary>
public class MacOSSystemControlService : ISystemControlService
{
    private readonly ILogger<MacOSSystemControlService> _logger;

    public MacOSSystemControlService(ILogger<MacOSSystemControlService> logger)
    {
        _logger = logger;
    }

    public Task RestartAsync()
    {
        _logger.LogInformation("Initiating macOS system restart");

        try
        {
            // shutdown -r now - restart immediately
            // Alternative: osascript -e 'tell app "System Events" to restart'
            var startInfo = new ProcessStartInfo
            {
                FileName = "/sbin/shutdown",
                Arguments = "-r now",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(5000);
                _logger.LogInformation("macOS restart command executed successfully (exit code: {ExitCode})", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart macOS system");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger.LogInformation("Initiating macOS system shutdown");

        try
        {
            // shutdown -h now - halt (shutdown) immediately
            // Alternative: osascript -e 'tell app "System Events" to shut down'
            var startInfo = new ProcessStartInfo
            {
                FileName = "/sbin/shutdown",
                Arguments = "-h now",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(5000);
                _logger.LogInformation("macOS shutdown command executed successfully (exit code: {ExitCode})", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown macOS system");
            throw;
        }

        return Task.CompletedTask;
    }
}
