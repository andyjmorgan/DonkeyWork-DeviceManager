using System.Runtime.InteropServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

public class DeviceInformationDiscoveryService : IDeviceInformationDiscoveryService
{
    public PostDeviceInformationRequest DiscoverDeviceInformation()
    {
        PostDeviceInformationRequest postDeviceInformationRequest = new();
        postDeviceInformationRequest.DeviceName = Environment.MachineName;
        postDeviceInformationRequest.CpuCores = Environment.ProcessorCount;
        postDeviceInformationRequest.TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        postDeviceInformationRequest.OSArchitecture = RuntimeInformation.OSArchitecture;
        postDeviceInformationRequest.Architecture = RuntimeInformation.OSArchitecture;
        postDeviceInformationRequest.OperatingSystemVersion = RuntimeInformation.OSDescription;
        GetPlatformType(postDeviceInformationRequest);
        return postDeviceInformationRequest;
    }

    private static void GetPlatformType(PostDeviceInformationRequest postDeviceInformationRequest)
    {
        if(OperatingSystem.IsLinux())
        {
            postDeviceInformationRequest.OperatingSystem = OperatingSystemType.Linux;
        }
        else if(OperatingSystem.IsWindows())
        {
            postDeviceInformationRequest.OperatingSystem = OperatingSystemType.Windows;
        }
        else if(OperatingSystem.IsMacOS())
        {
            postDeviceInformationRequest.OperatingSystem = OperatingSystemType.MacOS;
        }
        else
        {
            postDeviceInformationRequest.OperatingSystem = OperatingSystemType.Unknown;
        }
    }
}