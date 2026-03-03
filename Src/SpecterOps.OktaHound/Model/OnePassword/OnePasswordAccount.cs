using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.OnePassword;

internal static class OnePasswordAccount
{
    private const string NodeKind = "OP_Account";

    public static string? GetDomain(string? subDomain, string? regionType)
    {
        if (regionType != null && subDomain != null)
        {
            // Example: contoso.1password.com
            return $"{subDomain}.1password.{regionType}";
        }
        else
        {
            return null;
        }
    }

    public static OpenGraphEdgeNode? CreateEdgeNode(string? domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "domain", domain }
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
