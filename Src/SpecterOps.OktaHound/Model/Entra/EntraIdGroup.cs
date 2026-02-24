using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Entra;

internal static class EntraIdGroup
{
    private const string NodeKind = "AZGroup";

    public static OpenGraphEdgeNode? CreateEdgeNode(string groupName, string? tenantId)
    {
        if (groupName is null || tenantId is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "displayName", groupName },
            { "tenantId", tenantId }
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
