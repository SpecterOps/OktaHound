using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaRealm : OktaNode
{
    public const string RealmContainsEdgeKind = "Okta_RealmContains";
    private const string NodeKind = "Okta_Realm";

    public OktaRealm(Realm realm, string domainName) : base(realm.Id, domainName, NodeKind)
    {
        Name = realm.Profile?.Name;
        DisplayName = realm.Profile?.Name;

        SetProperty("created", realm.Created);
        SetProperty("isDefault", realm.IsDefault);
        SetProperty("type", realm.Profile?.RealmType?.Value);
    }
}
