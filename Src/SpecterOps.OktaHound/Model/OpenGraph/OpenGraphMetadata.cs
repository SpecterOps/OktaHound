using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

public readonly struct OpenGraphMetadata(string? sourceKind)
{
    [JsonPropertyName("source_kind")]
    public string? SourceKind { get; } = sourceKind;
}
