using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.ActiveDirectory;

internal sealed class ActiveDirectoryDomain : OpenGraphNode
{
    public const string ContainsEdgeKind = "Contains";
    private const string NodeKind = "Domain";

    public ActiveDirectoryDomain(string sid, string fqdn) : base(sid, [NodeKind])
    {
        Name = fqdn;
        DisplayName = fqdn;
    }

    public static string? ParseObjectGuid(string externalId)
    {
        try
        {
            return new Guid(Convert.FromBase64String(externalId)).ToString();
        }
        catch
        {
            // Do not cause any error if the conversion failed
            return null;
        }
    }

    public static OpenGraphEdgeNode CreateEdgeNode(string domainSid, string matchBy = "id") => new(domainSid, NodeKind, matchBy);
}
