using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.GitHub;

internal static class GitHubUser
{
    private const string NodeKind = "GH_User";

    public static OpenGraphEdgeNode? CreateEdgeNode(string userName, string? organizationName)
    {
        if (userName is null || organizationName is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "login", userName }, // Example: john
            { "environment_name", organizationName } // Example: ContosoTest
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
