using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaPolicy : OktaEntity
{
    public const string PolicyMappingEdgeKind = "Okta_PolicyMapping";
    public const string NodeKind = "Okta_Policy";

    [JsonPropertyName("type")]
    public string? PolicyType { get; set; }

    public int? Priority { get; set; }
    public bool? System { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? Created { get; set; }

    protected override string[] Kinds => [NodeKind];

    public static readonly PolicyTypeParameter[] PolicyTypes = [
        PolicyTypeParameter.OKTASIGNON,
        PolicyTypeParameter.PASSWORD,
        PolicyTypeParameter.MFAENROLL,
        PolicyTypeParameter.IDPDISCOVERY,
        PolicyTypeParameter.ACCESSPOLICY,
        PolicyTypeParameter.PROFILEENROLLMENT,
        PolicyTypeParameter.POSTAUTHSESSION,
        PolicyTypeParameter.ENTITYRISK,
        PolicyTypeParameter.DEVICESIGNALCOLLECTION
    ];

    private OktaPolicy() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaPolicy(Policy policy, string domainName) : base(policy.Id, policy.Name, domainName)
    {
        DisplayName = policy.Name;
        PolicyType = policy.Type?.Value;
        Priority = policy.Priority;
        System = policy.System;
        Description = policy.Description;
        Created = policy.Created;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaPolicy);
    }
}
