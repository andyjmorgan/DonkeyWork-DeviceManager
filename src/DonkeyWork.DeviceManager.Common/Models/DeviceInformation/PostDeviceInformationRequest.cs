namespace DonkeyWork.DeviceManager.Common.Models.DeviceInformation;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

public class PostDeviceInformationRequest
{
    public string DeviceName { get; set; } = string.Empty;
    
    public int CpuCores { get; set; }
    
    public Int64 TotalMemoryBytes { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperatingSystemType OperatingSystem { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Architecture OSArchitecture { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Architecture Architecture { get; set; }
    
    public string OperatingSystemVersion { get; set; } = string.Empty;
}