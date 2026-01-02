using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// macOS implementation of system control operations using P/Invoke.
/// Uses the reboot() system call from libc (Darwin).
/// </summary>
[SupportedOSPlatform("macos")]
public class MacOSSystemControlService : ISystemControlService
{
    private readonly ILogger<MacOSSystemControlService> _logger;

    public MacOSSystemControlService(ILogger<MacOSSystemControlService> logger)
    {
        _logger = logger;
    }

    public async Task RestartAsync()
    {
        _logger.LogInformation("Initiating macOS system restart via P/Invoke");

        try
        {
            // Give time for logs to flush and responses to be sent
            _logger.LogInformation("Waiting 3 seconds before restart to allow log flushing...");
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Call reboot() system call with RB_AUTOBOOT flag
            // This requires root privileges
            int result = reboot(RB_AUTOBOOT);

            if (result != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new InvalidOperationException($"reboot() failed with errno: {errno}");
            }

            _logger.LogInformation("macOS restart initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart macOS system");
            throw;
        }
    }

    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Initiating macOS system shutdown via P/Invoke");

        try
        {
            // Give time for logs to flush and responses to be sent
            _logger.LogInformation("Waiting 3 seconds before shutdown to allow log flushing...");
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Call reboot() system call with RB_HALT flag
            // This requires root privileges
            int result = reboot(RB_HALT);

            if (result != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new InvalidOperationException($"reboot() failed with errno: {errno}");
            }

            _logger.LogInformation("macOS shutdown initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown macOS system");
            throw;
        }
    }

    #region P/Invoke Declarations

    // reboot() system call - see man 2 reboot
    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int reboot(int howto);

    // macOS/BSD reboot flags from <sys/reboot.h>
    private const int RB_AUTOBOOT = 0;    // Restart the system
    private const int RB_HALT = 8;        // Halt and power down the system
    private const int RB_POWEROFF = 0x2000; // Power off the system (alternative)

    #endregion
}
