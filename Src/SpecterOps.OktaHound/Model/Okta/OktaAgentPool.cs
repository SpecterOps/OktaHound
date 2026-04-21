using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaAgentPool : OktaNode
{
    public const string NodeKind = "Okta_AgentPool";
    public const string AgentPoolForEdgeKind = "Okta_AgentPoolFor";
    public const string AgentlessDesktopSSOEdgeKind = "Okta_KerberosSSO";
    private const string TypePropertyName = "type";
    private const string OperationalStatusPropertyName = "operationalStatus";

    [JsonIgnore]
    private string _originalId;

    /// <summary>
    /// The original ID of the agent pool, as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public override string OriginalId => _originalId;

    [JsonIgnore]
    public string? Type => GetProperty<string>(TypePropertyName);

    [JsonIgnore]
    public bool IsActiveDirectoryAgentPool => Type == AgentType.AD;

    [JsonIgnore]
    public string? ActiveDirectoryDomain => IsActiveDirectoryAgentPool ? Name : null;

    public OktaAgentPool(AgentPool agentPool, OktaOrganization organization) : base(MakeAgentPoolUnique(agentPool.Id), organization, NodeKind)
    {
        Name = agentPool.Name;
        DisplayName = agentPool.Name;
        _originalId = agentPool.Id;

        SetProperty(OperationalStatusPropertyName, agentPool.OperationalStatus?.Value);
        SetProperty(TypePropertyName, agentPool.Type?.Value);
    }

    private static string MakeAgentPoolUnique(string agentPoolId)
    {
        // AD agent pools share their IDs with AD applications.
        return agentPoolId + "_pool";
    }
}
