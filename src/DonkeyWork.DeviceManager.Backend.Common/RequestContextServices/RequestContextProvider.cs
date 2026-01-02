namespace DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;

/// <summary>
/// The request context provider.
/// </summary>
public class RequestContextProvider : IRequestContextProvider
{
    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    public RequestContext Context { get; set; } = new  RequestContext();
}