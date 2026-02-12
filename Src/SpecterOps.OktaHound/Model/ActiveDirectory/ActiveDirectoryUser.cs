using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.ActiveDirectory;

internal sealed class ActiveDirectoryUser : OpenGraphNode
{
    private const string NodeKind = "User";
    private const string ObjectSidPropertyName = "objectsid";
    private const string ObjectGuidPropertyName = "objectguid";

    [JsonIgnore]
    public string? ObjectSid => GetProperty<string>(ObjectSidPropertyName);

    [JsonIgnore]
    public string ObjectGuid => GetProperty<string>(ObjectGuidPropertyName) ?? throw new ArgumentException("The AD user does not have objectGuid.");

    [JsonIgnore]
    public string? DomainSid
    {
        get
        {
            if (ObjectSid is null)
            {
                // SID is typically not available for Okta->AD synced users.
                return null;
            }

            // Cut off the RID from the SID, e.g., S-1-5-21-2697957641-2271029196-387917394-500
            var ridSeparatorIndex = ObjectSid.LastIndexOf('-');
            return ObjectSid[..ridSeparatorIndex];
        }
    }

    public ActiveDirectoryUser(AppUser user, string? domainName) : base(GetUserId(user), [NodeKind])
    {
        // userPrincipalName or samAccountName
        var upn = GetProfileProperty(user, "userName");
        var samAccountName = GetProfileProperty(user, "samAccountName");

        if (upn is null && samAccountName is not null && domainName is not null)
        {
            // Construct the UPN, e.g., john@contoso.com
            upn = $"{samAccountName}@{domainName}";
        }

        Name = upn;
        DisplayName = GetProfileProperty(user, "displayName");

        // TODO: Sync these properties with names used by SharpHound
        SetProperty("samaccountname", samAccountName);
        SetProperty("userprincipalname", upn);
        SetProperty(ObjectGuidPropertyName, ActiveDirectoryDomain.ParseObjectGuid(user.ExternalId));
        SetProperty(ObjectSidPropertyName, GetProfileProperty(user, "objectSid"));
        SetProperty("distinguishedname", GetProfileProperty(user, "dn"));
        SetProperty("cn", GetProfileProperty(user, "cn"));
        SetProperty("givenname", GetProfileProperty(user, "firstName"));
        SetProperty("sn", GetProfileProperty(user, "lastName"));
        SetProperty("employeeid", GetProfileProperty(user, "employeeID"));
        SetProperty("employeenumber", GetProfileProperty(user, "employeeNumber"));
        SetProperty("primarygroupid", GetProfileProperty(user, "primaryGroupId"));
        SetProperty("department", GetProfileProperty(user, "department"));
        SetProperty("description", GetProfileProperty(user, "description"));
        SetProperty("city", GetProfileProperty(user, "city"));
        SetProperty("title", GetProfileProperty(user, "title"));
        SetProperty("email", GetProfileProperty(user, "email"));
    }

    private static string GetUserId(AppUser user)
    {
        // Try to get the SID. This only works for inbound users.
        string? sid = GetProfileProperty(user, "objectSid");

        // Use objectGuid as a fallback for outbound synced users.
        // TODO: BloodHound currently does not support AD user matching by objectGuid.
        return sid ?? ActiveDirectoryDomain.ParseObjectGuid(user.ExternalId) ?? throw new ArgumentException("The AD account does not have any usable unique identifier.");
    }

    private static string? GetProfileProperty(AppUser user, string propertyName)
    {
        var success = user.Profile.TryGetValue(propertyName, out var valueObj);
        return valueObj as string;
    }
}
