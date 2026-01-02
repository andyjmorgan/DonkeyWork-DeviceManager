namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Audit;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Service for logging device audit events.
/// </summary>
public class DeviceAuditService : IDeviceAuditService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceAuditService> _logger;

    public DeviceAuditService(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceAuditService> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogStatusChangeAsync(Guid deviceId, bool isOnline, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        var auditLog = new DeviceAuditLogEntity
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            EventType = "StatusChange",
            Action = isOnline ? "Online" : "Offline",
            InitiatedByUserId = null, // System event
            TenantId = tenantId,
            Result = "Success",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.DeviceAuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Logged status change for device {DeviceId}: {Status}", deviceId, isOnline ? "Online" : "Offline");
    }

    /// <inheritdoc />
    public async Task LogCommandAsync(Guid deviceId, string action, Guid? initiatedByUserId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        // If no user specified, use current user from context
        if (!initiatedByUserId.HasValue)
        {
            initiatedByUserId = _requestContextProvider.Context.UserId;
        }

        var auditLog = new DeviceAuditLogEntity
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            EventType = "Command",
            Action = action,
            InitiatedByUserId = initiatedByUserId,
            TenantId = tenantId,
            Details = details,
            Result = "Pending",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.DeviceAuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Logged command {Action} for device {DeviceId} by user {UserId}",
            action, deviceId, initiatedByUserId);
    }

    /// <inheritdoc />
    public async Task LogOSQueryAsync(Guid deviceId, Guid initiatedByUserId, string query, bool success, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        var details = new
        {
            query = query.Length > 200 ? query.Substring(0, 200) + "..." : query,
            errorMessage
        };

        var auditLog = new DeviceAuditLogEntity
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            EventType = "Query",
            Action = "OSQuery",
            InitiatedByUserId = initiatedByUserId,
            TenantId = tenantId,
            Details = JsonSerializer.Serialize(details),
            Result = success ? "Success" : "Failure",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.DeviceAuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Logged OSQuery execution for device {DeviceId} by user {UserId}, success: {Success}",
            deviceId, initiatedByUserId, success);
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<DeviceAuditLogResponse>> GetDeviceAuditLogsAsync(Guid deviceId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("Getting audit logs for device {DeviceId} - Page: {Page}, PageSize: {PageSize}",
            deviceId, page, pageSize);

        // Get total count
        var totalCount = await _dbContext.DeviceAuditLogs
            .Where(log => log.DeviceId == deviceId)
            .CountAsync(cancellationToken);

        // Get paginated logs
        var logs = await _dbContext.DeviceAuditLogs
            .Include(log => log.Device)
            .Where(log => log.DeviceId == deviceId)
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = logs.Select(log => new DeviceAuditLogResponse
        {
            Id = log.Id,
            DeviceId = log.DeviceId,
            DeviceName = log.Device.Name,
            EventType = log.EventType,
            Action = log.Action,
            InitiatedByUserId = log.InitiatedByUserId,
            Details = log.Details,
            Result = log.Result,
            Timestamp = log.Timestamp
        }).ToList();

        return new PaginatedResponse<DeviceAuditLogResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<DeviceAuditLogResponse>> GetRecentAuditLogsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("Getting recent audit logs for tenant {TenantId} - Page: {Page}, PageSize: {PageSize}",
            tenantId, page, pageSize);

        // Get total count
        var totalCount = await _dbContext.DeviceAuditLogs
            .Where(log => log.TenantId == tenantId)
            .CountAsync(cancellationToken);

        // Get paginated logs
        var logs = await _dbContext.DeviceAuditLogs
            .Include(log => log.Device)
            .Where(log => log.TenantId == tenantId)
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = logs.Select(log => new DeviceAuditLogResponse
        {
            Id = log.Id,
            DeviceId = log.DeviceId,
            DeviceName = log.Device.Name,
            EventType = log.EventType,
            Action = log.Action,
            InitiatedByUserId = log.InitiatedByUserId,
            Details = log.Details,
            Result = log.Result,
            Timestamp = log.Timestamp
        }).ToList();

        return new PaginatedResponse<DeviceAuditLogResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
