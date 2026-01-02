namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.OSQuery;

/// <summary>
/// Service for managing OSQuery history and executions.
/// </summary>
public interface IOSQueryService
{
    /// <summary>
    /// Gets a paginated list of query history for the current user.
    /// </summary>
    Task<PaginatedResponse<OSQueryHistoryResponse>> GetQueryHistoryAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific query from history by ID.
    /// </summary>
    Task<OSQueryHistoryResponse?> GetQueryHistoryByIdAsync(Guid queryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates a query in history.
    /// </summary>
    Task<OSQueryHistoryResponse> SaveQueryToHistoryAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a query from history.
    /// </summary>
    Task DeleteQueryFromHistoryAsync(Guid queryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new execution record for a query.
    /// </summary>
    Task<Guid> CreateExecutionAsync(string query, Guid? queryHistoryId, int deviceCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a device execution result.
    /// </summary>
    Task SaveExecutionResultAsync(Guid executionId, Guid deviceId, bool success, string? errorMessage, string? rawJson, int executionTimeMs, int rowCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific execution with all its results.
    /// </summary>
    Task<OSQueryExecutionResponse?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent executions for the current user.
    /// </summary>
    Task<List<OSQueryExecutionResponse>> GetRecentExecutionsAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates execution counts after results are received.
    /// </summary>
    Task UpdateExecutionCountsAsync(Guid executionId, CancellationToken cancellationToken = default);
}
