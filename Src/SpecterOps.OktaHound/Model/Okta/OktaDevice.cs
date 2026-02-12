using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaDevice : OktaNode
{
    public const string NodeKind = "Okta_Device";
    // TODO: Rename Okta_OwnsDevice to Okta_DeviceOf for clarity.
    public const string OwnsDeviceEdgeKind = "Okta_OwnsDevice";
    private const string UdidPropertyName = "udid";
    private const string OriginalIdPropertyName = "oktaId";

    [JsonIgnore]
    public override string OriginalId => GetProperty<string>(OriginalIdPropertyName) ?? Id;

    /// <summary>
    /// The unique device ID (UDID).
    /// </summary>
    /// <remarks>
    /// The UDID is a unique number for identifying Apple devices on an iOS, macOS, tvOS, or watchOS platform.
    /// </remarks>
    [JsonIgnore]
    public string? UDID => GetProperty<string>(UdidPropertyName);

    public OktaDevice(DeviceList device, string domainName) : base(CreateDeviceId(device, domainName), domainName, NodeKind)
    {
        Name = device.ResourceDisplayName.Value;
        DisplayName = device.ResourceDisplayName.Value;

        // Store the original Okta device ID for reference.
        // This value will differ from the node ID for Apple Mac devices.
        SetProperty(OriginalIdPropertyName, device.Id);

        SetProperty("created", device.Created);
        SetProperty("lastUpdated", device.LastUpdated);
        SetProperty("status", device.Status?.Value);
        SetProperty("resourceType", device.ResourceType);
        SetProperty("platform", device.Profile.Platform?.Value);
        SetProperty("manufacturer", device.Profile.Manufacturer);
        SetProperty("model", device.Profile.Model);
        SetProperty("osVersion", device.Profile.OsVersion);
        SetProperty("registered", device.Profile.Registered);
        SetProperty("secureHardwarePresent", device.Profile.SecureHardwarePresent);
        SetProperty("jailBreak", device.Profile.IntegrityJailbreak);
        SetProperty(UdidPropertyName, device.Profile.Udid);
        SetProperty("objectSid", device.Profile.Sid);

        if (!string.IsNullOrEmpty(device.Profile.SerialNumber))
        {
            // Okta SDK sometimes sends serial numbers as empty strings instead of null.
            SetProperty("serialNumber", device.Profile.SerialNumber);
        }
    }

    private static string CreateDeviceId(DeviceList device, string oktaDomain)
    {
        // Use the Apple Mac UDID qualified with the Okta domain. This is to simplify hybrid edge creation by JamfHound.
        // Fallback to the Okta device ID for any other device types.
        return !string.IsNullOrEmpty(device.Profile.Udid) ? $"{device.Profile.Udid}@{oktaDomain}" : device.Id;
    }
}
