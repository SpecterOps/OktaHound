using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaOrganization : OktaNode
{
    public const string ContainsEdgeKind = "Okta_Contains";
    public const string NodeKind = "Okta_Organization";

    [JsonIgnore]
    public bool AgentlessDssoEnabled { get; set; }

    public OktaOrganization(OrgSetting settings, string domainName) : base(settings.Id, domainName, NodeKind)
    {
        Name = domainName;
        DisplayName = settings.CompanyName;

        SetProperty("subdomain", settings.Subdomain);
        SetProperty("status", settings.Status?.Value);
        SetProperty("created", settings.Created);
        SetProperty("lastUpdated", settings.LastUpdated);
    }

    public OktaOrganization(string id, string domainName, string companyName) : base(id, domainName, NodeKind)
    {
        Name = domainName;
        DisplayName = companyName;
    }
}
