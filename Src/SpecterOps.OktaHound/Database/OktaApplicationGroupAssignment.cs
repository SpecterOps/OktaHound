using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApplicationGroupAssignment
{
    public string ApplicationId { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public DateTimeOffset? LastUpdated { get; set; }

    public int? Priority { get; set; }

    public Dictionary<string, string>? Profile { get; set; }

    [JsonIgnore]
    public OktaApplication? Application { get; set; } = null!;

    [JsonIgnore]
    public OktaGroup? Group { get; set; } = null!;

    private OktaApplicationGroupAssignment()
    {
    }

    public OktaApplicationGroupAssignment(ApplicationGroupAssignment appGroupAssignment, string applicationId)
    {
        ApplicationId = applicationId;
        GroupId = appGroupAssignment.Id;
        LastUpdated = appGroupAssignment.LastUpdated;
        Priority = appGroupAssignment.Priority;

        // Convert the profile from Dictionary<string, object?> to Dictionary<string, string>, ignoring null or non-string values
        foreach (var profileProperty in appGroupAssignment?.Profile ?? [])
        {
            string? stringValue = profileProperty.Value?.ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                continue;
            }

            Profile ??= [];
            Profile[profileProperty.Key] = stringValue;
        }
    }
}
