namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Request from user portal to complete registration.
/// </summary>
public record CompleteRegistrationRequest
{
    /// <summary>
    /// Gets or sets the three-word code entered by user.
    /// </summary>
    public required string ThreeWordCode { get; init; }

    /// <summary>
    /// Gets or sets the room ID where the device will be placed.
    /// </summary>
    public required Guid RoomId { get; init; }
}
