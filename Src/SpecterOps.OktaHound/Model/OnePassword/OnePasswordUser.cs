using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.OnePassword;

internal static class OnePasswordUser
{
    private const string NodeKind = "OP_User";

    public static OpenGraphEdgeNode? CreateEdgeNode(string email, string? accountName)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(accountName))
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "email", email }, // Example: john@contoso.com
            { "account_name", accountName } // Example: contoso.1Password.com
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
