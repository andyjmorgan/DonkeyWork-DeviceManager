using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// Windows implementation of system control operations using P/Invoke.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSystemControlService : ISystemControlService
{
    private readonly ILogger<WindowsSystemControlService> _logger;

    public WindowsSystemControlService(ILogger<WindowsSystemControlService> logger)
    {
        _logger = logger;
    }

    public Task RestartAsync()
    {
        _logger.LogInformation("Initiating Windows system restart via P/Invoke");

        try
        {
            // Enable the SE_SHUTDOWN_NAME privilege
            EnableShutdownPrivilege();

            // Initiate system shutdown with restart
            // EWX_REBOOT | EWX_FORCE = 0x00000002 | 0x00000004 = 0x00000006
            if (!ExitWindowsEx(ExitWindows.Reboot | ExitWindows.Force, ShutdownReason.MajorApplication | ShutdownReason.MinorMaintenance))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            _logger.LogInformation("Windows restart initiated successfully");
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
        _logger.LogInformation("Initiating Windows system shutdown via P/Invoke");

        try
        {
            // Enable the SE_SHUTDOWN_NAME privilege
            EnableShutdownPrivilege();

            // Initiate system shutdown
            // EWX_SHUTDOWN | EWX_FORCE = 0x00000001 | 0x00000004 = 0x00000005
            if (!ExitWindowsEx(ExitWindows.ShutDown | ExitWindows.Force, ShutdownReason.MajorApplication | ShutdownReason.MinorMaintenance))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            _logger.LogInformation("Windows shutdown initiated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shutdown Windows system");
            throw;
        }

        return Task.CompletedTask;
    }

    private void EnableShutdownPrivilege()
    {
        IntPtr token;
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process token");
        }

        try
        {
            TOKEN_PRIVILEGES tp;
            tp.PrivilegeCount = 1;
            tp.Privileges = new LUID_AND_ATTRIBUTES[1];
            tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            if (!LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out tp.Privileges[0].Luid))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to lookup privilege value");
            }

            if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to adjust token privileges");
            }
        }
        finally
        {
            CloseHandle(token);
        }
    }

    #region P/Invoke Declarations

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(ExitWindows uFlags, ShutdownReason dwReason);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [Flags]
    private enum ExitWindows : uint
    {
        LogOff = 0x00000000,
        ShutDown = 0x00000001,
        Reboot = 0x00000002,
        Force = 0x00000004,
        PowerOff = 0x00000008,
        ForceIfHung = 0x00000010
    }

    [Flags]
    private enum ShutdownReason : uint
    {
        MajorApplication = 0x00040000,
        MajorHardware = 0x00010000,
        MajorLegacyApi = 0x00070000,
        MajorOperatingSystem = 0x00020000,
        MajorOther = 0x00000000,
        MajorPower = 0x00060000,
        MajorSoftware = 0x00030000,
        MajorSystem = 0x00050000,

        MinorBlueScreen = 0x0000000F,
        MinorCordUnplugged = 0x0000000b,
        MinorDisk = 0x00000007,
        MinorEnvironment = 0x0000000c,
        MinorHardwareDriver = 0x0000000d,
        MinorHotfix = 0x00000011,
        MinorHung = 0x00000005,
        MinorInstallation = 0x00000002,
        MinorMaintenance = 0x00000001,
        MinorMMC = 0x00000019,
        MinorNetworkConnectivity = 0x00000014,
        MinorNetworkCard = 0x00000009,
        MinorOther = 0x00000000,
        MinorOtherDriver = 0x0000000e,
        MinorPowerSupply = 0x0000000a,
        MinorProcessor = 0x00000008,
        MinorReconfig = 0x00000004,
        MinorSecurity = 0x00000013,
        MinorSecurityFix = 0x00000012,
        MinorSecurityFixUninstall = 0x00000018,
        MinorServicePack = 0x00000010,
        MinorServicePackUninstall = 0x00000016,
        MinorTermSrv = 0x00000020,
        MinorUnstable = 0x00000006,
        MinorUpgrade = 0x00000003,
        MinorWMI = 0x00000015,

        FlagUserDefined = 0x40000000,
        FlagPlanned = 0x80000000
    }

    #endregion
}
