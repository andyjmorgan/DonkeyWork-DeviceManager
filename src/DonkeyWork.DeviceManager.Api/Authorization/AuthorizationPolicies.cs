namespace DonkeyWork.DeviceManager.Api.Authorization;

/// <summary>
/// Authorization policy names.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy that allows only user connections (not devices).
    /// </summary>
    public const string UserOnly = "UserOnly";

    /// <summary>
    /// Policy that allows only device connections (not users).
    /// </summary>
    public const string DeviceOnly = "DeviceOnly";
}
