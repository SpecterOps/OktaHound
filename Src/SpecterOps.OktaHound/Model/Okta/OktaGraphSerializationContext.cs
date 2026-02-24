using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model;

// All types that will be serialized to JSON must be listed here
[JsonSerializable(typeof(OktaGraph))]
[JsonSerializable(typeof(OktaGraphElements))]
[JsonSerializable(typeof(OpenGraphBase<OktaGraphElements>))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(JArray))] // For Okta custom app settings that are arrays but not strongly typed
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, IncludeFields = true, IgnoreReadOnlyFields = false, UseStringEnumConverter = true)]
internal partial class OktaGraphSerializationContext : JsonSerializerContext
{
}
