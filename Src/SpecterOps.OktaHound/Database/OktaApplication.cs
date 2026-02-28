using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.ActiveDirectory;
using SpecterOps.OktaHound.Model.Entra;
using SpecterOps.OktaHound.Model.GitHub;
using SpecterOps.OktaHound.Model.Jamf;
using SpecterOps.OktaHound.Model.OnePassword;
using SpecterOps.OktaHound.Model.OpenGraph;
using SpecterOps.OktaHound.Model.Snowflake;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApplication : OktaSecurityPrincipal
{
    public const string NodeKind = "Okta_Application";
    public const string AppAssignmentEdgeKind = "Okta_AppAssignment";
    public const string ReadPasswordUpdatesEdgeKind = "Okta_ReadPasswordUpdates";
    public const string OrganizationalSingleSignOnEdgeKind = "Okta_OutboundOrgSSO";
    public const string OrganizationalSecureWebAuthenticationEdgeKind = "Okta_OrgSWA";

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

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public string? Status { get; set; }
    public string? ApplicationType { get; set; }
    public string? ClientType { get; set; }
    public string? SignOnMode { get; set; }
    public List<string>? Features { get; set; }
    public List<string>? Permissions { get; set; }
    public string? UserNameMapping { get; set; }
    public string? Url { get; set; }
    public List<string>? GrantTypes { get; set; }
    public string? GitHubOrganizationName { get; set; }

    /// <summary>
    /// FQDN of the domain this app represents.
    /// </summary>
    public string? ActiveDirectoryDomain { get; set; }

    /// <summary>
    /// Security identifier (SID) of the AD domain this app represents.
    /// </summary>
    /// <remarks>
    /// Domain SID is not stored in app objects, but we can derive it from the SIDs of the associated users or groups.
    /// </remarks>
    public string? ActiveDirectoryDomainSid { get; set; }

    public string? EntraIdDiscoveryEndpoint { get; set; }
    public string? EntraOnMicrosoftDomain { get; set; }
    public string? EntraTenantId { get; set; }
    public string? EntraClientId { get; set; }
    public string? JamfDomain { get; set; }
    public string? SnowflakeSubdomain { get; set; }
    public string? OnePasswordRegionType { get; set; }
    public string? OnePasswordSubDomain { get; set; }
    public bool? FilterGroupsByOU { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = [];

    [JsonIgnore]
    public List<OktaJWK> JWKs { get; set; } = [];

    [JsonIgnore]
    public List<OktaClientSecret> ClientSecrets { get; set; } = [];

    [JsonIgnore]
    public List<OktaGroup> ImportedGroups { get; set; } = [];

    [JsonIgnore]
    [NotMapped]
    public bool SupportsSCIM => Features?.Contains("SCIM_PROVISIONING") ?? false;

    [JsonIgnore]
    [NotMapped]
    public bool SupportsPasswordUpdates => Features?.Contains("PUSH_PASSWORD_UPDATES") ?? false;

    [JsonIgnore]
    [NotMapped]
    public bool IsService => ClientType == OpenIdConnectApplicationType.Service.Value;

    [JsonIgnore]
    [NotMapped]
    public bool IsActiveDirectory => ApplicationType == ActiveDirectoryApplicationType;

    [JsonIgnore]
    [NotMapped]
    public bool IsLdapInterface => ApplicationType == LdapInterfaceApplicationType;

    [JsonIgnore]
    [NotMapped]
    public bool IsEntraId => ApplicationType == EntraIdApplicationType;

    [JsonIgnore]
    [NotMapped]
    public bool IsSAMLApplication =>
        SignOnMode == ApplicationSignOnMode.SAML20.Value ||
        SignOnMode == ApplicationSignOnMode.SAML11.Value ||
        SignOnMode == ApplicationSignOnMode.WSFEDERATION;

    [JsonIgnore]
    [NotMapped]
    public bool IsOIDCApplication => SignOnMode == ApplicationSignOnMode.OPENIDCONNECT.Value;

    [JsonIgnore]
    [NotMapped]
    public bool IsSWAApplication =>
        SignOnMode == ApplicationSignOnMode.AUTOLOGIN.Value ||
        SignOnMode == ApplicationSignOnMode.BASICAUTH.Value ||
        SignOnMode == ApplicationSignOnMode.BROWSERPLUGIN.Value;

    [JsonIgnore]
    [NotMapped]
    public bool IsBookmarkApplication => SignOnMode == ApplicationSignOnMode.BOOKMARK.Value;

    /// <summary>
    /// Indicates whether outbound user SCIM sync edges for this application should be ignored.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
    public bool IsOutboundSyncIgnored => IgnoredOutboundSyncApps.Contains(ApplicationType ?? string.Empty);

    /// <summary>
    /// Indicates whether user app assignment edges for this application should be ignored.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
    public bool IsAssignmentIgnored => IgnoredAssignedApps.Contains(ApplicationType ?? string.Empty);

    /// <summary>
    /// Gets the OnePassword domain for this application.
    /// </summary>
    /// <example>contoso.1Password.com</example>
    [JsonIgnore]
    [NotMapped]
    public string? OnePasswordDomain => OnePasswordAccount.GetDomain(OnePasswordSubDomain, OnePasswordRegionType);

    [JsonIgnore]
    [NotMapped]
    public string? EntraRegion => EntraIdTenant.GetRegionFromEndpoint(EntraIdDiscoveryEndpoint);

    /// <summary>
    /// Gets the type of edge connecting a user in this organization with another account in a different tenant.
    /// scenario.
    /// </summary>
    [JsonIgnore]
    [NotMapped]
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
    [NotMapped]
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

    protected override string[] Kinds => [NodeKind];

    private OktaApplication() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaApplication(Application application, string domainName) : base(application.Id, application.Label, domainName)
    {
        DisplayName = application.Label;
        Created = application.Created;
        LastUpdated = application.LastUpdated;
        Status = application.Status?.Value;
        Features = application.Features?.Select(feature => feature.Value).ToList();
        SignOnMode = application.SignOnMode?.Value;

        if (application is OpenIdConnectApplication oidcApp)
        {
            // OIDC Application
            // Sample Name values:
            // saasure: Okta Admin Console
            // oidc_client: Generic OIDC app
            // aws: Amazon Web Services
            // office365: Entra ID
            ApplicationType = oidcApp.Name?.Value;

            // Sample client types: web, browser, native, service
            ClientType = oidcApp.Settings?.OauthClient?.ApplicationType?.Value;

            // Sample grant types: authorization_code, implicit, refresh_token, client_credentials
            GrantTypes = oidcApp.Settings?.OauthClient?.GrantTypes?.Select(grant => grant.Value).ToList();

            // Sample template: ${source.login}
            UserNameMapping = oidcApp.Credentials?.UserNameTemplate?.Template;
            // The default primary redirect URI for OIDC SPA apps is http://localhost:8080/login/callback
            // We will collect the optional Initiate login URI instead:
            Url = oidcApp.Settings?.OauthClient?.InitiateLoginUri ?? oidcApp.Settings?.OauthClient?.RedirectUris?.FirstOrDefault();

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
                SetKnownProperty(property.Key, property.Value);
                SetCustomProperty(property.Key, property.Value);
            }
        }
        else if (application is SamlApplication saml2App)
        {
            // SAML 2.0 Application
            ApplicationType = saml2App.Name;

            // Sample template: ${source.login}
            UserNameMapping = saml2App.Credentials?.UserNameTemplate?.Template;

            // SAML Assertion Consumer Service (ACS) URL
            Url = saml2App.Settings?.SignOn?.SsoAcsUrl;

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
                SetKnownProperty(property.Key, property.Value);
                SetCustomProperty(property.Key, property.Value);
            }
        }
        else if (application is Saml11Application saml1App)
        {
            // SAML 1.1 Application / WS-Federation Application
            ApplicationType = saml1App.Name;

            // Sample template: ${source.login}
            UserNameMapping = saml1App.Credentials?.UserNameTemplate?.Template;

            // SAML Assertion Consumer Service (ACS) URL
            Url = saml1App.Settings?.SignOn?.SsoAcsUrlOverride;

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
                SetKnownProperty(property.Key, property.Value);
                SetCustomProperty(property.Key, property.Value);
            }

            // Derive Entra Tenant ID from the primary domain if specified
            EntraTenantId = EntraIdTenant.GetTenantIdFromOnMicrosoftDomain(EntraOnMicrosoftDomain).GetAwaiter().GetResult();
        }
        else if (application is WsFederationApplication wsFedApp)
        {
            // WS-Federation Application
            ApplicationType = wsFedApp.Name?.Value;
        }
        else if (application is AutoLoginApplication swaApp)
        {
            // Secure Web Authentication (SWA) Application
            ApplicationType = swaApp.Name;
            Url = swaApp.Settings?.SignOn?.LoginUrl;

            // Fetch additional properties
            foreach (var property in swaApp.Settings?.App ?? [])
            {
                SetKnownProperty(property.Key, property.Value);
                SetCustomProperty(property.Key, property.Value);
            }
        }
        else if (application is BrowserPluginApplication browserPluginApp)
        {
            ApplicationType = browserPluginApp.Name?.Value;
            Url = browserPluginApp.Settings?.App?.Url;

            foreach (var property in browserPluginApp.Settings?.App?.AdditionalProperties ?? ImmutableDictionary<string, object>.Empty)
            {
                SetKnownProperty(property.Key, property.Value);
                SetCustomProperty(property.Key, property.Value);
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
            ApplicationType = spsApp.Name?.Value;
            Url = spsApp.Settings?.App?.Url;
            // TODO: Consider fetching additional properties of SPS apps (requires OpenAPI schema update)
        }
        else if (application is BasicAuthApplication basicAuthApp)
        {
            // Basic Authentication Application
            ApplicationType = basicAuthApp.Name?.Value;
            Url = basicAuthApp.Settings?.App?.Url;
        }
        else if (application is BookmarkApplication bookmarkApp)
        {
            // Bookmark-only Application
            ApplicationType = bookmarkApp.Name?.Value;
            Url = bookmarkApp.Settings?.App?.Url;
        }
        else if (application is ActiveDirectoryApplication adApp)
        {
            // SignOnMode property is null and we have the base Application object.
            // This has only been observed for Active Directory synced domains.
            ApplicationType = adApp.Name;
            ActiveDirectoryDomain = adApp.Settings?.App.NamingContext;
            FilterGroupsByOU = adApp.Settings?.App.FilterGroupsByOU;
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

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaApplication);
    }

    /*
    public OpenGraphEdgeNode? CreateHybridUserNode(string? targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            // Mapping is not configured or could not be resolved.
            return null;
        }

        // Check if the application is supported by a known BloodHound collector
        return ApplicationType switch
        {
            JamfProSamlApplicationType or JamfProSwaApplicationType => JamfAccount.CreateEdgeNode(targetUserId, JamfDomain),
            GitHubCloudApplicationType => GitHubUser.CreateEdgeNode(targetUserId, GitHubOrganizationName),
            OnePasswordBusinessApplicationType => OnePasswordUser.CreateEdgeNode(targetUserId, OnePasswordDomain),

            // SAML-only Snowflake users (without SCIM) can be matched by the username.
            SnowflakeApplicationType => SnowflakeUser.CreateEdgeNode(targetUserId, SnowflakeSubdomain),

            // Entra ID users are matched by their userPrincipalName.
            Office365ApplicationType => EntraIdUser.CreateEdgeNode(targetUserId, EntraTenantId),

            // TODO: Add support for additional applications
            _ => null // This app is not yet supported
        };
    }

    public OpenGraphEdge? CreateHybridUserSignOnEdge(string sourceUserId, string? targetUserId)
    {
        OpenGraphEdgeNode? endNode = CreateHybridUserNode(targetUserId);
        string? edgeKind = HybridUserSignOnEdgeType;

        if (endNode is null || edgeKind is null)
        {
            return null;
        }

        // Example: (:Okta_User)-[:Okta_OutboundSSO]->(:Jamf_Account)
        // Example: (:Okta_User)-[:Okta_SWA]->(:Jamf_Account)
        OpenGraphEdgeNode startNode = new(sourceUserId, OktaUser.NodeKind);
        OpenGraphEdge result = new(startNode, endNode, edgeKind);

        // Set SSO mode on the edge (SAML / OIDC / WS-FED,...)
        result.SetProperty("mode", SignOnMode);
        return result;
    }

    public OpenGraphEdgeNode? CreateHybridGroupEdgeNode(OktaUserGroupProfile groupProfile)
    {
        return ApplicationType switch
        {
            // AD groups are matched by their name and domain FQDN, e.g., contoso.com
            ActiveDirectoryApplicationType => ActiveDirectoryGroup.CreateEdgeNode(groupProfile.Name, ActiveDirectoryDomain),

            // Entra ID groups are matched by their display name and tenant ID, e.g., "Test" + 8cb4e812-3974-4f6d-bc74-abcfcd70f252
            Office365ApplicationType => EntraIdGroup.CreateEdgeNode(groupProfile.Name, EntraTenantId),

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
            JamfProSamlApplicationType or JamfProSwaApplicationType => JamfTenant.CreateEdgeNode(JamfDomain),

            GitHubCloudApplicationType => GitHubOrganization.CreateEdgeNode(GitHubOrganizationName),

            OnePasswordBusinessApplicationType => OnePasswordAccount.CreateEdgeNode(OnePasswordDomain),

            // Snowflake accounts are matched by the subdomain stored in the ID attribute, e.g., CGXOVHZ-NR46411.
            SnowflakeApplicationType => SnowflakeAccount.CreateEdgeNode(SnowflakeSubdomain),

            // Entra ID tenants are matched by their tenant ID, e.g., 31537af4-6d77-4bb9-a681-d2394888ea26.
            Office365ApplicationType => EntraIdTenant.CreateEdgeNode(EntraTenantId),

            // TODO: Add support for additional applications
            _ => null // This app is not yet supported
        };
    }

    public OpenGraphEdge? CreateOutboundTrustEdge()
    {
        OpenGraphEdgeNode? endNode = CreateOutboundTrustNode();
        string? edgeKind = HybridApplicationEdgeKind;

        if (endNode is null || edgeKind is null)
        {
            return null;
        }

        // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:Jamf_Tenant)
        // Example: (:Okta_Application)-[:Okta_OrgSWA]->(:Jamf_Tenant)
        // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:GHOrganization)
        // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:AZTenant)
        OpenGraphEdge result = new(new OpenGraphEdgeNode(Id, NodeKind), endNode, edgeKind);

        // Set SSO mode on the edge (SAML / OIDC / WS-FED,...)
        result.SetProperty("mode", SignOnMode);
        return result;
    }
    */
    // public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);

    private void SetKnownProperty(string key, object? value)
    {
        switch (key)
        {
            case "githubOrg":
                GitHubOrganizationName = value?.ToString();
                break;
            case "namingContext":
                ActiveDirectoryDomain = value?.ToString();
                break;
            case "domainSid":
                ActiveDirectoryDomainSid = value?.ToString();
                break;
            case "microsoftDiscoveryEndpoint":
                EntraIdDiscoveryEndpoint = value?.ToString();
                break;
            case "msftTenant":
                EntraOnMicrosoftDomain = value?.ToString();
                break;
            case "microsoftTenantId":
                EntraTenantId = value?.ToString();
                break;
            case "microsoftAppId":
                EntraClientId = value?.ToString();
                break;
            case "domain":
                JamfDomain = value?.ToString();
                break;
            case "subDomain":
                SnowflakeSubdomain ??= value?.ToString();
                OnePasswordSubDomain ??= value?.ToString();
                break;
            case "regionType":
                OnePasswordRegionType = value?.ToString();
                break;
            default:
                break;
        }
    }

    private void SetCustomProperty(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            return;
        }

        string? stringValue = value.ToString();

        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return;
        }

        CustomProperties[key] = stringValue;
    }
}
