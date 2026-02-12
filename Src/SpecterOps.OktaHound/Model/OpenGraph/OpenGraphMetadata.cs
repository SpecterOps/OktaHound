using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal readonly struct OpenGraphMetadata(string? sourceKind)
{
    [JsonPropertyName("source_kind")]
    public readonly string? SourceKind = sourceKind;
}
