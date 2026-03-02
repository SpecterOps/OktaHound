using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaAgent : OktaEntity
{
    public const string HostsAgentEdgeKind = "Okta_HostsAgent";
    public const string HasAgentMemberOfEdgeKind = "Okta_AgentMemberOf";
    public const string NodeKind = "Okta_Agent";

    public string? OperationalStatus { get; set; }
    public string? UpdateStatus { get; set; }
    public string? Type { get; set; }
    public string? Version { get; set; }
    public DateTimeOffset? LastConnection { get; set; }

    [JsonIgnore]
    public string? AgentPoolId { get; set; }

    [JsonPropertyName("poolId")]
    [NotMapped]
    public string? OriginalAgentPoolId => AgentPool?.OriginalId;

    [JsonPropertyName("poolName")]
    [NotMapped]
    public string? PoolName => AgentPool?.Name;

    [JsonIgnore]
    public OktaAgentPool? AgentPool { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaAgent() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaAgent(Agent agent, string domainName) : base(agent.Id, agent.Name, domainName)
    {
        DisplayName = agent.Name;
        OperationalStatus = agent.OperationalStatus?.Value;
        UpdateStatus = agent.UpdateStatus?.Value;
        Type = agent.Type?.Value;
        Version = agent._Version;

        if (!string.IsNullOrWhiteSpace(agent.PoolId))
        {
            AgentPoolId = OktaAgentPool.MakeAgentPoolUnique(agent.PoolId);
        }

        // Convert lastConnection from Unix timestamp
        LastConnection = DateTimeOffset.FromUnixTimeMilliseconds(agent.LastConnection);
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaAgent);
    }
}
