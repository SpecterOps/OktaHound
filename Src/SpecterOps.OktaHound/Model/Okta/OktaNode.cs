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

    public OktaNode(string id, string oktaOrganization, string kind) : base(id, [kind])
    {
        SetProperty(DomainNamePropertyKey, oktaOrganization);
    }

    public static OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, OktaGraph.OktaSourceKind);
}
