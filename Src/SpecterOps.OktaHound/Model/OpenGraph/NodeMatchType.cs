using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

[JsonConverter(typeof(JsonStringEnumConverter<NodeMatchType>))]
public enum NodeMatchType
{
    [JsonStringEnumMemberName("id")]
    Id,

    [JsonStringEnumMemberName("name")]
    Name,

    [JsonStringEnumMemberName("properties")]
    Properties
}
