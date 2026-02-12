using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal sealed class OpenGraphEdgeNode(string value, string? kind = null, string matchBy = "id")
{
    [JsonPropertyName("value")]
    public readonly string Value = value;

    [JsonPropertyName("match_by")]
    public readonly string MatchBy = matchBy;

    [JsonPropertyName("kind")]
    public readonly string? Kind = kind;
}
