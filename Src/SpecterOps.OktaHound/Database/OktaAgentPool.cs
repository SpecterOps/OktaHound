using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaAgentPool : OktaEntity
{
    public const string NodeKind = "Okta_AgentPool";
    public const string AgentPoolForEdgeKind = "Okta_AgentPoolFor";
    public const string AgentlessDesktopSSOEdgeKind = "Okta_KerberosSSO";

    public string? Type { get; set; }
    public string? OperationalStatus { get; set; }

    [JsonIgnore]
    public List<OktaAgent> Agents { get; set; } = [];

    /// <summary>
    /// The original identifier of the agent pool as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public string OriginalId { get; private set; } = string.Empty;

    [JsonIgnore]
    public bool IsActiveDirectoryAgentPool { get; set; }

    [JsonIgnore]
    [NotMapped]
    public string? ActiveDirectoryDomain => IsActiveDirectoryAgentPool ? Name : null;

    protected override string[] Kinds => [NodeKind];

    private OktaAgentPool() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaAgentPool(AgentPool agentPool, string domainName) : base(MakeAgentPoolUnique(agentPool.Id), agentPool.Name, domainName)
    {
        DisplayName = agentPool.Name;
        OriginalId = agentPool.Id;
        OperationalStatus = agentPool.OperationalStatus?.Value;
        Type = agentPool.Type?.Value;
        IsActiveDirectoryAgentPool = Type == AgentType.AD;
    }

    internal static string MakeAgentPoolUnique(string agentPoolId)
    {
        // AD agent pools share their IDs with AD applications.
        return agentPoolId + "_pool";
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaAgentPool);
    }
}
