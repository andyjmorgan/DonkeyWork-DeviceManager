# SignalR Hub Patterns

This document describes the modern SignalR communication patterns used in DonkeyWork DeviceManager.

## Overview

The application uses **only modern SignalR patterns** (request-response and streaming) for real-time bidirectional communication between:
- **UserHub**: Frontend users (web browsers) connect here
- **DeviceHub**: Device clients connect here
- **DeviceRegistrationHub**: Unauthenticated devices connect for initial registration

**No legacy fire-and-forget patterns** - all communication uses awaitable request-response or efficient streaming.

## Architecture

### Strongly-Typed Client Interfaces

We use strongly-typed client interfaces for compile-time safety and IntelliSense support:

**IDeviceClient** (`src/DonkeyWork.DeviceManager.Api/Hubs/IDeviceClient.cs`):
- Defines methods that can be invoked on device clients
- Used by UserHub via `IHubContext<DeviceHub, IDeviceClient>`
- All methods return `Task<T>` or `IAsyncEnumerable<T>`

**IUserClient** (`src/DonkeyWork.DeviceManager.Api/Hubs/IUserClient.cs`):
- Defines methods that can be invoked on user clients (web browsers)
- Used by DeviceHub via `IHubContext<UserHub, IUserClient>`
- Used for broadcasting status notifications to all users

### Hub Configuration

```csharp
// Program.cs
builder.Services.AddSignalR(hubOptions =>
{
    // Enable request-response pattern: allows InvokeAsync from hub methods
    // Required for client results and awaiting responses from clients
    hubOptions.MaximumParallelInvocationsPerClient = 10;
})
```

**MaximumParallelInvocationsPerClient** must be > 1 to enable:
- `InvokeAsync<T>()` - Request-response pattern
- `StreamAsync<T>()` - Streaming pattern

## Communication Patterns

### 1. Request-Response Pattern

The request-response pattern allows hubs to invoke methods on clients and await their responses. No manual command correlation needed.

#### Example: Ping Device

**UserHub** (server-side):
```csharp
public async Task<int?> PingDevice(Guid deviceId, int timeoutSeconds = 30)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

    var latencyMs = await _deviceHubContext.Clients.User(deviceId.ToString())
        .InvokeAsync<int>("MeasurePing", commandId, DateTimeOffset.UtcNow, userId, cts.Token);

    return latencyMs;
}
```

**Device Client** (client-side):
```csharp
// Device client implements a handler that returns a value
hubConnection.On<Guid, DateTimeOffset, Guid, int>("MeasurePing",
    (commandId, timestamp, requestedBy) =>
{
    var startTime = Stopwatch.GetTimestamp();
    // Perform ping measurement
    var elapsed = Stopwatch.GetElapsedTime(startTime);
    return (int)elapsed.TotalMilliseconds;
});
```

**Benefits**:
- No manual command ID correlation
- Built-in timeout support via CancellationToken
- Exceptions propagate from client to server
- Type-safe with generics
- Direct return values to caller

#### Available Request-Response Methods

| Method | Hub | Parameters | Returns | Timeout |
|--------|-----|------------|---------|---------|
| `PingDevice` | UserHub | deviceId, timeoutSeconds? | `int?` (latency ms) | 30s |
| `ShutdownDevice` | UserHub | deviceId, timeoutSeconds? | `CommandResult?` | 30s |
| `RestartDevice` | UserHub | deviceId, timeoutSeconds? | `CommandResult?` | 30s |

### 2. Streaming Pattern

The streaming pattern allows efficient handling of large datasets by sending data incrementally rather than buffering entire results.

#### Example: OSQuery Streaming

**UserHub** (server-side):
```csharp
public async IAsyncEnumerable<OSQueryResultRow> ExecuteOSQuery(Guid deviceId, string query)
{
    var resultStream = _deviceHubContext.Clients.User(deviceId.ToString())
        .StreamAsync<OSQueryResultRow>("ExecuteStreamingOSQuery",
            executionId, query, DateTimeOffset.UtcNow, userId);

    await foreach (var row in resultStream)
    {
        yield return row;
    }
}
```

**Device Client** (client-side):
```csharp
// Device client implements a streaming handler
hubConnection.On<Guid, string, DateTimeOffset, Guid>("ExecuteStreamingOSQuery",
    async (executionId, query, timestamp, requestedBy) =>
{
    // Return IAsyncEnumerable
    return ExecuteQueryStreamAsync(query);
});

private async IAsyncEnumerable<OSQueryResultRow> ExecuteQueryStreamAsync(string query)
{
    var rowNumber = 0;
    await foreach (var row in _osquery.ExecuteAsync(query))
    {
        yield return new OSQueryResultRow
        {
            RowJson = JsonSerializer.Serialize(row),
            RowNumber = ++rowNumber
        };
    }
}
```

**Benefits**:
- Memory efficient - no buffering of large datasets
- Progressive rendering - users see results as they arrive
- Cancellation support - can cancel mid-stream
- Backpressure handling - producer waits if consumer is slow
- Perfect for large query results or log tailing

## Client Implementation Guide

### Device Client Requirements

Device clients must implement handlers for request-response and streaming patterns:

