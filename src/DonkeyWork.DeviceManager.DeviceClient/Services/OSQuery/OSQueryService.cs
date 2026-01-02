using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.DeviceManager.DeviceClient.Configuration;
using DonkeyWork.DeviceManager.DeviceClient.Models.OSQuery;
using Microsoft.Extensions.Options;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.OSQuery;

/// <summary>
/// Service for executing OSQuery queries on the local system.
/// Uses the OSQuery CLI (osqueryi) to execute queries and parse results.
/// </summary>
public class OSQueryService : IOSQueryService
{
    private readonly ILogger<OSQueryService> _logger;
    private readonly OSQueryConfiguration _config;
    private string? _cachedOSQueryPath;
    private bool? _cachedAvailability;

    public OSQueryService(
        ILogger<OSQueryService> logger,
        IOptions<OSQueryConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Return cached result if available
        if (_cachedAvailability.HasValue)
        {
            return _cachedAvailability.Value;
        }

        var path = await DetectOSQueryPathAsync(cancellationToken);
        _cachedAvailability = path != null;

        if (!_cachedAvailability.Value)
        {
            _logger.LogWarning("OSQuery is not available on this system");
        }
        else
        {
            _logger.LogInformation("OSQuery found at: {Path}", path);
        }

        return _cachedAvailability.Value;
    }

    /// <inheritdoc />
    public async Task<OSQueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        var stopwatch = Stopwatch.StartNew();

        // Get OSQuery path
        var osqueryPath = await DetectOSQueryPathAsync(cancellationToken);
        if (osqueryPath == null)
        {
            throw new OSQueryNotAvailableException(
                "OSQuery is not installed on this system. Please install OSQuery from https://osquery.io");
        }

        _logger.LogDebug("Executing OSQuery: {Query}", query);

