using System.Text.Json;
using DonkeyWork.DeviceManager.DeviceClient.Models;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Storage;

/// <summary>
/// Service for persisting device tokens to a local JSON file.
/// </summary>
public class TokenStorageService : ITokenStorageService
{
    private const string TokenFileName = "device-tokens.json";
    private readonly string _tokenFilePath;
    private readonly ILogger<TokenStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TokenStorageService(ILogger<TokenStorageService> logger)
    {
        _logger = logger;

        // Store in current working directory
        _tokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), TokenFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogDebug("Token storage path: {TokenFilePath}", _tokenFilePath);
    }

    public async Task<DeviceTokens?> LoadTokensAsync()
    {
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                _logger.LogInformation("No token file found at {TokenFilePath}", _tokenFilePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_tokenFilePath);
            var tokens = JsonSerializer.Deserialize<DeviceTokens>(json, _jsonOptions);

            if (tokens == null)
            {
                _logger.LogWarning("Failed to deserialize tokens from {TokenFilePath}", _tokenFilePath);
                return null;
            }

            _logger.LogInformation("Successfully loaded device tokens. Device ID: {DeviceUserId}, Tenant ID: {TenantId}",
                tokens.DeviceUserId, tokens.TenantId);

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tokens from {TokenFilePath}", _tokenFilePath);
            return null;
        }
    }

    public async Task SaveTokensAsync(DeviceTokens tokens)
    {
        try
        {
            var json = JsonSerializer.Serialize(tokens, _jsonOptions);
            await File.WriteAllTextAsync(_tokenFilePath, json);

            _logger.LogInformation("Successfully saved device tokens. Device ID: {DeviceUserId}, Tenant ID: {TenantId}",
                tokens.DeviceUserId, tokens.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens to {TokenFilePath}", _tokenFilePath);
            throw;
        }
    }
}
