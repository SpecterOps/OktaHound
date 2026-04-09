using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaUser : OktaSecurityPrincipal
{
    public const string NodeKind = "Okta_User";
    public const string ManagerOfEdgeKind = "Okta_ManagerOf";
    public const string UserSyncEdgeKind = "Okta_UserSync";
    public const string UserPushEdgeKind = "Okta_UserPush";
    public const string UserPullEdgeKind = "Okta_UserPull";
    public const string PasswordSyncEdgeKind = "Okta_PasswordSync";
    public const string SingleSignOnEdgeKind = "Okta_OutboundSSO";
    public const string SecureWebAuthenticationEdgeKind = "Okta_SWA";
    private const string EnabledPropertyName = "enabled";
    private const string ManagerIdPropertyName = "managerId";
    private const string RealmIdPropertyName = "realmId";
    private const string LoginPropertyName = "login";
    private const string EmailPropertyName = "email";
    private const string AuthenticationFactorsCountPropertyName = "authenticationFactors";

    [JsonIgnore]
    public int AuthenticationFactorsCount
    {
        get => GetPropertyAsInt(AuthenticationFactorsCountPropertyName) ?? 0;
        set => SetProperty(AuthenticationFactorsCountPropertyName, value);
    }

    [JsonIgnore()]
    public bool Enabled
    {
        get => GetPropertyAsBool(EnabledPropertyName) ?? false;
        set => SetProperty(EnabledPropertyName, value);
    }

    [JsonIgnore]
    public string? Login => GetProperty<string>(LoginPropertyName);

    [JsonIgnore]
    public string? EmailAddress => GetProperty<string>(EmailPropertyName);

    [JsonIgnore]
    public string? ManagerId => GetProperty<string>(ManagerIdPropertyName);

    [JsonIgnore]
    public string? RealmId => GetProperty<string>(RealmIdPropertyName);

    public OktaUser(User user, string domainName) : base(user.Id, domainName, NodeKind)
    {
        // Write common properties
        Name = user.Profile?.Login;
        DisplayName = user.Profile?.DisplayName;

        // Determine if user is enabled (see the Okta_User node documentation for details)
        if (user.Status == UserStatus.SUSPENDED || user.Status == UserStatus.DEPROVISIONED || user.Status == UserStatus.STAGED)
        {
            Enabled = false;
        }
        else
        {
            // ACTIVE, PASSWORD_EXPIRED, LOCKED_OUT, PROVISIONED, RECOVERY
            Enabled = true;
        }

        // Write additional properties
        // TODO: Unify these keys across AD and Entra ID
        SetProperty("firstName", user.Profile?.FirstName);
        SetProperty("lastName", user.Profile?.LastName);
        SetProperty(LoginPropertyName, user.Profile?.Login);
        SetProperty(EmailPropertyName, user.Profile?.Email);
        SetProperty("status", user.Status?.Value);
        SetProperty("created", user.Created);
        SetProperty("lastLogin", user.LastLogin);
        SetProperty("lastUpdated", user.LastUpdated);
        SetProperty("passwordChanged", user.PasswordChanged);
        SetProperty("activated", user.Activated);
        SetProperty("userType", user.Profile?.UserType);
        SetProperty("title", user.Profile?.Title);
        SetProperty("department", user.Profile?.Department);
        SetProperty("city", user.Profile?.City);
        SetProperty("state", user.Profile?.State);
        SetProperty("countryCode", user.Profile?.CountryCode);
        SetProperty("employeeNumber", user.Profile?.EmployeeNumber);
        SetProperty("organization", user.Profile?.Organization);
        SetProperty("division", user.Profile?.Division);
        SetProperty(RealmIdPropertyName, user.RealmId);
        SetProperty(ManagerIdPropertyName, user.Profile?.ManagerId);

        if (user.Credentials is not null)
        {
            if (user.Credentials.Provider is not null)
            {
                SetProperty("credentialProviderType", user.Credentials.Provider.Type?.Value);
                SetProperty("credentialProviderName", user.Credentials.Provider.Name);
            }
        }

        // Initialize to the default value
        AuthenticationFactorsCount = 0;
    }

    [return: NotNullIfNotNull(nameof(id))]
    public static new OpenGraphEdgeNode? CreateEdgeNode(string? id) => id is null ? null : new OpenGraphEdgeNode(id, NodeKind);
}
