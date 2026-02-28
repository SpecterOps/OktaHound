using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaClientSecret : OktaEntity
{
    public const string SecretOfEdgeKind = "Okta_SecretOf";
    public const string NodeKind = "Okta_ClientSecret";

    public string? Status { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonIgnore]
    public string? ApplicationId { get; set; }

    [JsonIgnore]
    public string? ApiServiceIntegrationId { get; set; }

    [JsonIgnore]
    public OktaApplication? Application { get; set; }

    [JsonIgnore]
    public OktaApiServiceIntegration? ApiServiceIntegration { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaClientSecret() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaClientSecret(APIServiceIntegrationInstanceSecret secret, string apiServiceIntegrationId, string domainName) : base(secret.Id, secret.SecretHash, domainName)
    {
        DisplayName = secret.SecretHash;
        Status = secret.Status?.Value;
        ApiServiceIntegrationId = apiServiceIntegrationId;

        if (secret.Created is not null)
        {
            Created = DateTimeOffset.Parse(secret.Created, CultureInfo.InvariantCulture);
        }

        if (secret.LastUpdated is not null)
        {
            LastUpdated = DateTimeOffset.Parse(secret.LastUpdated, CultureInfo.InvariantCulture);
        }

        // Although for API Service Integrations the returned secret values are partially redacted (only the last 4 characters are visible),
        // we will still avoid collecting the actual secret value for security reasons.
    }

    public OktaClientSecret(OAuth2ClientSecret secret, string applicationId, string domainName) : base(secret.Id, secret.SecretHash, domainName)
    {
        DisplayName = secret.SecretHash;
        Status = secret.Status?.Value;
        ApplicationId = applicationId;

        if (secret.Created is not null)
        {
            Created = DateTimeOffset.Parse(secret.Created, CultureInfo.InvariantCulture);
        }

        if (secret.LastUpdated is not null)
        {
            LastUpdated = DateTimeOffset.Parse(secret.LastUpdated, CultureInfo.InvariantCulture);
        }

        // DO NOT COLLECT THE ACTUAL SECRET VALUE FOR SECURITY REASONS
        // For Service Applications the returned secret value is the full plaintext value!
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaClientSecret);
    }
}
