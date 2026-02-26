using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.ActiveDirectory;

internal static class ActiveDirectoryComputer
{
    private const string NodeKind = "Computer";

    public static OpenGraphEdgeNode? CreateEdgeNode(string computerName, string? domainName)
    {
        if (computerName is null || domainName is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "samaccountname", computerName.ToUpperInvariant() + '$' }, // Example: LON-SRV01$
            { "domain", domainName.ToUpperInvariant() } // Example: CONTOSO.COM
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
