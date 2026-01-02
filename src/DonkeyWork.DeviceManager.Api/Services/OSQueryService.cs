namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.OSQuery;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing OSQuery history and executions.
/// </summary>
public class OSQueryService : IOSQueryService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<OSQueryService> _logger;

    public OSQueryService(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        ILogger<OSQueryService> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<OSQueryHistoryResponse>> GetQueryHistoryAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("Getting OSQuery history for user {UserId} - Page: {Page}, PageSize: {PageSize}",
            userId, page, pageSize);

        // Get total count
        var totalCount = await _dbContext.OSQueryHistory
            .Where(q => q.UserId == userId)
            .CountAsync(cancellationToken);

        // Get paginated queries
        var queries = await _dbContext.OSQueryHistory
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.Updated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = queries.Select(q => new OSQueryHistoryResponse
        {
            Id = q.Id,
            Query = q.Query,
            ExecutionCount = q.ExecutionCount,
            LastExecutedAt = q.LastExecutedAt,
            Created = q.Created,
            Updated = q.Updated
        }).ToList();

        return new PaginatedResponse<OSQueryHistoryResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<OSQueryHistoryResponse?> GetQueryHistoryByIdAsync(Guid queryId, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;

        var query = await _dbContext.OSQueryHistory
            .Where(q => q.Id == queryId && q.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (query == null)
        {
            _logger.LogWarning("OSQuery history {QueryId} not found for user {UserId}", queryId, userId);
            return null;
        }

        return new OSQueryHistoryResponse
        {
            Id = query.Id,
            Query = query.Query,
            ExecutionCount = query.ExecutionCount,
            LastExecutedAt = query.LastExecutedAt,
            Created = query.Created,
            Updated = query.Updated
        };
    }

    /// <inheritdoc />
    public async Task<OSQueryHistoryResponse> SaveQueryToHistoryAsync(string query, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;
        var tenantId = _requestContextProvider.Context.TenantId;

        // Check if this exact query already exists for this user
        var existingQuery = await _dbContext.OSQueryHistory
            .Where(q => q.UserId == userId && q.Query == query)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingQuery != null)
        {
            // Update existing query
            existingQuery.Updated = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated existing OSQuery history {QueryId} for user {UserId}", existingQuery.Id, userId);

            return new OSQueryHistoryResponse
            {
                Id = existingQuery.Id,
                Query = existingQuery.Query,
                ExecutionCount = existingQuery.ExecutionCount,
                LastExecutedAt = existingQuery.LastExecutedAt,
                Created = existingQuery.Created,
                Updated = existingQuery.Updated
            };
        }

        // Create new query history entry
        var newQuery = new OSQueryHistoryEntity
        {
            Id = Guid.NewGuid(),
            Query = query,
            UserId = userId,
            TenantId = tenantId,
            ExecutionCount = 0,
            Created = DateTimeOffset.UtcNow,
            Updated = DateTimeOffset.UtcNow
        };

        _dbContext.OSQueryHistory.Add(newQuery);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new OSQuery history {QueryId} for user {UserId}", newQuery.Id, userId);

        return new OSQueryHistoryResponse
        {
            Id = newQuery.Id,
            Query = newQuery.Query,
            ExecutionCount = newQuery.ExecutionCount,
            LastExecutedAt = newQuery.LastExecutedAt,
            Created = newQuery.Created,
            Updated = newQuery.Updated
        };
    }

    /// <inheritdoc />
    public async Task DeleteQueryFromHistoryAsync(Guid queryId, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;

        var query = await _dbContext.OSQueryHistory
            .Where(q => q.Id == queryId && q.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (query == null)
        {
            _logger.LogWarning("OSQuery history {QueryId} not found for user {UserId}", queryId, userId);
            throw new InvalidOperationException($"Query {queryId} not found");
        }

        _dbContext.OSQueryHistory.Remove(query);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted OSQuery history {QueryId} for user {UserId}", queryId, userId);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateExecutionAsync(string query, Guid? queryHistoryId, int deviceCount, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;
        var tenantId = _requestContextProvider.Context.TenantId;

        var execution = new OSQueryExecutionEntity
        {
            Id = Guid.NewGuid(),
            QueryHistoryId = queryHistoryId,
            Query = query,
            ExecutedAt = DateTimeOffset.UtcNow,
            DeviceCount = deviceCount,
            SuccessCount = 0,
            FailureCount = 0,
            UserId = userId,
            TenantId = tenantId,
            Created = DateTimeOffset.UtcNow,
            Updated = DateTimeOffset.UtcNow
        };

        _dbContext.OSQueryExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update query history if this execution is from saved history
        if (queryHistoryId.HasValue)
        {
            var queryHistory = await _dbContext.OSQueryHistory
                .FirstOrDefaultAsync(q => q.Id == queryHistoryId.Value, cancellationToken);

            if (queryHistory != null)
            {
                queryHistory.ExecutionCount++;
                queryHistory.LastExecutedAt = DateTimeOffset.UtcNow;
                queryHistory.Updated = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Created OSQuery execution {ExecutionId} for user {UserId} on {DeviceCount} devices",
            execution.Id, userId, deviceCount);

        return execution.Id;
    }

    /// <inheritdoc />
    public async Task SaveExecutionResultAsync(Guid executionId, Guid deviceId, bool success, string? errorMessage, string? rawJson, int executionTimeMs, int rowCount, CancellationToken cancellationToken = default)
    {
        var result = new OSQueryExecutionResultEntity
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            DeviceId = deviceId,
            Success = success,
            ErrorMessage = errorMessage,
            RawJson = rawJson,
            ExecutionTimeMs = executionTimeMs,
            RowCount = rowCount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.OSQueryExecutionResults.Add(result);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved OSQuery execution result for execution {ExecutionId}, device {DeviceId}, success: {Success}",
            executionId, deviceId, success);
    }

    /// <inheritdoc />
    public async Task<OSQueryExecutionResponse?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;

        var execution = await _dbContext.OSQueryExecutions
            .Include(e => e.Results)
                .ThenInclude(r => r.Device)
            .Where(e => e.Id == executionId && e.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (execution == null)
        {
            _logger.LogWarning("OSQuery execution {ExecutionId} not found for user {UserId}", executionId, userId);
            return null;
        }

        return new OSQueryExecutionResponse
        {
            Id = execution.Id,
            QueryHistoryId = execution.QueryHistoryId,
            Query = execution.Query,
            ExecutedAt = execution.ExecutedAt,
            DeviceCount = execution.DeviceCount,
            SuccessCount = execution.SuccessCount,
            FailureCount = execution.FailureCount,
            UserId = execution.UserId,
            Created = execution.Created,
            Results = execution.Results.Select(r => new OSQueryExecutionResultResponse
            {
                Id = r.Id,
                ExecutionId = r.ExecutionId,
                DeviceId = r.DeviceId,
                DeviceName = r.Device.Name,
                Success = r.Success,
                ErrorMessage = r.ErrorMessage,
                RawJson = r.RawJson,
                ExecutionTimeMs = r.ExecutionTimeMs,
                RowCount = r.RowCount,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<List<OSQueryExecutionResponse>> GetRecentExecutionsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;

        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var executions = await _dbContext.OSQueryExecutions
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ExecutedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return executions.Select(e => new OSQueryExecutionResponse
        {
            Id = e.Id,
            QueryHistoryId = e.QueryHistoryId,
            Query = e.Query,
            ExecutedAt = e.ExecutedAt,
            DeviceCount = e.DeviceCount,
            SuccessCount = e.SuccessCount,
            FailureCount = e.FailureCount,
            UserId = e.UserId,
            Created = e.Created,
            Results = new List<OSQueryExecutionResultResponse>() // Don't load results for list view
        }).ToList();
    }

    /// <inheritdoc />
    public async Task UpdateExecutionCountsAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.OSQueryExecutions
            .Include(e => e.Results)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution == null)
        {
            _logger.LogWarning("OSQuery execution {ExecutionId} not found", executionId);
            return;
        }

        execution.SuccessCount = execution.Results.Count(r => r.Success);
        execution.FailureCount = execution.Results.Count(r => !r.Success);
        execution.Updated = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated OSQuery execution {ExecutionId} counts: {SuccessCount} success, {FailureCount} failure",
            executionId, execution.SuccessCount, execution.FailureCount);
    }
}
