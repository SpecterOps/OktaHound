using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Jamf;

internal static class JamfTenant
{
    private const string NodeKind = "jamf_SSOIntegration";

    public static OpenGraphEdgeNode? CreateEdgeNode(string? domainName)
    {
        if (domainName is null)
        {
            return null;
        }

        string ssoObjectId = $"{domainName}-SSO"; // Example: sol.jamfcloud.com-SSO
        return new OpenGraphEdgeNode(ssoObjectId, NodeKind, NodeMatchType.Id);
    }
}
