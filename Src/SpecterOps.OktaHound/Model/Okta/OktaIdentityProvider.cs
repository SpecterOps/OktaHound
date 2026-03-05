using System;
using System.Diagnostics.CodeAnalysis;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.Entra;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaIdentityProvider : OktaNode
{
    public const string NodeKind = "Okta_IdentityProvider";
    public const string IdpForEdgeKind = "Okta_IdentityProviderFor";
    public const string InboundOrgSSOEdgeKind = "Okta_InboundOrgSSO";
    public const string AutoGroupAssignmentEdgeKind = "Okta_IdpGroupAssignment";
    public const string InboundSsoEdgeKind = "Okta_InboundSSO";
    public const string ExternalIdPropertyName = "externalId";
    private const string TenantIdPropertyName = "entraTenantId";
    private const string AutomaticUserProvisioningPropertyName = "autoUserProvisioning";
    private const string EnabledPropertyName = "enabled";
    private const string UrlPropertyName = "url";
    private const string GovernedGroupsPropertyName = "governedGroupIds";
    private const string ProtocolTypePropertyName = "protocolType";

    /// <summary>
    /// Gets the Microsoft Entra Tenant ID associated with this identity provider, if applicable.
    /// </summary>
    public string? TenantId => GetProperty<string>(TenantIdPropertyName);

    /// <summary>
    /// Gets a value indicating whether this identity provider is configured for automatic user provisioning.
    /// </summary>
    public bool AutomaticUserProvisioning => GetPropertyAsBool(AutomaticUserProvisioningPropertyName) ?? false;

    /// <summary>
    /// Gets a value indicating whether this identity provider is enabled (active).
    /// </summary>
    public bool IsEnabled => GetPropertyAsBool(EnabledPropertyName) ?? true;

    /// <summary>
    /// Gets the list of group IDs whose membership is governed by this identity provider.
    /// </summary>
    public string[] GovernedGroupIds => GetProperty<string[]>(GovernedGroupsPropertyName) ?? [];

    public OktaIdentityProvider(IdentityProvider idp, string domainName) : base(idp.Id, domainName, NodeKind)
    {
        Name = idp.Name;
        DisplayName = idp.Name;

        SetProperty("created", idp.Created);
        SetProperty("issuerMode", idp.IssuerMode?.Value);
        SetProperty("type", idp.Type?.Value);
        SetProperty(EnabledPropertyName, idp.Status?.Value == LifecycleStatus.ACTIVE);
        SetProperty(AutomaticUserProvisioningPropertyName, idp.Policy?.Provisioning?.Action == ProvisioningAction.AUTO);

        // Governed Groups
        // Hardcoded group assignemnts
        string[] assignedGroupIds = idp.Policy?.Provisioning?.Groups?.Assignments?.ToArray() ?? [];
        // Group assignments sourced from SAML claims
        string[] dynamicGroupIds = idp.Policy?.Provisioning?.Groups?.Filter?.ToArray() ?? [];
        // Combine both assignment types. The UI only allows one or the other.
        string[] governedGroupIds = [.. assignedGroupIds, .. dynamicGroupIds];
        SetProperty(GovernedGroupsPropertyName, governedGroupIds);

        object protocol = idp.Protocol.ActualInstance;

        if (protocol is ProtocolSaml samlProtocol)
        {
            SetProperty(ProtocolTypePropertyName, samlProtocol.Type?.Value);

            // SAML ACS URL
            string? samlEndpoint = samlProtocol.Endpoints?.Sso?.Url;
            SetProperty(UrlPropertyName, samlEndpoint);

            // Handle specific identity providers
            // Only Microsoft Entra ID is supported for now
            string? tenantId = EntraIdTenant.ParseTenantIdFromUrl(samlEndpoint);
            SetProperty(TenantIdPropertyName, tenantId);
        }
        else if (protocol is ProtocolOidc oidcProtocol)
        {
            SetProperty(ProtocolTypePropertyName, oidcProtocol.Type?.Value);

            // OpenID Connect URL
            SetProperty(UrlPropertyName, oidcProtocol.Endpoints?.Authorization?.Url);

            // TODO: Tenant ID is currently not available for Microsoft Entra ID OIDC IdPs, only client ID is available.
        }
        else if (protocol is ProtocolOAuth oauthProtocol)
        {
            SetProperty(ProtocolTypePropertyName, oauthProtocol.Type?.Value);

            // OAuth 2.0 URL
            SetProperty(UrlPropertyName, oauthProtocol.Endpoints?.Authorization?.Url);
        }
        else if (protocol is ProtocolMtls mtlsProtocol)
        {
            SetProperty(ProtocolTypePropertyName, mtlsProtocol.Type?.Value);

            // Mutual TLS URL
            SetProperty(UrlPropertyName, mtlsProtocol.Endpoints?.Sso?.Url);
        }
        else if (protocol is ProtocolIdVerification idvProtocol)
        {
            SetProperty(ProtocolTypePropertyName, idvProtocol.Type?.Value);

            // Identity Verification URL
            SetProperty(UrlPropertyName, idvProtocol.Endpoints?.Authorization?.Url);
        }
    }

    [return: NotNullIfNotNull(nameof(id))]
    public static new OpenGraphEdgeNode? CreateEdgeNode(string? id) => id is not null ? new(id, NodeKind) : null;
}
