using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApplicationUserAssignment
{
    public string ApplicationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? TargetUserName { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? SyncState { get; set; }
    public Dictionary<string, string> Profile { get; set; } = [];
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? StatusChanged { get; set; }
    public DateTimeOffset? PasswordChanged { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public DateTimeOffset? LastSync { get; set; }

    public OktaApplication? Application { get; set; } = null!;
    public OktaUser? User { get; set; } = null!;

    private OktaApplicationUserAssignment()
    {
    }

    public OktaApplicationUserAssignment(AppUser appUser, string applicationId)
    {
        ApplicationId = applicationId;
        UserId = appUser.Id;
        Created = appUser.Created;
        ExternalId = appUser.ExternalId;
        TargetUserName = appUser.Credentials?.UserName;
        LastSync = appUser.LastSync;
        LastUpdated = appUser.LastUpdated;
        PasswordChanged = appUser.PasswordChanged;
        Scope = appUser.Scope.Value;
        Status = appUser.Status?.Value;
        StatusChanged = appUser.StatusChanged;
        SyncState = appUser.SyncState?.Value;

        foreach (var profileProperty in appUser?.Profile ?? [])
        {
            string? stringValue = profileProperty.Value?.ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                continue;
            }

            Profile[profileProperty.Key] = stringValue;
        }
    }
}
