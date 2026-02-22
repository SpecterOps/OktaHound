using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.Entra;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaApplication : OktaSecurityPrincipal
{
    public const string NodeKind = "Okta_Application";
    public const string AppAssignmentEdgeKind = "Okta_AppAssignment";
    public const string ReadPasswordUpdatesEdgeKind = "Okta_ReadPasswordUpdates";
    public const string OrganizationalSingleSignOnEdgeKind = "Okta_OutboundOrgSSO";
    public const string OrganizationalSecureWebAuthenticationEdgeKind = "Okta_OrgSWA";
    private const string FeaturesPropertyName = "features";
    private const string ApplicationTypePropertyName = "appType";
    private const string ClientTypePropertyName = "clientType";
    private const string SignOnModePropertyName = "signOnMode";
    private const string PermissionsPropertyName = "oauthScopes";
    private const string UserNameMappingPropertyName = "userNameMapping";
    private const string UrlPropertyName = "url";

    // Built-in apps

    /// <summary>
    /// List of built-in Okta applications with SCIM user sync.
    /// </summary>
    /// <remarks>
    /// We do not want the UI to be cluttered by their sync edges.
    /// </remarks>
    private static readonly string[] IgnoredOutboundSyncApps = [
        "okta_flow_sso", // Okta Workflows
        "okta_atspoke_sso" // Okta Access Requests
    ];

    /// <summary>
    /// List of built-in applications that are assigned to all users by default.
    /// </summary>
    /// <remarks>
    /// We do not want the UI to be cluttered by their app assignment edges.
    /// </remarks>
    private static readonly string[] IgnoredAssignedApps = [
        "okta_oin_submission_tester_app", // Okta OIN Submission Tester
        "okta_access_requests_resource_catalog", // Okta Identity Governance
        "okta_enduser", // Okta Dashboard
        "okta_browser_plugin", // Okta Browser Plugin
        "active_directory", // Active Directory, for which there are sync edges
        "ldap_interface" // LDAP Interface, similar to AD
    ];

    // Well-known application types

    /// <summary>
    /// Synchronized Active Directory domain
    /// </summary>
    private const string ActiveDirectoryApplicationType = "active_directory";

    /// <summary>
    /// Microsoft Entra ID (formerly Azure AD) External Authentication
    /// </summary>
    private const string EntraIdApplicationType = "microsoft_external_authentication_method";

    /// <summary>
    /// Snowflake Application
    /// </summary>
    private const string SnowflakeApplicationType = "snowflake";

    /// <summary>
    /// GitHub Cloud Application
    /// </summary>
    private const string GitHubCloudApplicationType = "githubcloud";

    /// <summary>
    /// Microsoft Office 365 Application (Outbound OIDC authentication to Entra ID)
    /// </summary>
    private const string Office365ApplicationType = "office365";

    /// <summary>
    /// Amazon AWS IAM Identity Center
    /// </summary>
    private const string AmazonSsoApplicationType = "amazon_aws_sso";

    /// <summary>
    /// Amazon AWS Account Federation
    /// </summary>
    private const string AmazonAccountFederationApplicationType = "amazon_aws";

    /// <summary>
    /// 1Password Business Application
    /// </summary>
    private const string OnePasswordBusinessApplicationType = "1password_business";

    /// <summary>
    /// Jamf Pro SAML Application
    /// </summary>
    private const string JamfProSamlApplicationType = "jamfsoftwareserver";

    /// <summary>
    /// Jamf Pro Application (SWA)
    /// </summary>
    private const string JamfProSwaApplicationType = "casper";

    /// <summary>
    /// Google Workspace Application
    /// </summary>
    private const string GoogleWorkspaceApplicationType = "google";

    /// <summary>
    /// LDAP Interface Directory Integration
    /// </summary>
    private const string LdapInterfaceApplicationType = "ldap_interface";

    // App-specific properties
    private const string GitHubOrganizationPropertyName = "githubOrg";
    private const string ActiveDirectoryDomainPropertyName = "namingContext";
    private const string ActiveDirectoryDomainSidPropertyName = "domainSid";
    private const string EntraIdDiscoveryEndpointPropertyName = "microsoftDiscoveryEndpoint";

    /// <summary>
    /// Name of the primary Entra ID domain used by the Office 365 application.
    /// </summary>
    /// <remarks>
    /// If the value is "contoso", then it represents the "contoso.onmicrosoft.com" tenant.
    /// </remarks>
    private const string EntraOnMicrosoftDomainPropertyName = "msftTenant";
    private const string EntraTenantIdPropertyName = "microsoftTenantId";
    private const string EntraClientIdPropertyName = "microsoftAppId";
    private const string JamfDomainPropertyName = "domain";
    private const string SnowflakeSubdomainPropertyName = "subDomain";
    private const string OnePasswordRegionPropertyName = "regionType";
    private const string OnePasswordSubdomainPropertyName = "subDomain";

    [JsonIgnore]
    public string? ApplicationType => GetProperty<string>(ApplicationTypePropertyName);

    [JsonIgnore]
    public string? ClientType => GetProperty<string>(ClientTypePropertyName);

    [JsonIgnore]
    public string? SignOnMode => GetProperty<string>(SignOnModePropertyName);

    [JsonIgnore]
    private IEnumerable<string> Features =>
        GetProperty<string[]>(FeaturesPropertyName) ?? [];

    [JsonIgnore]
    public bool SupportsSCIM => Features.Contains("SCIM_PROVISIONING");

    [JsonIgnore]
    public bool SupportsPasswordUpdates => Features.Contains("PUSH_PASSWORD_UPDATES");

    [JsonIgnore]
    public bool IsService => ClientType == OpenIdConnectApplicationType.Service.Value;

    [JsonIgnore]
    public bool IsActiveDirectory => ApplicationType == ActiveDirectoryApplicationType;

    [JsonIgnore]
    public bool IsLdapInterface => ApplicationType == LdapInterfaceApplicationType;

    [JsonIgnore]
    public bool IsEntraId => ApplicationType == EntraIdApplicationType;

    [JsonIgnore]
    public bool IsSAMLApplication =>
        SignOnMode == ApplicationSignOnMode.SAML20.Value ||
        SignOnMode == ApplicationSignOnMode.SAML11.Value ||
        SignOnMode == ApplicationSignOnMode.WSFEDERATION;

    [JsonIgnore]
    public bool IsOIDCApplication => SignOnMode == ApplicationSignOnMode.OPENIDCONNECT.Value;

    [JsonIgnore]
    public bool IsSWAApplication =>
        SignOnMode == ApplicationSignOnMode.AUTOLOGIN.Value ||
        SignOnMode == ApplicationSignOnMode.BASICAUTH.Value ||
        SignOnMode == ApplicationSignOnMode.BROWSERPLUGIN.Value;

    [JsonIgnore]
    public bool IsBookmarkApplication => SignOnMode == ApplicationSignOnMode.BOOKMARK.Value;

    /// <summary>
    /// Indicates whether outbound user SCIM sync edges for this application should be ignored.
    /// </summary>
    [JsonIgnore]
    public bool IsOutboundSyncIgnored => IgnoredOutboundSyncApps.Contains(ApplicationType ?? string.Empty);

    /// <summary>
    /// Indicates whether user app assignment edges for this application should be ignored.
    /// </summary>
    [JsonIgnore]
    public bool IsAssignmentIgnored => IgnoredAssignedApps.Contains(ApplicationType ?? string.Empty);

    [JsonIgnore]
    public List<string>? Permissions
    {
        get => GetProperty<List<string>>(PermissionsPropertyName);
        set => SetProperty(PermissionsPropertyName, value);
    }

    [JsonIgnore]
    public string? UserNameMapping => GetProperty<string>(UserNameMappingPropertyName);

    [JsonIgnore]
    public string? GitHubOrganization => GetProperty<string>(GitHubOrganizationPropertyName);

    [JsonIgnore]
    public string? JamfDomain => GetProperty<string>(JamfDomainPropertyName);

    [JsonIgnore]
    public string? SnowflakeSubdomain => GetProperty<string>(SnowflakeSubdomainPropertyName);

    [JsonIgnore]
    public string? OnePasswordDomain
    {
        get
        {
            string? regionType = GetProperty<string>(OnePasswordRegionPropertyName);
            string? subDomain = GetProperty<string>(OnePasswordSubdomainPropertyName);

            if (regionType != null && subDomain != null)
            {
                // Example: contoso.1Password.com
                return $"{subDomain}.1Password.{regionType}";
            }
            else
            {
                return null;
            }

        }
    }

    /// <summary>
    /// FQDN of the domain this app represents.
    /// </summary>
    [JsonIgnore]
    public string? ActiveDirectoryDomain => GetProperty<string>(ActiveDirectoryDomainPropertyName);

    /// <summary>
    /// Security identifier (SID) of the AD domain this app represents.
    /// </summary>
    /// <remarks>
    /// Domain SID is not stored in app objects, but we can derive it from the SIDs of the associated users or groups.
    /// </remarks>
    [JsonIgnore]
    public string? ActiveDirectoryDomainSid
    {
        get => GetProperty<string>(ActiveDirectoryDomainSidPropertyName);
        set => SetProperty(ActiveDirectoryDomainSidPropertyName, value);
    }

    [JsonIgnore]
    public string? EntraTenantId => GetProperty<string>(EntraTenantIdPropertyName);

    [JsonIgnore]
    public string? EntraClientId => GetProperty<string>(EntraClientIdPropertyName);

    [JsonIgnore]
    public string? EntraRegion => GetProperty<string>(EntraIdDiscoveryEndpointPropertyName) switch
    {
        // TODO: Consider using an Enum for the Entra region
        "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration" => "Global",
        "https://login.microsoftonline.us/common/v2.0/.well-known/openid-configuration" => "USGovernment",
        "https://login.partner.microsoftonline.cn/common/v2.0/.well-known/openid-configuration" => "China",
        _ => null,
    };

    [JsonIgnore]
    public string? Url => GetProperty<string>(UrlPropertyName);

    /// <summary>
    /// Gets the type of edge connecting a user in this organization with another account in a different tenant.
    /// scenario.
    /// </summary>
    [JsonIgnore]
    public string? HybridUserSignOnEdgeType
    {
        get
        {
            if (IsSAMLApplication || IsOIDCApplication)
            {
                // SAML and OIDC trusts are traversable for users.
                return OktaUser.SingleSignOnEdgeKind;
            }
            else if (IsSWAApplication)
            {
                // SWA apps may have passwords cached, but not for all users.
                return OktaUser.SecureWebAuthenticationEdgeKind;
            }
            else
            {
                // Do not create user->user edges for Bookmarks and other app types.
                return null;
            }
        }
    }

    /// <summary>
    /// Gets the edge kind for outbound trusts from this Okta organization to another organization's tenant.
    /// </summary>
    [JsonIgnore]
    public string? HybridApplicationEdgeKind
    {
        get
        {
            if (IsActiveDirectory || IsLdapInterface || IsService)
            {
                // AD and LDAP edges are inbound and handled separately.
                // Services do not have outbound trusts, even if they use OIDC to authenticate.
                return null;
            }
            else if (IsSAMLApplication || IsOIDCApplication)
            {
                // SAML and OIDC trusts are traversable between organizations.
                return OrganizationalSingleSignOnEdgeKind;
            }
            else if (IsSWAApplication)
            {
                // SWA trusts are generally not traversable, but we want to have them in the graph.
                return OrganizationalSecureWebAuthenticationEdgeKind;
            }
            else
            {
                // Any other trust types are not worth adding.
                return null;
            }
        }
    }

    public OktaApplication(Application application, string domainName) : base(application.Id, domainName, NodeKind)
    {
        Name = application.Label;
        DisplayName = application.Label;

        SetProperty("created", application.Created);
        SetProperty("lastUpdated", application.LastUpdated);
        SetProperty("status", application.Status?.Value);
        SetProperty(FeaturesPropertyName, application.Features?.Select(feature => feature?.Value ?? string.Empty).ToArray());
        SetProperty(SignOnModePropertyName, application.SignOnMode?.Value);

        if (application is OpenIdConnectApplication oidcApp)
        {
            // OIDC Application
            // Sample Name values:
            // saasure: Okta Admin Console
            // oidc_client: Generic OIDC app
            // aws: Amazon Web Services
            // office365: Entra ID
            SetProperty(ApplicationTypePropertyName, oidcApp.Name?.Value);

            // Sample client types: web, browser, native, service
            SetProperty(ClientTypePropertyName, oidcApp.Settings?.OauthClient?.ApplicationType?.Value);

            // Sample grant types: authorization_code, implicit, refresh_token, client_credentials
            var grants = oidcApp.Settings?.OauthClient?.GrantTypes?.Select(grant => grant?.Value ?? string.Empty).ToArray();
            SetProperty("grantTypes", grants);

            // Sample template: ${source.login}
            SetProperty(UserNameMappingPropertyName, oidcApp.Credentials?.UserNameTemplate?.Template);

            // Sign-in redirect URI for standard OIDC web applications
            // For now, we will just take the first redirect URI if multiple are defined
            SetProperty(UrlPropertyName, oidcApp.Settings?.OauthClient?.RedirectUris?.FirstOrDefault());

            // The default primary redirect URI for OIDC SPA apps is http://localhost:8080/login/callback
            // We will collect the optional Initiate login URI instead:
            SetProperty(UrlPropertyName, oidcApp.Settings?.OauthClient?.InitiateLoginUri);

            /* Sample additional properties for specific apps:
            Microsoft Entra ID (formerly Azure AD) External Authentication
                "microsoftDiscoveryEndpoint": "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                "microsoftTenantId": "c364c732-ad9c-441c-9cf4-d3c3656b551c",
                "microsoftAppId": "9539823d-3806-43cf-808d-962b53635c3b"
            LDAP Interface:
                "domainName": null
            */

            // Fetch additional properties
            foreach (var property in oidcApp.Settings?.App ?? [])
            {
                SetProperty(property.Key, property.Value);
            }
        }
        else if (application is SamlApplication saml2App)
        {
            // SAML 2.0 Application
            SetProperty(ApplicationTypePropertyName, saml2App.Name);

            // Sample template: ${source.login}
            SetProperty(UserNameMappingPropertyName, saml2App.Credentials?.UserNameTemplate?.Template);

            // SAML Assertion Consumer Service (ACS) URL
            SetProperty(UrlPropertyName, saml2App.Settings?.SignOn?.SsoAcsUrl);

            /* Sample additional properties for specific apps:
            GitHub Enterprise Cloud:
                "githubOrg": "Contoso"
            Jamf Pro SAML:
                "domain": "contoso.jamfcloud.com"
            Amazon AWS SSO:
                "acsURL": "https://res-gaenv1-232e2f51-652d-42b1-b1c9-cf38b25638db.auth.us-east-1.amazoncognito.com/saml2/idpresponse",
                "entityID": "https://res-gaenv1-232e2f51-652d-42b1-b1c9-cf38b25638db.auth.us-east-1.amazoncognito.com"
            Google Workspace:
                "afwOnly": false,
                "domain": "contoso.com",
                "afwId": null,
                "rpId": null
            */

            // Fetch additional properties
            foreach (var property in saml2App.Settings?.App ?? [])
            {
                SetProperty(property.Key, property.Value);
            }
        }
        else if (application is Saml11Application saml1App)
        {
            // SAML 1.1 Application / WS-Federation Application
            SetProperty(ApplicationTypePropertyName, saml1App.Name);

            // Sample template: ${source.login}
            SetProperty(UserNameMappingPropertyName, saml1App.Credentials?.UserNameTemplate?.Template);

            // SAML Assertion Consumer Service (ACS) URL
            // TODO: SetProperty(UrlPropertyName, saml1App.Settings?.SignOn?.SsoAcsUrl);

            /* Sample additional properties for specific apps:
            Office 365:
                "domain": "contoso.com",
                "domains": [
                    "contoso.com"
                ],
                "msftTenant": "contoso",
            */

            // Fetch additional properties
            foreach (var property in saml1App.Settings?.App ?? [])
            {
                SetProperty(property.Key, property.Value);
            }

            // Derive Entra Tenant ID from the primary domain if specified
            string? onMicrosoftDomain = GetProperty<string>(EntraOnMicrosoftDomainPropertyName);
            string? tenantId = EntraIdTenant.GetTenantIdFromOnMicrosoftDomain(onMicrosoftDomain).GetAwaiter().GetResult();
            SetProperty(EntraTenantIdPropertyName, tenantId);
        }
        else if (application is WsFederationApplication wsFedApp)
        {
            // WS-Federation Application
            SetProperty(ApplicationTypePropertyName, wsFedApp.Name?.Value);
        }
        else if (application is AutoLoginApplication swaApp)
        {
            // Secure Web Authentication (SWA) Application
            SetProperty(ApplicationTypePropertyName, swaApp.Name);
            SetProperty(UrlPropertyName, swaApp.Settings?.SignOn?.LoginUrl);

            // Fetch additional properties
            foreach (var property in swaApp.Settings?.App ?? [])
            {
                SetProperty(property.Key, property.Value);
            }
        }
        else if (application is BrowserPluginApplication browserPluginApp)
        {
            // Browser Plugin Application
            SetProperty(ApplicationTypePropertyName, browserPluginApp.Name?.Value);
            SetProperty(UrlPropertyName, browserPluginApp.Settings?.App?.Url);

            // Fetch additional properties
            foreach (var property in browserPluginApp.Settings?.App?.AdditionalProperties ?? ImmutableDictionary<string, object>.Empty)
            {
                SetProperty(property.Key, property.Value);
            }

            /* Sample additional properties for specific apps:
            AWS Account Federation
                "appFilter": "okta",
                "groupFilter": "aws_(?{{accountid}}\\d+)_(?{{role}}[a-zA-Z0-9+=,.@\\-_]+)",
                "secretKey": null,
                "webSSOAllowedClient": null,
                "useGroupMapping": false,
                "joinAllRoles": false,
                "identityProviderArn": null,
                "overrideAcsURL": null,
                "sessionDuration": 3600,
                "roleValuePattern": "arn:aws:iam::${accountid}:saml-provider/OKTA,arn:aws:iam::${accountid}:role/${role}",
                "accountID": null,
                "awsEnvironmentType": "aws.amazon",
                "accessKey": null,
                "loginURL": "https://console.aws.amazon.com/ec2/home",
                "secretKeyEnc": null

            1Password Business
                "regionType": "com",
                "subDomain": "contoso"

            Jamf Pro SWA:
                "loginURL": "https://jss.acme.com/"
            */
        }
        else if (application is SecurePasswordStoreApplication spsApp)
        {
            SetProperty(ApplicationTypePropertyName, spsApp.Name?.Value);
            SetProperty(UrlPropertyName, spsApp.Settings?.App?.Url);

            // TODO: Consider fetching additional properties of SPS apps (requires OpenAPI schema update)
        }
        else if (application is BasicAuthApplication basicAuthApp)
        {
            // Basic Authentication Application
            SetProperty(ApplicationTypePropertyName, basicAuthApp.Name?.Value);
            SetProperty(UrlPropertyName, basicAuthApp.Settings?.App?.Url);
        }
        else if (application is BookmarkApplication bookmarkApp)
        {
            // Bookmark-only Application
            SetProperty(ApplicationTypePropertyName, bookmarkApp.Name?.Value);
            SetProperty(UrlPropertyName, bookmarkApp.Settings?.App?.Url);
        }
        else if (application is ActiveDirectoryApplication adApp)
        {
            // SignOnMode property is null and we have the base Application object.
            // This has only been observed for Active Directory synced domains.
            SetProperty(ApplicationTypePropertyName, adApp.Name);
            SetProperty("namingContext", adApp.Settings?.App.NamingContext);
            SetProperty("filterGroupsByOU", adApp.Settings?.App.FilterGroupsByOU);

            /* AD Synced Domain additional properties sample:
            "jitGroupsAcrossDomains": false,
            "password": null,
            "scanRate": null,
            "searchOrgUnit": null,
            "filterGroupsByOU": false,
            "namingContext": "contoso.com",
            "login": null,
            "activationEmail": null
            */
        }
        // There might be other (undocumented) application types we do not specifically handle yet.
    }

    public OpenGraphEdgeNode? CreateHybridUserNode(string? targetUserId)
    {
        if (targetUserId is null)
        {
            // Mapping is not configured or could not be resolved.
            return null;
        }

        // Check if the application is supported by a known BloodHound collector
        return ApplicationType switch
        {
            // TODO: Check that the Jamf mapping is OK
            JamfProSamlApplicationType or JamfProSwaApplicationType => new OpenGraphEdgeNode(targetUserId, "jamf_Account", "name"),

            // TODO: Check that the GitHub mapping is OK
            GitHubCloudApplicationType => new OpenGraphEdgeNode(targetUserId, "GH_User", "name"),

            // TODO: Check OP mapping
            OnePasswordBusinessApplicationType => new OpenGraphEdgeNode(targetUserId, "OPUser", "name"),

            // SAML-only Snowflake users (without SCIM) can be matched by the username.
            SnowflakeApplicationType => new OpenGraphEdgeNode(targetUserId, "SNOWUser", "name"),

            // Entra ID users are matched by their userPrincipalName.
            // TODO: Add tenant ID to the node properties?
            Office365ApplicationType => new OpenGraphEdgeNode(targetUserId, "AZUser", matchBy: "name"),

            // TODO: Add support for additional applications
            _ => null // This app is not yet supported
        };
    }

    public OpenGraphEdge? CreateHybridUserSignOnEdge(string sourceUserId, string? targetUserId)
    {
        OpenGraphEdgeNode? endNode = CreateHybridUserNode(targetUserId);
        string? edgeKind = HybridUserSignOnEdgeType;

        OpenGraphEdge? result = null;

        if (endNode is not null && edgeKind is not null)
        {
            // Example: (:Okta_User)-[:Okta_OutboundSSO]->(:Jamf_Account)
            // Example: (:Okta_User)-[:Okta_SWA]->(:Jamf_Account)
            OpenGraphEdgeNode startNode = OktaUser.CreateEdgeNode(sourceUserId);
            result = new(startNode, endNode, edgeKind);

            // Set SSO mode on the edge (SAML / OIDC / WS-FED,...)
            result.SetProperty("mode", SignOnMode);
        }

        return result;
    }

    public OpenGraphEdgeNode? CreateHybridGroupEdgeNode(OktaUserGroupProfile groupProfile)
    {
        return ApplicationType switch
        {
            // TODO: Check that the AD group mapping is OK
            // AD groups are matched by their qualified name, e.g., IT@contoso.com
            ActiveDirectoryApplicationType => ActiveDirectoryDomain is not null ? new OpenGraphEdgeNode($"{groupProfile.Name}@{ActiveDirectoryDomain}", "Group", matchBy: "name") : null,

            // Entra ID groups are matched by their qualified name, e.g., Test@8cb4e812-3974-4f6d-bc74-abcfcd70f252
            // TODO: Validate that the Entra group mapping works
            Office365ApplicationType => EntraTenantId is not null ? new OpenGraphEdgeNode($"{groupProfile.Name}@{EntraTenantId}", "AZGroup", matchBy: "name") : null,
            // TODO: Add mapping for SNOWGroup
            SnowflakeApplicationType => null,
            // TODO: Add support for Org2Org group push mappings
            // TODO: Add support for additional applications
            _ => null // This app is not yet supported
        };
    }

    public OpenGraphEdgeNode? CreateOutboundTrustNode()
    {
        return ApplicationType switch
        {
            // TODO: Check that the Jamf mapping is OK
            JamfProSamlApplicationType => JamfDomain is not null ? new OpenGraphEdgeNode(JamfDomain, "jamf_Tenant", "name") : null,

            // TODO: Check that the GitHub mapping is OK
            GitHubCloudApplicationType => GitHubOrganization is not null ? new OpenGraphEdgeNode(GitHubOrganization, "GH_Organization", "name") : null,

            // TODO: Check OP mapping
            OnePasswordBusinessApplicationType => OnePasswordDomain is not null ? new OpenGraphEdgeNode(OnePasswordDomain, "OPAccount", "name") : null,

            // Snowflake accounts are matched by the subdomain stored in the ID attribute, e.g., CGXOVHZ-NR46411.
            SnowflakeApplicationType => SnowflakeSubdomain is not null ? new OpenGraphEdgeNode(SnowflakeSubdomain, "SNOWAccount") : null,

            // Entra ID tenants are matched by their tenant ID, e.g., 31537af4-6d77-4bb9-a681-d2394888ea26.
            Office365ApplicationType => EntraTenantId is not null ? EntraIdTenant.CreateEdgeNode(EntraTenantId) : null,

            // TODO: Add support for additional applications
            _ => null // This app is not yet supported
        };
    }

    public OpenGraphEdge? CreateOutboundTrustEdge()
    {
        OpenGraphEdgeNode? endNode = CreateOutboundTrustNode();
        string? edgeKind = HybridApplicationEdgeKind;

        OpenGraphEdge? result = null;

        if (endNode is not null && edgeKind is not null)
        {
            // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:Jamf_Tenant)
            // Example: (:Okta_Application)-[:Okta_OrgSWA]->(:Jamf_Tenant)
            // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:GHOrganization)
            // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:AZTenant)
            result = new(this, endNode, edgeKind);

            // Set SSO mode on the edge (SAML / OIDC / WS-FED,...)
            result.SetProperty("mode", SignOnMode);
        }

        return result;
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
