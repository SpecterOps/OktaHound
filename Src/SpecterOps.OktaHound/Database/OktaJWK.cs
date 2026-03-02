using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

/// <summary>
/// Represents an OAuth 2.0 client JSON Web Key (JWK) used for private_key_jwt authentication.
/// </summary>
public sealed class OktaJWK : OktaEntity
{
    public const string KeyOfEdgeKind = "Okta_KeyOf";
    public const string NodeKind = "Okta_JWK";

    public string? Status { get; set; }
    public string? Created { get; set; }
    public string? LastUpdated { get; set; }

    [JsonPropertyName("kid")]
    public string? KeyId { get; set; }

    [JsonPropertyName("kty")]
    public string? KeyType { get; set; }

    [JsonPropertyName("use")]
    public string? KeyUsage { get; set; }

    [JsonIgnore]
    public string? ApplicationId { get; set; }

    [JsonIgnore]
    public OktaApplication? Application { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaJWK() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaJWK(OAuth2ClientJsonWebKeyECResponse jwk, string appId, string domainName) : base(jwk.Id, jwk.Kid ?? jwk.Id, domainName)
    {
        DisplayName = jwk.Kid ?? jwk.Id;
        Status = jwk.Status?.Value;
        Created = jwk.Created;
        LastUpdated = jwk.LastUpdated;
        KeyId = jwk.Kid;
        KeyType = jwk.Kty?.Value;
        KeyUsage = jwk.Use;
        ApplicationId = appId;
    }

    public OktaJWK(OAuth2ClientJsonWebKeyRsaResponse jwk, string appId, string domainName) : base(jwk.Id, jwk.Kid ?? jwk.Id, domainName)
    {
        DisplayName = jwk.Kid ?? jwk.Id;
        Status = jwk.Status?.Value;
        Created = jwk.Created;
        LastUpdated = jwk.LastUpdated;
        KeyId = jwk.Kid;
        KeyType = jwk.Kty?.Value;
        KeyUsage = jwk.Use;
        ApplicationId = appId;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaJWK);
    }
}
