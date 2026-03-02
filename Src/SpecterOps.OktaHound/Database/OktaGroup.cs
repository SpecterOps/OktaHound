using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaGroup : OktaSecurityPrincipal
{
    public const string NodeKind = "Okta_Group";
    public const string MemberOfEdgeKind = "Okta_MemberOf";
    public const string MembershipSyncEdgeKind = "Okta_MembershipSync";
    public const string GroupPushEdgeKind = "Okta_GroupPush";
    public const string GroupPullEdgeKind = "Okta_GroupPull";

    private const string OktaGroupObjectClass = "okta:user_group";
    private const string ActiveDirectoryGroupObjectClass = "okta:windows_security_principal";

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public DateTimeOffset? LastMembershipUpdated { get; set; }
    public string? OktaGroupType { get; set; }
    public string? ObjectClass { get; set; }
    public string? Description { get; set; }
    public string? ObjectSid { get; set; }
    public string? DistinguishedName { get; set; }
    public string? SamAccountName { get; set; }
    public string? DomainQualifiedName { get; set; }
    public string? GroupScope { get; set; }
    public string? GroupType { get; set; }
    public string? ObjectGuid { get; set; }

    [JsonIgnore]
    public string? SourceApplicationId { get; private set; }

    [JsonIgnore]
    public OktaApplication? SourceApplication { get; set; }

    [JsonIgnore]
    public List<OktaIdentityProvider>? GoverningIdentityProviders { get; set; }

    [JsonIgnore]
    public List<OktaUser> Members { get; set; } = [];

    [JsonIgnore]
    [NotMapped]
    public bool MembershipLocked => OktaGroupType == global::Okta.Sdk.Model.GroupType.APPGROUP.Value;

    [JsonIgnore]
    [NotMapped]
    public bool IsActiveDirectoryGroup => ObjectClass == ActiveDirectoryGroupObjectClass;

    [JsonIgnore]
    [NotMapped]
    public bool IsOktaGroup => ObjectClass == OktaGroupObjectClass;

    [JsonIgnore]
    public string? DomainSid { get; private set; }

    protected override string[] Kinds => [NodeKind];

    private OktaGroup() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaGroup(Group group, string domainName) : base(group.Id, ResolveName(group), domainName)
    {
        DisplayName = Name;
        Created = group.Created;
        LastUpdated = group.LastUpdated;
        LastMembershipUpdated = group.LastMembershipUpdated;

        // Types: APP_GROUP, BUILT_IN, OKTA_GROUP
        OktaGroupType = group.Type.Value;

        // Sample objectClass value: okta:windows_security_principal
        ObjectClass = group.ObjectClass?.FirstOrDefault();

        // For synchronized groups (AD or LDAP or SCIM), cache the source app ID for later processing.
        SourceApplicationId = group.Source?.Id;

        // Set profile-specific properties
        if (group.Profile.ActualInstance is OktaUserGroupProfile groupProfile)
        {
            Name = groupProfile.Name;
            DisplayName = groupProfile.Name;
            Description = groupProfile.Description;
        }
        else if (group.Profile.ActualInstance is OktaActiveDirectoryGroupProfile adGroupProfile)
        {
            Name = adGroupProfile.Name;
            DisplayName = adGroupProfile.Name;
            Description = adGroupProfile.Description;
            ObjectSid = adGroupProfile.ObjectSid;
            DistinguishedName = adGroupProfile.Dn;
            SamAccountName = adGroupProfile.SamAccountName;
            DomainQualifiedName = adGroupProfile.WindowsDomainQualifiedName;
            GroupScope = adGroupProfile.GroupScope;
            GroupType = adGroupProfile.GroupType;

            // Cut off the RID from the SID to get the domain SID, e.g., S-1-5-21-2697957641-2271029196-387917394 from S-1-5-21-2697957641-2271029196-387917394-500
            DomainSid = GetDomainSid(ObjectSid);

            // Base-64 encoded GUID (objectGUID) of the Windows group
            ObjectGuid = DecodeObjectGuid(adGroupProfile.ExternalId);
        }
        else
        {
            throw new NotSupportedException($"Unknown profile type encountered on group {group.Id}.");
        }
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaGroup);
    }

    private static string ResolveName(Group group)
    {
        return group.Profile.ActualInstance switch
        {
            OktaUserGroupProfile userGroupProfile => userGroupProfile.Name,
            OktaActiveDirectoryGroupProfile adGroupProfile => adGroupProfile.Name,
            _ => group.Id
        };
    }

    /// <summary>
    /// Decodes a base64-encoded GUID string to its standard string representation.
    /// </summary>
    private static string? DecodeObjectGuid(string? base64Guid)
    {
        if (string.IsNullOrEmpty(base64Guid))
        {
            return null;
        }

        try
        {
            byte[] guidBytes = Convert.FromBase64String(base64Guid);
            Guid guid = new Guid(guidBytes);
            return guid.ToString();
        }
        catch
        {
            // Return the original string if it cannot be parsed for some reason
            return base64Guid;
        }
    }

    private static string? GetDomainSid(string? groupSid)
    {
        if (groupSid is null)
        {
            // This is apparently not an AD group
            return null;
        }

        // Cut off the RID from the SID, e.g., S-1-5-21-2697957641-2271029196-387917394-500
        var ridSeparatorIndex = groupSid.LastIndexOf('-');
        return groupSid[..ridSeparatorIndex];
    }
}
