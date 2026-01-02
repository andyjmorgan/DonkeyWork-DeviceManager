namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Response when device initiates registration.
/// </summary>
public record DeviceRegistrationResponse
{
    /// <summary>
    /// Gets the three-word registration code to display to user.
    /// </summary>
    public required string ThreeWordCode { get; init; }

    /// <summary>
    /// Gets the registration ID.
    /// </summary>
    public required Guid RegistrationId { get; init; }
}