```csharp
// Request-response handlers (return values directly)
hubConnection.On<Guid, DateTimeOffset, Guid, int>("MeasurePing",
    (commandId, timestamp, requestedBy) =>
{
    // Measure and return latency
    return MeasurePingLatency();
});

hubConnection.On<Guid, DateTimeOffset, Guid, CommandResult>("ExecuteShutdown",
    (commandId, timestamp, requestedBy) =>
{
    // Execute shutdown and return result
    return ExecuteShutdownCommand();
});

hubConnection.On<Guid, DateTimeOffset, Guid, CommandResult>("ExecuteRestart",
    (commandId, timestamp, requestedBy) =>
{
    // Execute restart and return result
    return ExecuteRestartCommand();
});

// Streaming handlers (return IAsyncEnumerable)
hubConnection.On<Guid, string, DateTimeOffset, Guid, IAsyncEnumerable<OSQueryResultRow>>(
    "ExecuteStreamingOSQuery",
    (executionId, query, timestamp, requestedBy) =>
{
    return ExecuteOSQueryStream(query);
});
```

### User Client (Browser) Requirements

Browsers receive status notifications from devices:

```javascript
// Connect to UserHub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/user")
    .build();

// Receive device status updates
connection.on("ReceiveDeviceStatus", (status) => {
    console.log(`Device ${status.deviceId} is ${status.isOnline ? 'online' : 'offline'}`);
});

await connection.start();

// Invoke request-response methods
const latency = await connection.invoke("PingDevice", deviceId, 30);
console.log(`Device latency: ${latency}ms`);

// Invoke streaming methods
const stream = connection.stream("ExecuteOSQuery", deviceId, "SELECT * FROM processes");
stream.subscribe({
    next: (row) => console.log("Row:", row),
    complete: () => console.log("Query complete"),
    error: (err) => console.error("Error:", err)
});
```

## Error Handling

### Request-Response Pattern

Exceptions in client handlers propagate to server:

```csharp
try
{
    var result = await _deviceHubContext.Clients.User(deviceId)
        .InvokeAsync<CommandResult>("ExecuteShutdown", ...);
}
catch (HubException ex) // Device threw exception
{
    _logger.LogError(ex, "Device {DeviceId} failed to shutdown", deviceId);
}
catch (OperationCanceledException) // Timeout
{
    _logger.LogWarning("Device {DeviceId} did not respond", deviceId);
}
```

### Streaming Pattern

Errors during streaming stop the stream:

```csharp
await foreach (var row in resultStream)
{
    try
    {
        yield return row;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing row");
        yield break; // Stop streaming
    }
}
```

## Performance Considerations

### Request-Response
- Minimal overhead vs fire-and-forget (< 1ms additional latency)
- Timeout values should be tuned based on expected latency
- Connection pool limits matter when many requests are in flight
- Supports up to 10 parallel invocations per client (configurable)

### Streaming
- Much more efficient than buffering for large datasets
- Memory usage proportional to backpressure, not dataset size
- Cancellation tokens should be passed through entire pipeline
- Consider chunking/batching for very high-frequency data
- No memory overhead for million-row datasets

## Type Definitions

### CommandResult
```csharp
public class CommandResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

### OSQueryResultRow
```csharp
public class OSQueryResultRow
{
    public string RowJson { get; set; }
    public int RowNumber { get; set; }
}
```

### DeviceStatus
```csharp
public class DeviceStatus
{
    public Guid DeviceId { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Reason { get; set; }
}
```

## Comparison with Legacy Patterns

| Aspect | Old Fire-and-Forget | New Request-Response & Streaming |
|--------|---------------------|----------------------------------|
| **Command Correlation** | Manual GUID tracking | Automatic framework correlation |
| **Type Safety** | String-based method names | Strongly-typed interfaces |
| **Timeout Handling** | Manual implementation | Built-in CancellationToken support |
| **Error Handling** | Manual success/error flags | Exception propagation |
| **Large Data Sets** | Buffered in memory | Streamed incrementally |
| **API Complexity** | Separate send/receive pairs | Single awaitable methods |
| **Memory Efficiency** | High for large payloads | Constant, independent of size |
| **User Experience** | Wait for complete result | Progressive rendering |

## Migration from Legacy Code

If you have existing device clients using the old pattern, they need to be updated:

### Before (Legacy - DO NOT USE):
```csharp
// Old fire-and-forget handler
hubConnection.On<object>("ReceivePingCommand", async (commandData) =>
{
    var commandId = commandData.CommandId;
    var latency = MeasurePing();

    // Manual response send
    await hubConnection.InvokeAsync("SendPingResponse", commandId, latency);
});
```

### After (Modern):
```csharp
// New request-response handler
hubConnection.On<Guid, DateTimeOffset, Guid, int>("MeasurePing",
    (commandId, timestamp, requestedBy) =>
{
    return MeasurePing(); // Direct return, no separate send
});
```

## References

- [ASP.NET Core SignalR Hubs](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs)
- [SignalR Streaming](https://learn.microsoft.com/en-us/aspnet/core/signalr/streaming)
- [SignalR Client Results (ASP.NET Core 7.0+)](https://devblogs.microsoft.com/dotnet/asp-net-core-updates-in-dotnet-7-preview-4/#signalr-client-results)
- [IAsyncEnumerable in C#](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/generate-consume-asynchronous-stream)
