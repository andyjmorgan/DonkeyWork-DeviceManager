namespace DonkeyWork.DeviceManager.Common.SignalR;

/// <summary>
/// Constants for SignalR hub method names.
/// Ensures consistency between API and device client.
/// </summary>
public static class HubMethodNames
{
    /// <summary>
    /// Commands sent FROM users TO devices
    /// </summary>
    public static class UserToDevice
    {
        /// <summary>
        /// Ping command - device should respond with latency
        /// </summary>
        public const string ReceivePingCommand = "ReceivePingCommand";

        /// <summary>
        /// Shutdown command - device should initiate shutdown
        /// </summary>
        public const string ReceiveShutdownCommand = "ReceiveShutdownCommand";

        /// <summary>
        /// Restart command - device should restart
        /// </summary>
        public const string ReceiveRestartCommand = "ReceiveRestartCommand";

        /// <summary>
        /// OSQuery command - device should execute query and return results
        /// </summary>
        public const string ReceiveOSQueryCommand = "ReceiveOSQueryCommand";
    }

    /// <summary>
    /// Responses sent FROM devices TO users
    /// </summary>
    public static class DeviceToUser
    {
        /// <summary>
        /// Ping response with latency measurement
        /// </summary>
        public const string ReceivePingResponse = "ReceivePingResponse";

        /// <summary>
        /// Command acknowledgment (success/failure)
        /// </summary>
        public const string ReceiveCommandAcknowledgment = "ReceiveCommandAcknowledgment";

        /// <summary>
        /// Device status change notification (online/offline)
        /// </summary>
        public const string ReceiveDeviceStatus = "ReceiveDeviceStatus";

        /// <summary>
        /// OSQuery result from device
        /// </summary>
        public const string ReceiveOSQueryResult = "ReceiveOSQueryResult";
    }

    /// <summary>
    /// Methods invoked BY devices ON the hub
    /// </summary>
    public static class DeviceInvoke
    {
        /// <summary>
        /// Device sends ping response to server
        /// </summary>
        public const string SendPingResponse = "SendPingResponse";

        /// <summary>
        /// Device acknowledges a command
        /// </summary>
        public const string AcknowledgeCommand = "AcknowledgeCommand";

        /// <summary>
        /// Device reports its status
        /// </summary>
        public const string ReportStatus = "ReportStatus";

        /// <summary>
        /// Device pings the server (test connection)
        /// </summary>
        public const string Ping = "Ping";

        /// <summary>
        /// Device sends OSQuery result to server
        /// </summary>
        public const string SendOSQueryResult = "SendOSQueryResult";
    }
}
