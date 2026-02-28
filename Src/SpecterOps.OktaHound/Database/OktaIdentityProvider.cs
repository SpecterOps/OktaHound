using System.Text.Json.Serialization;
using System.Text.Json;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.Entra;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaIdentityProvider : OktaEntity
{
    public const string NodeKind = "Okta_IdentityProvider";
    public const string IdpForEdgeKind = "Okta_IdentityProviderFor";
    public const string InboundOrgSSOEdgeKind = "Okta_InboundOrgSSO";
    public const string AutoGroupAssignmentEdgeKind = "Okta_IdpGroupAssignment";
    public const string InboundSsoEdgeKind = "Okta_InboundSSO";

    public DateTimeOffset? Created { get; set; }
    public string? IssuerMode { get; set; }
    public string? Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ProtocolType { get; set; }
    public string? Url { get; set; }

    [JsonPropertyName("autoUserProvisioning")]
    public bool AutomaticUserProvisioning { get; set; }

    [JsonPropertyName("entraTenantId")]
    public string? TenantId { get; set; }

    [JsonIgnore]
    public List<OktaGroup>? GovernedGroups { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaIdentityProvider() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaIdentityProvider(IdentityProvider idp, string domainName) : base(idp.Id, idp.Name, domainName)
    {
        DisplayName = idp.Name;
        Created = idp.Created;
        IssuerMode = idp.IssuerMode?.Value;
        Type = idp.Type?.Value;
        IsEnabled = idp.Status?.Value == LifecycleStatus.ACTIVE;
        AutomaticUserProvisioning = idp.Policy?.Provisioning?.Action == ProvisioningAction.AUTO;

        object protocol = idp.Protocol.ActualInstance;

        if (protocol is ProtocolSaml samlProtocol)
        {
            ProtocolType = samlProtocol.Type?.Value;

            // SAML ACS URL
            string? samlEndpoint = samlProtocol.Endpoints?.Sso?.Url;
            Url = samlEndpoint;

            // Handle specific identity providers
            // Only Microsoft Entra ID is supported for now
            TenantId = EntraIdTenant.ParseTenantIdFromUrl(samlEndpoint);
        }
        else if (protocol is ProtocolOidc oidcProtocol)
        {
            ProtocolType = oidcProtocol.Type?.Value;

            // OpenID Connect URL
            Url = oidcProtocol.Endpoints?.Authorization?.Url;

            // TODO: Tenant ID is currently not available for Microsoft Entra ID OIDC IdPs, only client ID is available.
        }
        else if (protocol is ProtocolOAuth oauthProtocol)
        {
            ProtocolType = oauthProtocol.Type?.Value;

            // OAuth 2.0 URL
            Url = oauthProtocol.Endpoints?.Authorization?.Url;
        }
        else if (protocol is ProtocolMtls mtlsProtocol)
        {
            ProtocolType = mtlsProtocol.Type?.Value;

            // Mutual TLS URL
            Url = mtlsProtocol.Endpoints?.Sso?.Url;
        }
        else if (protocol is ProtocolIdVerification idvProtocol)
        {
            ProtocolType = idvProtocol.Type?.Value;

            // Identity Verification URL
            Url = idvProtocol.Endpoints?.Authorization?.Url;
        }
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaIdentityProvider);
    }

    // public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
