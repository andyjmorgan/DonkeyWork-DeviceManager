using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;
using Refit;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Api;

/// <summary>
/// Refit interface for Device Manager API calls.
/// </summary>
public interface IDeviceManagerApi
{
    /// <summary>
    /// Posts device hardware and system information to the backend.
    /// </summary>
    /// <param name="request">The device information to post.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API response.</returns>
    [Post("/api/device-information")]
    Task<IApiResponse> PostDeviceInformationAsync(
        [Body] PostDeviceInformationRequest request,
        CancellationToken cancellationToken = default);
}
