namespace DonkeyWork.DeviceManager.Api.DTOs;

/// <summary>
/// User profile data transfer object.
/// </summary>
public record UserProfileDto
{
    /// <summary>
    /// Gets the user's unique identifier.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public required string Email { get; init; }
}
