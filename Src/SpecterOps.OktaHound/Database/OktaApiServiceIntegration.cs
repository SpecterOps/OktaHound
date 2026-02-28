using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApiServiceIntegration : OktaEntity
{
    public const string CreatorOfEdgeKind = "Okta_CreatorOf";
    public const string NodeKind = "Okta_ApiServiceIntegration";

    public string? IntegrationType { get; set; }
    public List<string>? Permissions { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonIgnore]
    public string? CreatedById { get; set; }

    [JsonIgnore]
    public OktaUser? CreatedBy { get; set; }

    [JsonIgnore]
    public List<OktaClientSecret> ClientSecrets { get; set; } = [];

    protected override string[] Kinds => [NodeKind];

    private OktaApiServiceIntegration() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaApiServiceIntegration(APIServiceIntegrationInstance service, string domainName) : base(service.Id, service.Name, domainName)
    {
        DisplayName = service.Name;
        Permissions = service.GrantedScopes;
        IntegrationType = service.Type;
        CreatedById = service.CreatedBy;

        if (service.CreatedAt is not null)
        {
            CreatedAt = DateTimeOffset.Parse(service.CreatedAt, CultureInfo.InvariantCulture);
        }
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaApiServiceIntegration);
    }
}
