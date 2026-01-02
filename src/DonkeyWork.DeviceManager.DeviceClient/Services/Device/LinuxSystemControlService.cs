using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// Linux implementation of system control operations using P/Invoke.
/// Uses the reboot() system call from libc.
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxSystemControlService : ISystemControlService
{
    private readonly ILogger<LinuxSystemControlService> _logger;

    public LinuxSystemControlService(ILogger<LinuxSystemControlService> logger)
    {
        _logger = logger;
    }

    public Task RestartAsync()
    {
        _logger.LogInformation("Initiating Linux system restart via P/Invoke");

        try
        {
            // Call reboot() system call with RB_AUTOBOOT flag
            // This requires root privileges or CAP_SYS_BOOT capability
            int result = reboot(LINUX_REBOOT_CMD_RESTART);

            if (result != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new InvalidOperationException($"reboot() failed with errno: {errno}");
            }

            _logger.LogInformation("Linux restart initiated successfully");
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
        _logger.LogInformation("Initiating Linux system shutdown via P/Invoke");

        try
        {
            // Call reboot() system call with RB_POWER_OFF flag
            // This requires root privileges or CAP_SYS_BOOT capability
            int result = reboot(LINUX_REBOOT_CMD_POWER_OFF);

            if (result != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                throw new InvalidOperationException($"reboot() failed with errno: {errno}");
            }

            _logger.LogInformation("Linux shutdown initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown Linux system");
            throw;
        }

        return Task.CompletedTask;
    }

    #region P/Invoke Declarations

    // reboot() system call - see man 2 reboot
    [DllImport("libc", SetLastError = true)]
    private static extern int reboot(uint cmd);

    // Linux reboot commands from <linux/reboot.h>
    private const uint LINUX_REBOOT_CMD_RESTART = 0x01234567;   // Restart system
    private const uint LINUX_REBOOT_CMD_POWER_OFF = 0x4321FEDC; // Power off system

    #endregion
}
