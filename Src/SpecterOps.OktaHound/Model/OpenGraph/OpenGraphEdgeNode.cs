using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal sealed class OpenGraphEdgeNode
{
    [JsonPropertyName("value")]
    public readonly string? Value;

    [JsonPropertyName("properties")]
    public readonly OrderedDictionary<string, string>? Properties;

    [JsonPropertyName("match_by")]
    public readonly NodeMatchType MatchBy;

    [JsonPropertyName("kind")]
    public readonly string? Kind;

    public OpenGraphEdgeNode(string value, string? kind = null, NodeMatchType matchBy = NodeMatchType.Id)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (matchBy != NodeMatchType.Id && matchBy != NodeMatchType.Name)
        {
            throw new ArgumentException($"Invalid match type '{matchBy}' for string value. Must be 'id' or 'name'.", nameof(matchBy));
        }

        this.Value = value;
        this.Kind = kind;
        this.MatchBy = matchBy;
    }

    public OpenGraphEdgeNode(OrderedDictionary<string, string> properties, string? kind = null)
    {
        ArgumentNullException.ThrowIfNull(properties);

        this.MatchBy = NodeMatchType.Properties;
        this.Properties = properties;
        this.Kind = kind;
    }
}
