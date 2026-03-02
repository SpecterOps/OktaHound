namespace SpecterOps.OktaHound.Database;

public sealed class OktaDeviceOwner
{
    public string DeviceId { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public OktaDevice Device { get; set; } = null!;

    public OktaUser Owner { get; set; } = null!;
}
