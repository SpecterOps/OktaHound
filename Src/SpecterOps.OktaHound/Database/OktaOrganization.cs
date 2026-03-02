using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaOrganization : OpenGraphEntity
{
    public const string ContainsEdgeKind = "Okta_Contains";
    public const string NodeKind = "Okta_Organization";

    [JsonPropertyName("environment_name")]
    public string DomainName { get; set; }

    public string? Subdomain { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }

    protected override string[] Kinds => [NodeKind];

    [JsonIgnore]
    public bool AgentlessDssoEnabled { get; set; }

    private OktaOrganization() : base(string.Empty, string.Empty)
    {
        DomainName = string.Empty;
    }

    public OktaOrganization(OrgSetting settings, string domainName) : base(settings.Id, domainName)
    {
        DisplayName = settings.CompanyName;
        DomainName = domainName;
        Subdomain = settings.Subdomain;
        Status = settings.Status?.Value;
        Created = settings.Created;
        LastUpdated = settings.LastUpdated;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaOrganization);
    }
}
