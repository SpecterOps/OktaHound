using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Jamf;

internal static class JamfTenant
{
    private const string NodeKind = "jamf_Tenant";

    public static OpenGraphEdgeNode? CreateEdgeNode(string? domainName)
    {
        if (domainName is null)
        {
            return null;
        }

        return new OpenGraphEdgeNode(domainName, NodeKind, NodeMatchType.Id);
    }
}
