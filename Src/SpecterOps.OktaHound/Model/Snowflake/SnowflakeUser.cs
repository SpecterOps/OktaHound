using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Snowflake;

internal static class SnowflakeUser
{
    private const string NodeKind = "SNOW_User";

    public static OpenGraphEdgeNode? CreateEdgeNode(string userName, string? accountName)
    {
        if (userName is null || accountName is null)
        {
            return null;
        }

        string objectId = $"{accountName}.{userName}".ToUpperInvariant(); // Example: CGXOVHZ-NR46411.JOHN.DOE@CONTOSO.COM

        return new OpenGraphEdgeNode(objectId, NodeKind, NodeMatchType.Id);
    }
}
