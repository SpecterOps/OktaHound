using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Jamf;

internal static class JamfAccount
{
    private const string NodeKind = "jamf_Account";

    public static OpenGraphEdgeNode? CreateEdgeNode(string accountName, string? domainName)
    {
        if (accountName is null || domainName is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "email", accountName }, // Example: john@contoso.com
            { "domainName", domainName } // Example: sol.jamfcloud.com
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
