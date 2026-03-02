using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApiToken : OktaEntity
{
    public const string NodeKind = "Okta_ApiToken";
    public const string ApiTokenForEdgeKind = "Okta_ApiTokenFor";

    public string? UserId { get; set; }
    public string? ClientName { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? NetworkConnection { get; set; }
    public TimeSpan? TokenWindow { get; set; }

    [JsonIgnore]
    public OktaUser? User { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaApiToken() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaApiToken(ApiToken apiToken, string domainName) : base(apiToken.Id, apiToken.Name, domainName)
    {
        DisplayName = apiToken.Name;

        UserId = apiToken.UserId;
        ClientName = apiToken.ClientName;
        Created = apiToken.Created;
        LastUpdated = apiToken.LastUpdated;
        ExpiresAt = apiToken.ExpiresAt;
        NetworkConnection = apiToken.Network.Connection;

        if (!string.IsNullOrEmpty(apiToken.TokenWindow))
        {
            // Parse ISO-8601 period, e.g., "P30D".
            TokenWindow = XmlConvert.ToTimeSpan(apiToken.TokenWindow);
        }
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaApiToken);
    }
}
