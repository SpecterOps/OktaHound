using System.Text.Json.Serialization;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal abstract class OktaNode : OpenGraphNode
{
    private const string DomainNamePropertyKey = "oktaDomain";

    [JsonIgnore]
    public string DomainName
    {
        get => GetProperty<string>(DomainNamePropertyKey) ?? throw new ArgumentNullException("Domain name is missing or invalid.");
        set => SetProperty(DomainNamePropertyKey, value);
    }

    public OktaNode(string nodeId, string oktaOrganizationId, string oktaDomainName, string kind) : base(nodeId, [kind])
    {
        DomainName = oktaDomainName;
        EnvironmentId = oktaOrganizationId;
    }

    public OktaNode(string nodeId, OktaOrganization organization, string kind) : base(nodeId, [kind])
    {
        DomainName = organization.DomainName;
        EnvironmentId = organization.Id;
    }

    public static OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, OktaGraph.OktaSourceKind);
}
