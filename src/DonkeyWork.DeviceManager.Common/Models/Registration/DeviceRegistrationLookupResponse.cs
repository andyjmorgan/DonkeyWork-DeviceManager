namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Response from device registration lookup containing user's buildings and rooms.
/// </summary>
public record DeviceRegistrationLookupResponse
{
    /// <summary>
    /// Gets the registration ID.
    /// </summary>
    public required Guid RegistrationId { get; init; }

    /// <summary>
    /// Gets the three-word code.
    /// </summary>
    public required string ThreeWordCode { get; init; }

    /// <summary>
    /// Gets the list of buildings and their rooms.
    /// </summary>
    public required List<BuildingResponse> Buildings { get; init; }
}
