namespace DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;

/// <summary>
/// An interface for holding the request context.
/// </summary>
public interface IRequestContextProvider
{
    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    RequestContext Context { get; set; }
}