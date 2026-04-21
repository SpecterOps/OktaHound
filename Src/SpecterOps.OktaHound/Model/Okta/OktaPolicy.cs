using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaPolicy : OktaNode
{
    public const string PolicyMappingEdgeKind = "Okta_PolicyMapping";
    private const string NodeKind = "Okta_Policy";
    private const string PolicyTypePropertyName = "type";

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

    [JsonIgnore]
    public string? PolicyType => GetProperty<string>(PolicyTypePropertyName);

    public OktaPolicy(Policy policy, OktaOrganization organization) : base(policy.Id, organization, NodeKind)
    {
        Name = policy.Name;
        DisplayName = policy.Name;

        SetProperty(PolicyTypePropertyName, policy.Type?.Value);
        SetProperty("priority", policy.Priority);
        SetProperty("system", policy.System);
        SetProperty("description", policy.Description);
        SetProperty("created", policy.Created);
    }
}
