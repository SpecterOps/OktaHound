namespace SpecterOps.OktaHound.Database;

public sealed class OktaIdentityProviderGovernedGroup
{
    public string IdentityProviderId { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public OktaIdentityProvider IdentityProvider { get; set; } = null!;

    public OktaGroup Group { get; set; } = null!;
}
