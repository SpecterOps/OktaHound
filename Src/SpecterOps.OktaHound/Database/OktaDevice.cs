using System.Text.Json.Serialization;
using System.Text.Json;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaDevice : OktaEntity
{
    public const string NodeKind = "Okta_Device";
    public const string DeviceOfEdgeKind = "Okta_DeviceOf";

    /// <summary>
    /// The original identifier of the device as defined in Okta.
    /// </summary>
    [JsonPropertyName("oktaId")]
    public string OriginalId { get; private set; } = string.Empty;

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public string? Status { get; set; }
    public string? ResourceType { get; set; }
    public string? Platform { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public bool? Registered { get; set; }
    public bool? SecureHardwarePresent { get; set; }
    public bool? JailBreak { get; set; }
    public string? ObjectSid { get; set; }
    public string? SerialNumber { get; set; }

    /// <summary>
    /// The unique device ID (UDID).
    /// </summary>
    /// <remarks>
    /// The UDID is a unique number for identifying Apple devices on an iOS, macOS, tvOS, or watchOS platform.
    /// </remarks>
    [JsonPropertyName("udid")]
    public string? UDID { get; set; }

    [JsonIgnore]
    public List<OktaUser> Owners { get; set; } = [];

    protected override string[] Kinds => [NodeKind];

    private OktaDevice() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaDevice(DeviceList device, string domainName) : base(CreateDeviceId(device, domainName), device.ResourceDisplayName.Value, domainName)
    {
        DisplayName = device.ResourceDisplayName.Value;
        OriginalId = device.Id;
        Created = device.Created;
        LastUpdated = device.LastUpdated;
        Status = device.Status?.Value;
        ResourceType = device.ResourceType;
        Platform = device.Profile.Platform?.Value;
        Manufacturer = device.Profile.Manufacturer;
        Model = device.Profile.Model;
        OsVersion = device.Profile.OsVersion;
        Registered = device.Profile.Registered;
        SecureHardwarePresent = device.Profile.SecureHardwarePresent;
        JailBreak = device.Profile.IntegrityJailbreak;
        UDID = device.Profile.Udid;
        ObjectSid = device.Profile.Sid;

        if (!string.IsNullOrEmpty(device.Profile.SerialNumber))
        {
            // Okta SDK sometimes sends serial numbers as empty strings instead of null.
            SerialNumber = device.Profile.SerialNumber;
        }
    }

    private static string CreateDeviceId(DeviceList device, string oktaDomain)
    {
        // Use the Apple Mac UDID qualified with the Okta domain. This is to simplify hybrid edge creation by JamfHound.
        // Fallback to the Okta device ID for any other device types.
        return !string.IsNullOrEmpty(device.Profile.Udid) ? $"{device.Profile.Udid}@{oktaDomain}" : device.Id;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaDevice);
    }
}
