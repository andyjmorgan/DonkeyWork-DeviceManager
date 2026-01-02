namespace DonkeyWork.DeviceManager.Common.Models.DeviceInformation;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperatingSystemType
{
    Unknown,
    Windows,
    Linux,
    MacOS,
}