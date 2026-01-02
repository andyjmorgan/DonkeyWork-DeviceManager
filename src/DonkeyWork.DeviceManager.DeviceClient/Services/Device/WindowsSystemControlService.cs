using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// Windows implementation of system control operations.
/// </summary>
public class WindowsSystemControlService : ISystemControlService
{
    private readonly ILogger<WindowsSystemControlService> _logger;

    public WindowsSystemControlService(ILogger<WindowsSystemControlService> logger)
    {
        _logger = logger;
    }

    public Task RestartAsync()
    {
        _logger.LogInformation("Initiating Windows system restart");

        try
        {
            // shutdown.exe /r = restart, /t 5 = delay 5 seconds, /f = force close applications
            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/r /t 5 /f /c \"DonkeyWork Device Manager initiated restart\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            _logger.LogInformation("Windows restart command executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart Windows system");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger.LogInformation("Initiating Windows system shutdown");

        try
        {
            // shutdown.exe /s = shutdown, /t 5 = delay 5 seconds, /f = force close applications
            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/s /t 5 /f /c \"DonkeyWork Device Manager initiated shutdown\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            _logger.LogInformation("Windows shutdown command executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown Windows system");
            throw;
        }

        return Task.CompletedTask;
    }
}
