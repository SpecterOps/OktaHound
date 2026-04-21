using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaAgent : OktaNode
{
    public const string HostsAgentEdgeKind = "Okta_HostsAgent";
    public const string HasAgentMemberOfEdgeKind = "Okta_AgentMemberOf";

    private const string NodeKind = "Okta_Agent";

    public OktaAgent(Agent agent, string agentPoolName, OktaOrganization organization) : base(agent.Id, organization, NodeKind)
    {
        Name = agent.Name;
        DisplayName = agent.Name;

        // Agent pool name corresponds to the Active Directory Domain
        SetProperty("poolName", agentPoolName);

        SetProperty("operationalStatus", agent.OperationalStatus?.Value);
        SetProperty("updateStatus", agent.UpdateStatus?.Value);
        SetProperty("type", agent.Type?.Value);
        SetProperty("version", agent._Version);
        SetProperty("poolId", agent.PoolId);

        // Convert lastConnection from Unix timestamp
        var lastConnection = DateTimeOffset.FromUnixTimeMilliseconds(agent.LastConnection);
        SetProperty("lastConnection", lastConnection);
    }
}
