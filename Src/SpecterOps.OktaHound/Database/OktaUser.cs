using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

[Index(nameof(Login), IsUnique = true)]
[Index(nameof(EmailAddress), IsUnique = false)]
public sealed class OktaUser : OktaEntity
{
    public const string NodeKind = "Okta_User";
    public const string ManagerOfEdgeKind = "Okta_ManagerOf";
    public const string UserSyncEdgeKind = "Okta_UserSync";
    public const string UserPushEdgeKind = "Okta_UserPush";
    public const string UserPullEdgeKind = "Okta_UserPull";
    public const string SingleSignOnEdgeKind = "Okta_OutboundSSO";
    public const string SecureWebAuthenticationEdgeKind = "Okta_SWA";
    public bool Enabled { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Login { get; set; }

    [JsonPropertyName("email")]
    public string? EmailAddress { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public DateTimeOffset? PasswordChanged { get; set; }
    public DateTimeOffset? Activated { get; set; }
    public string? UserType { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CountryCode { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? Organization { get; set; }
    public string? Division { get; set; }
    public string? RealmId { get; set; }

    [JsonIgnore]
    public string? ManagerId { get; set; }

    [JsonPropertyName("managerId")]
    public string? OriginalManagerId { get; set; }

    public string? CredentialProviderType { get; set; }
    public string? CredentialProviderName { get; set; }

    [JsonPropertyName("authenticationFactors")]
    public int AuthenticationFactorCount => AuthenticationFactors?.Count ?? 0;

    [JsonIgnore]
    public OktaRealm? Realm { get; set; }

    [JsonIgnore]
    public OktaUser? Manager { get; set; }

    [JsonIgnore]
    public List<OktaUser> DirectReports { get; set; } = [];

    [JsonIgnore]
    public List<OktaApiToken> ApiTokens { get; set; } = [];

    [JsonIgnore]
    public List<OktaApiServiceIntegration> CreatedApiServiceIntegrations { get; set; } = [];

    [JsonIgnore]
    public List<OktaDevice> OwnedDevices { get; set; } = [];

    [JsonIgnore]
    public List<OktaGroup> Groups { get; set; } = [];

    [JsonIgnore]
    public List<OktaUserFactor> AuthenticationFactors { get; set; } = [];

    protected override string[] Kinds => [NodeKind];

    private OktaUser() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaUser(User user, string domainName)
        : base(user.Id, user.Profile.Login, domainName)
    {
        DisplayName = user.Profile.DisplayName;
        Enabled = IsEnabled(user.Status);
        FirstName = user.Profile.FirstName;
        LastName = user.Profile.LastName;
        Login = user.Profile.Login;
        EmailAddress = user.Profile.Email;
        Status = user.Status.Value;
        Created = user.Created;
        LastLogin = user.LastLogin;
        LastUpdated = user.LastUpdated;
        PasswordChanged = user.PasswordChanged;
        Activated = user.Activated;
        UserType = user.Profile.UserType;
        Title = user.Profile.Title;
        Department = user.Profile.Department;
        City = user.Profile.City;
        State = user.Profile.State;
        CountryCode = user.Profile.CountryCode;
        EmployeeNumber = user.Profile.EmployeeNumber;
        Organization = user.Profile.Organization;
        Division = user.Profile.Division;
        RealmId = user.RealmId;
        OriginalManagerId = user.Profile.ManagerId;

        if (user.Credentials is not null)
        {
            if (user.Credentials.Provider is not null)
            {
                CredentialProviderType = user.Credentials.Provider.Type?.Value;
                CredentialProviderName = user.Credentials.Provider.Name;
            }
        }
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaUser);
    }

    /// <summary>
    /// Determines if a user is enabled based on their Okta UserStatus.
    /// </summary>
    /// <param name="status">The UserStatus to evaluate.</param>
    /// <returns>True if the user is enabled; otherwise, false.</returns>
    internal static bool IsEnabled(UserStatus status)
    {
        if (status == UserStatus.SUSPENDED || status == UserStatus.DEPROVISIONED || status == UserStatus.STAGED)
        {
            return false;
        }
        else
        {
            // ACTIVE, PASSWORD_EXPIRED, LOCKED_OUT, PROVISIONED, RECOVERY
            return true;
        }
    }

    // public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
