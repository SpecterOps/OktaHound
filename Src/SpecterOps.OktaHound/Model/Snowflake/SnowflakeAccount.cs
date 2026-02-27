using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Snowflake;

internal static class SnowflakeAccount
{
    private const string NodeKind = "SNOW_Account";

    public static OpenGraphEdgeNode? CreateEdgeNode(string? accountName)
    {
        if (accountName is null)
        {
            return null;
        }

        return new OpenGraphEdgeNode(accountName.ToUpperInvariant(), NodeKind, NodeMatchType.Id);
    }
}
