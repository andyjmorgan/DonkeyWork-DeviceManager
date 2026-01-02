namespace DonkeyWork.DeviceManager.Common.Models.Commands;

/// <summary>
/// Base class for device commands.
/// </summary>
public abstract record DeviceCommand
{
    /// <summary>
    /// Gets the unique identifier for this command.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Gets the timestamp when the command was issued.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the user ID who requested the command.
    /// </summary>
    public required Guid RequestedBy { get; init; }
}
