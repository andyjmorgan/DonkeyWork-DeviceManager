using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

public interface IDeviceInformationDiscoveryService
{
    public PostDeviceInformationRequest DiscoverDeviceInformation();
}