        try
        {
            var result = await ExecuteOSQueryProcessAsync(osqueryPath, query, cancellationToken);
            stopwatch.Stop();

            result = result with { ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds };

            _logger.LogInformation(
                "OSQuery executed successfully in {Duration}ms, returned {RowCount} rows",
                result.ExecutionTimeMs,
                result.RowCount);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw new OSQueryTimeoutException(
                $"Query execution exceeded timeout of {_config.QueryTimeoutSeconds} seconds");
        }
        catch (OSQueryException)
        {
            // Re-throw OSQuery-specific exceptions
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing OSQuery");
            throw new OSQueryException("Unexpected error executing OSQuery", ex);
        }
    }

    /// <inheritdoc />
    public string? GetOSQueryPath()
    {
        return _cachedOSQueryPath;
    }

    /// <summary>
    /// Detects the OSQuery executable path.
    /// Checks: 1) configured path, 2) PATH environment variable, 3) common installation locations.
    /// </summary>
    private async Task<string?> DetectOSQueryPathAsync(CancellationToken cancellationToken)
    {
        // Return cached path if available
        if (_cachedOSQueryPath != null)
        {
            return _cachedOSQueryPath;
        }

        // 1. Check configured path
        if (!string.IsNullOrWhiteSpace(_config.OSQueryPath))
        {
            if (File.Exists(_config.OSQueryPath))
            {
                _cachedOSQueryPath = _config.OSQueryPath;
                _logger.LogDebug("Using configured OSQuery path: {Path}", _cachedOSQueryPath);
                return _cachedOSQueryPath;
            }

            _logger.LogWarning("Configured OSQuery path does not exist: {Path}", _config.OSQueryPath);
        }

        // 2. Check PATH environment variable
        var pathFromEnvironment = await FindInPathEnvironmentAsync(cancellationToken);
        if (pathFromEnvironment != null)
        {
            _cachedOSQueryPath = pathFromEnvironment;
            _logger.LogDebug("Found OSQuery in PATH: {Path}", _cachedOSQueryPath);
            return _cachedOSQueryPath;
        }

        // 3. Check common installation locations
        var pathFromCommonLocations = CheckCommonInstallLocations();
        if (pathFromCommonLocations != null)
        {
            _cachedOSQueryPath = pathFromCommonLocations;
            _logger.LogDebug("Found OSQuery in common location: {Path}", _cachedOSQueryPath);
            return _cachedOSQueryPath;
        }

        _logger.LogDebug("OSQuery not found in any known location");
        return null;
    }

    /// <summary>
    /// Searches for osqueryi in the PATH environment variable.
    /// </summary>
    private async Task<string?> FindInPathEnvironmentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var command = isWindows ? "where" : "which";
            var executable = isWindows ? "osqueryi.exe" : "osqueryi";

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Take the first line (in case multiple paths are returned)
                var path = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching PATH for osqueryi");
        }

        return null;
    }

    /// <summary>
    /// Checks common installation locations for osqueryi.
    /// </summary>
    private string? CheckCommonInstallLocations()
    {
        var commonLocations = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            commonLocations.Add(@"C:\Program Files\osquery\osqueryi.exe");
            commonLocations.Add(@"C:\Program Files (x86)\osquery\osqueryi.exe");
        }
        else if (OperatingSystem.IsLinux())
        {
            commonLocations.Add("/usr/bin/osqueryi");
            commonLocations.Add("/usr/local/bin/osqueryi");
            commonLocations.Add("/opt/osquery/bin/osqueryi");
        }
        else if (OperatingSystem.IsMacOS())
        {
            commonLocations.Add("/usr/local/bin/osqueryi");
            commonLocations.Add("/opt/homebrew/bin/osqueryi");
            commonLocations.Add("/usr/local/osquery/bin/osqueryi");
        }

        foreach (var location in commonLocations)
        {
            if (File.Exists(location))
            {
                return location;
            }
        }

        return null;
    }

    /// <summary>
    /// Executes the osqueryi process and returns the parsed result.
    /// </summary>
    private async Task<OSQueryResult> ExecuteOSQueryProcessAsync(
        string osqueryPath,
        string query,
        CancellationToken cancellationToken)
    {
        // Escape double quotes in query
        var escapedQuery = query.Replace("\"", "\\\"");

        var startInfo = new ProcessStartInfo
        {
            FileName = osqueryPath,
            Arguments = $"--json --line \"{escapedQuery}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new OSQueryException("Failed to start OSQuery process");
        }

        // Create timeout token
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.QueryTimeoutSeconds));

        try
        {
            // Read output and error streams
            var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token);

            var output = await outputTask;
            var error = await errorTask;

            // Check exit code
            if (process.ExitCode != 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(error)
                    ? "OSQuery returned non-zero exit code"
                    : error.Trim();

                _logger.LogError(
                    "OSQuery failed with exit code {ExitCode}: {Error}",
                    process.ExitCode,
                    errorMessage);

                return new OSQueryResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    RawJson = output
                };
            }

            // Parse JSON output
            return ParseOSQueryOutput(output);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill OSQuery process after timeout");
            }

            throw new OSQueryTimeoutException(
                $"Query execution exceeded timeout of {_config.QueryTimeoutSeconds} seconds");
        }
    }

    /// <summary>
    /// Parses OSQuery JSON output into an OSQueryResult.
    /// </summary>
    private OSQueryResult ParseOSQueryOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new OSQueryResult
            {
                Success = true,
                RawJson = output,
                Data = new List<Dictionary<string, object>>(),
                RowCount = 0
            };
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(output);
            var results = new List<Dictionary<string, object>>();

            foreach (var element in jsonDoc.RootElement.EnumerateArray())
            {
                var row = new Dictionary<string, object>();

                foreach (var property in element.EnumerateObject())
                {
                    row[property.Name] = ConvertJsonValue(property.Value);
                }

                results.Add(row);

                // Check max rows limit
                if (results.Count >= _config.MaxRowsPerQuery)
                {
                    _logger.LogWarning(
                        "Query returned more than {MaxRows} rows, truncating results",
                        _config.MaxRowsPerQuery);
                    break;
                }
            }

            return new OSQueryResult
            {
                Success = true,
                RawJson = output,
                Data = results,
                RowCount = results.Count
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse OSQuery JSON output");
            throw new OSQueryException("Failed to parse OSQuery output as JSON", ex);
        }
    }

    /// <summary>
    /// Converts a JsonElement to an appropriate .NET object.
    /// </summary>
    private static object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value)),
            _ => element.GetRawText()
        };
    }
}
