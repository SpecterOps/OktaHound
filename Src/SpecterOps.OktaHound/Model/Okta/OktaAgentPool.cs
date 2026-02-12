using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaAgentPool : OktaNode
{
    public const string NodeKind = "Okta_AgentPool";
    public const string HasAgentEdgeKind = "Okta_HasAgent";

    [JsonIgnore]
    private string _originalId;

    /// <summary>
    /// The original ID of the agent pool, as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public override string OriginalId => _originalId;

    public OktaAgentPool(AgentPool agentPool, string domainName) : base(MakeAgentPoolUnique(agentPool.Id), domainName, NodeKind)
    {
        Name = agentPool.Name;
        DisplayName = agentPool.Name;
        _originalId = agentPool.Id;

        SetProperty("operationalStatus", agentPool.OperationalStatus?.Value);
        SetProperty("type", agentPool.Type?.Value);
    }

    private static string MakeAgentPoolUnique(string agentPoolId)
    {
        // AD agent pools share their IDs with AD applications.
        return agentPoolId + "_pool";
    }
}
