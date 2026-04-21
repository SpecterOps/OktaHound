using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaClientSecret : OktaNode
{
    public const string SecretOfEdgeKind = "Okta_SecretOf";
    private const string NodeKind = "Okta_ClientSecret";

    public OktaClientSecret(APIServiceIntegrationInstanceSecret secret, OktaOrganization organization) : base(secret.Id, organization, NodeKind)
    {
        Name = secret.SecretHash;
        DisplayName = secret.SecretHash;

        SetProperty("status", secret.Status?.Value);
        SetProperty("created", secret.Created);
        SetProperty("lastUpdated", secret.LastUpdated);
        // Although for API Service Integrations the returned secret values are partially redacted (only the last 4 characters are visible),
        // we will still avoid collecting the actual secret value for security reasons.
    }

    public OktaClientSecret(OAuth2ClientSecret secret, OktaOrganization organization) : base(secret.Id, organization, NodeKind)
    {
        Name = secret.SecretHash;
        DisplayName = secret.SecretHash;

        SetProperty("status", secret.Status?.Value);
        SetProperty("created", secret.Created);
        SetProperty("lastUpdated", secret.LastUpdated);
        // DO NOT COLLECT THE ACTUAL SECRET VALUE FOR SECURITY REASONS
        // For Service Applications the returned secret value is the full plaintext value!
    }
}
