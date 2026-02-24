using System.Diagnostics.CodeAnalysis;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Entra;

internal static class EntraIdUser
{
    private const string NodeKind = "AZUser";

    [return: NotNullIfNotNull(nameof(tenantId))]
    public static OpenGraphEdgeNode? CreateEdgeNode(string? userPrincipalName, string? tenantId)
    {
        if (userPrincipalName is null || tenantId is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "userPrincipalName", userPrincipalName },
            { "tenantId", tenantId }
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
