using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

/// <summary>
/// Represents an OAuth 2.0 client JSON Web Key (JWK) used for private_key_jwt authentication.
/// </summary>
internal sealed class OktaJWK : OktaNode
{
    public const string KeyOfEdgeKind = "Okta_KeyOf";
    private const string NodeKind = "Okta_JWK";

    public OktaJWK(OAuth2ClientJsonWebKeyECResponse jwk, string domainName) : base(jwk.Id, domainName, NodeKind)
    {
        Name = jwk.Kid ?? jwk.Id;
        DisplayName = jwk.Kid ?? jwk.Id;

        SetProperty("status", jwk.Status?.Value);
        SetProperty("created", jwk.Created);
        SetProperty("lastUpdated", jwk.LastUpdated);
        SetProperty("kid", jwk.Kid);
        SetProperty("kty", jwk.Kty?.Value);
        SetProperty("use", jwk.Use);
    }

    public OktaJWK(OAuth2ClientJsonWebKeyRsaResponse jwk, string domainName) : base(jwk.Id, domainName, NodeKind)
    {
        Name = jwk.Kid ?? jwk.Id;
        DisplayName = jwk.Kid ?? jwk.Id;

        SetProperty("status", jwk.Status?.Value);
        SetProperty("created", jwk.Created);
        SetProperty("lastUpdated", jwk.LastUpdated);
        SetProperty("kid", jwk.Kid);
        SetProperty("kty", jwk.Kty?.Value);
        SetProperty("use", jwk.Use);
    }
}
