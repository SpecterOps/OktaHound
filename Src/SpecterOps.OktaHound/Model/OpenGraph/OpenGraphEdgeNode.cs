using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal sealed class OpenGraphEdgeNode
{
    [JsonPropertyName("match_by")]
    public readonly NodeMatchType MatchBy;

    [JsonPropertyName("property_matchers")]
    public readonly List<PropertyMatcher>? PropertyMatchers;

    [JsonPropertyName("value")]
    public readonly string? Value;

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
        this.Kind = kind;

        // Only support equality matchers for now, so convert the properties to PropertyMatchers with the "equals" operator.
        this.PropertyMatchers = [.. properties.Select(kvp => new PropertyMatcher(kvp.Key, kvp.Value))];
    }
}
