# SignalR Hub Patterns

This document describes the SignalR communication patterns used in DonkeyWork DeviceManager.

## Overview

The application uses SignalR for real-time bidirectional communication between:
- **UserHub**: Frontend users (web browsers) connect here
- **DeviceHub**: Device clients connect here
- **DeviceRegistrationHub**: Unauthenticated devices connect for initial registration

## Architecture

### Strongly-Typed Client Interfaces

We use strongly-typed client interfaces for compile-time safety and better IDE support:

**IDeviceClient** (`src/DonkeyWork.DeviceManager.Api/Hubs/IDeviceClient.cs`):
- Defines methods that can be invoked on device clients
- Used by UserHub to send commands to devices

**IUserClient** (`src/DonkeyWork.DeviceManager.Api/Hubs/IUserClient.cs`):
- Defines methods that can be invoked on user clients (web browsers)
- Used by DeviceHub to send notifications to users

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

### 1. Request-Response Pattern (New)

The request-response pattern allows hubs to invoke methods on clients and await their responses. This eliminates manual command correlation with GUIDs.

#### Example: Ping Device

**UserHub** (server-side):
```csharp
public async Task<int?> PingDeviceWithResponse(Guid deviceId, int timeoutSeconds = 30)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

    var latencyMs = await _deviceHubContext.Clients.User(deviceId.ToString())
        .InvokeAsync<int>("MeasurePing", commandId, DateTimeOffset.UtcNow, userId, cts.Token);

    return latencyMs;
}
```

**Device Client** (client-side):
```csharp
// Device client must implement a handler that returns a value
hubConnection.On<Guid, DateTimeOffset, Guid, int>("MeasurePing",
    async (commandId, timestamp, requestedBy) =>
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

#### Available Request-Response Methods

| Method | Hub | Returns | Timeout |
|--------|-----|---------|---------|
| `PingDeviceWithResponse` | UserHub | `int?` (latency ms) | 30s |
| `ShutdownDeviceWithResponse` | UserHub | `CommandResult?` | 30s |
| `RestartDeviceWithResponse` | UserHub | `CommandResult?` | 30s |

### 2. Streaming Pattern (New)

The streaming pattern allows efficient handling of large datasets by sending data incrementally rather than buffering entire results.

#### Example: OSQuery Streaming

**UserHub** (server-side):
```csharp
public async IAsyncEnumerable<OSQueryResultRow> ExecuteStreamingOSQuery(Guid deviceId, string query)
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

### 3. Fire-and-Forget Pattern (Legacy)

The legacy pattern uses one-way messaging with manual command correlation. Still supported for backward compatibility.

#### Example: Ping Device (Legacy)

**UserHub**:
```csharp
public async Task PingDevice(Guid deviceId)
{
    var commandId = Guid.NewGuid();

    await _deviceHubContext.Clients.User(deviceId.ToString())
        .ReceivePingCommand(new
        {
            CommandId = commandId,
            Timestamp = DateTimeOffset.UtcNow,
            RequestedBy = userId
        });
}
```

**DeviceHub**:
```csharp
public async Task SendPingResponse(Guid commandId, int latencyMs)
{
    await _userHubContext.Clients.Group($"tenant:{tenantId}")
        .ReceivePingResponse(new PingResponse
        {
            CommandId = commandId,
            LatencyMs = latencyMs,
            Timestamp = DateTimeOffset.UtcNow
        });
}
```

**Device Client**:
```csharp
hubConnection.On<object>("ReceivePingCommand", async (commandData) =>
{
    var commandId = commandData.CommandId;
    var latency = MeasurePing();

    await hubConnection.InvokeAsync("SendPingResponse", commandId, latency);
});
```

**Drawbacks**:
- Manual command ID correlation
- No built-in timeout mechanism
- Requires separate send and receive methods
- More complex error handling

## Migration Strategy

New implementations should use request-response or streaming patterns. Legacy methods remain for backward compatibility:

1. **Phase 1** (Current): Both patterns coexist
   - New methods: `*WithResponse`, `ExecuteStreaming*`
   - Legacy methods: `PingDevice`, `ShutdownDevice`, etc.

2. **Phase 2**: Update device clients to support new patterns
   - Implement `MeasurePing()`, `ExecuteShutdown()`, etc.
   - Keep legacy handlers for backward compatibility

3. **Phase 3**: Update frontend to use new patterns
   - Switch API calls to new methods
   - Remove legacy method calls

4. **Phase 4**: Deprecate legacy methods
   - Mark legacy methods as `[Obsolete]`
   - Remove after transition period

## Client Implementation Guide

### Device Client Requirements

Device clients must implement handlers for both patterns:

```csharp
// Request-response handlers (return values)
hubConnection.On<Guid, DateTimeOffset, Guid, int>("MeasurePing", ...);
hubConnection.On<Guid, DateTimeOffset, Guid, CommandResult>("ExecuteShutdown", ...);
hubConnection.On<Guid, DateTimeOffset, Guid, CommandResult>("ExecuteRestart", ...);

// Streaming handlers (return IAsyncEnumerable)
hubConnection.On<Guid, string, DateTimeOffset, Guid, IAsyncEnumerable<OSQueryResultRow>>(
    "ExecuteStreamingOSQuery", ...);

// Legacy handlers (fire-and-forget)
hubConnection.On<object>("ReceivePingCommand", ...);
hubConnection.On<object>("ReceiveShutdownCommand", ...);
hubConnection.On<object>("ReceiveRestartCommand", ...);
hubConnection.On<object>("ReceiveOSQueryCommand", ...);
```

### User Client (Browser) Requirements

Browsers receive notifications from devices:

```javascript
connection.on("ReceivePingResponse", (response) => {
    console.log(`Device ${response.deviceId} latency: ${response.latencyMs}ms`);
});

connection.on("ReceiveCommandAcknowledgment", (ack) => {
    console.log(`Command ${ack.commandType}: ${ack.success ? 'success' : 'failed'}`);
});

connection.on("ReceiveOSQueryResult", (result) => {
    console.log(`Query ${result.executionId}: ${result.rowCount} rows`);
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
- Minimal overhead vs fire-and-forget
- Timeout values should be tuned based on expected latency
- Consider connection pool limits when many requests are in flight

### Streaming
- Much more efficient than buffering for large datasets
- Memory usage is proportional to backpressure, not dataset size
- Cancellation tokens should be passed through the entire pipeline
- Consider chunking/batching for very high-frequency data

### Fire-and-Forget
- Lowest latency for simple notifications
- No built-in confirmation of delivery
- Manual correlation adds complexity but no performance overhead

## References

- [ASP.NET Core SignalR Hubs](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs)
- [SignalR Streaming](https://learn.microsoft.com/en-us/aspnet/core/signalr/streaming)
- [SignalR Client Results (ASP.NET Core 7.0+)](https://devblogs.microsoft.com/dotnet/asp-net-core-updates-in-dotnet-7-preview-4/#signalr-client-results)
