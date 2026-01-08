namespace DonkeyWork.DeviceManager.Common.SignalR;

/// <summary>
/// Constants for SignalR hub method names.
/// DEPRECATED: Use strongly-typed interfaces (IDeviceClient, IUserClient) instead.
/// This class is kept for reference only and may be removed in a future version.
/// </summary>
[Obsolete("Use strongly-typed interfaces (IDeviceClient, IUserClient) instead of string-based method names.")]
public static class HubMethodNames
{
    /// <summary>
    /// DEPRECATED: Methods invoked BY devices ON the hub (still in use for basic operations).
    /// </summary>
    public static class DeviceInvoke
    {
        /// <summary>
        /// Device reports its status
        /// </summary>
        public const string ReportStatus = "ReportStatus";

        /// <summary>
        /// Device pings the server (test connection)
        /// </summary>
        public const string Ping = "Ping";
    }
}
