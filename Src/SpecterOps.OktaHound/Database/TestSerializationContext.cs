using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Database;

// All types that will be serialized to JSON must be listed here
[JsonSerializable(typeof(OktaEntity))]
[JsonSerializable(typeof(OktaUser))]
[JsonSerializable(typeof(OktaGroup))]
[JsonSerializable(typeof(OktaOrganization))]
[JsonSerializable(typeof(OktaAgent))]
[JsonSerializable(typeof(OktaAgentPool))]
[JsonSerializable(typeof(OktaApplication))]
[JsonSerializable(typeof(OktaApiServiceIntegration))]
[JsonSerializable(typeof(OktaApiToken))]
[JsonSerializable(typeof(OktaAuthorizationServer))]
[JsonSerializable(typeof(OktaBuiltinRole))]
[JsonSerializable(typeof(OktaClientSecret))]
[JsonSerializable(typeof(OktaCustomRole))]
[JsonSerializable(typeof(OktaDevice))]
[JsonSerializable(typeof(OktaIdentityProvider))]
[JsonSerializable(typeof(OktaJWK))]
[JsonSerializable(typeof(OktaPolicy))]
[JsonSerializable(typeof(OktaRealm))]
[JsonSerializable(typeof(OktaResourceSet))]
[JsonSerializable(typeof(OktaRoleAssignment))]
[JsonSerializable(typeof(OpenGraphEntity))]
[JsonSerializable(typeof(Model.OpenGraph.OpenGraphMetadata))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true,
    IgnoreReadOnlyFields = false,
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true
)]
internal partial class TestSerializationContext : JsonSerializerContext
{
}
