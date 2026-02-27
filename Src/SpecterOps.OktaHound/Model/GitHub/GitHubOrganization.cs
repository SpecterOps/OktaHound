using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.GitHub;

internal static class GitHubOrganization
{
    private const string NodeKind = "GH_Organization";

    public static OpenGraphEdgeNode? CreateEdgeNode(string? organizationName)
    {
        if (organizationName is null)
        {
            return null;
        }

        return new OpenGraphEdgeNode(organizationName, NodeKind, NodeMatchType.Name);
    }
}
