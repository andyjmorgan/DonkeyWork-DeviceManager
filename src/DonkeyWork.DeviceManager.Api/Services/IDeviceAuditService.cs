namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Audit;

/// <summary>
/// Service for logging device audit events.
/// </summary>
public interface IDeviceAuditService
{
    /// <summary>
    /// Logs a device status change (online/offline).
    /// </summary>
    Task LogStatusChangeAsync(Guid deviceId, bool isOnline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a command sent to a device.
    /// </summary>
    Task LogCommandAsync(Guid deviceId, string action, Guid? initiatedByUserId = null, string? details = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an OSQuery execution on a device.
    /// </summary>
    Task LogOSQueryAsync(Guid deviceId, Guid initiatedByUserId, string query, bool success, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific device.
    /// </summary>
    Task<PaginatedResponse<DeviceAuditLogResponse>> GetDeviceAuditLogsAsync(Guid deviceId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs for the current tenant.
    /// </summary>
    Task<PaginatedResponse<DeviceAuditLogResponse>> GetRecentAuditLogsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
