using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaClientSecret : OktaNode
{
    public const string SecretOfEdgeKind = "Okta_SecretOf";
    private const string NodeKind = "Okta_ClientSecret";

    public OktaClientSecret(APIServiceIntegrationInstanceSecret secret, string domainName) : base(secret.Id, domainName, NodeKind)
    {
        Name = secret.SecretHash;
        DisplayName = secret.SecretHash;

        SetProperty("status", secret.Status?.Value);
        SetProperty("created", secret.Created);
        SetProperty("lastUpdated", secret.LastUpdated);

        // Only the last couple of characters are revealed for security reasons
        SetProperty("secret", secret.ClientSecret);
    }

    public OktaClientSecret(OAuth2ClientSecret secret, string domainName) : base(secret.Id, domainName, NodeKind)
    {
        Name = secret.SecretHash;
        DisplayName = secret.SecretHash;

        SetProperty("status", secret.Status?.Value);
        SetProperty("created", secret.Created);
        SetProperty("lastUpdated", secret.LastUpdated);

        // Only the last couple of characters are revealed for security reasons
        SetProperty("secret", secret.ClientSecret);
    }
}
