using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

// All types that will be serialized to JSON must be listed here
[JsonSerializable(typeof(OpenGraph))]
[JsonSerializable(typeof(OpenGraphElements))]
[JsonSerializable(typeof(OpenGraphBase<OpenGraphElements>))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, IncludeFields = true, IgnoreReadOnlyFields = false)]
internal partial class OpenGraphSerializationContext : JsonSerializerContext
{
}
