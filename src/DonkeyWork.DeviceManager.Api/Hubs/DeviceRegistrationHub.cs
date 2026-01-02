namespace DonkeyWork.DeviceManager.Api.Hubs;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Registration;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// SignalR hub for device registration.
/// </summary>
public class DeviceRegistrationHub : Hub
{
    private readonly DeviceManagerContext _dbContext;
    private readonly ThreeWordCodeGenerator _codeGenerator;
    private readonly ILogger<DeviceRegistrationHub> _logger;

    public DeviceRegistrationHub(
        DeviceManagerContext dbContext,
        ThreeWordCodeGenerator codeGenerator,
        ILogger<DeviceRegistrationHub> logger)
    {
        _dbContext = dbContext;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Called when device connects and requests registration.
    /// </summary>
    public async Task<DeviceRegistrationResponse> RequestRegistration()
    {
        var connectionId = Context.ConnectionId;
        
        // Generate unique three-word code
        string threeWordCode;
        bool isUnique;
        do
        {
            threeWordCode = _codeGenerator.Generate();
            isUnique = !await _dbContext.DeviceRegistrations
                .AnyAsync(r => r.ThreeWordRegistration == threeWordCode);
        } while (!isUnique);

        // Create registration record
        var registration = new DeviceRegistrationEntity
        {
            ConnectionId = connectionId,
            ThreeWordRegistration = threeWordCode
        };

        _dbContext.DeviceRegistrations.Add(registration);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Device registration initiated - ConnectionId: {ConnectionId}, Code: {Code}, RegistrationId: {RegistrationId}",
            connectionId, threeWordCode, registration.Id);

        return new DeviceRegistrationResponse
        {
            ThreeWordCode = threeWordCode,
            RegistrationId = registration.Id
        };
    }

    /// <summary>
    /// Called when SignalR connection disconnects - cleanup registration.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        var registration = await _dbContext.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.ConnectionId == connectionId);

        if (registration != null)
        {
            _dbContext.DeviceRegistrations.Remove(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Device disconnected - Removed registration for ConnectionId: {ConnectionId}, Code: {Code}",
                connectionId, registration.ThreeWordRegistration);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
