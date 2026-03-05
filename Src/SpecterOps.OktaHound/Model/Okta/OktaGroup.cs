using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaGroup : OktaSecurityPrincipal
{
    public const string NodeKind = "Okta_Group";
    public const string MemberOfEdgeKind = "Okta_MemberOf";
    public const string MembershipSyncEdgeKind = "Okta_MembershipSync";
    public const string GroupPushEdgeKind = "Okta_GroupPush";
    public const string GroupPullEdgeKind = "Okta_GroupPull";
    private const string DescriptionPropertyName = "description";
    private const string OktaGroupObjectClass = "okta:user_group";
    private const string ActiveDirectoryGroupObjectClass = "okta:windows_security_principal";
    private const string GroupTypePropertyName = "oktaGroupType";
    private const string ObjectSidPropertyName = "objectSid";

    /// <summary>
    /// Indicates whether the group's membership is locked (i.e., managed externally).
    /// </summary>
    [JsonIgnore]
    public bool MembershipLocked => GroupType == global::Okta.Sdk.Model.GroupType.APPGROUP.Value;

    [JsonIgnore]
    public string? SourceApplicationId { get; private set; }

    [JsonIgnore]
    public string? GroupType => GetProperty<string>(GroupTypePropertyName);

    [JsonIgnore]
    public bool IsActiveDirectoryGroup { get; private set; }

    [JsonIgnore]
    public string? ObjectSid => GetProperty<string>(ObjectSidPropertyName);

    [JsonIgnore]
    public string? DomainSid
    {
        get
        {
            string? groupSid = ObjectSid;

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

    [JsonIgnore]
    public string? ObjectGuid
    {
        get
        {
            string? externalId = GetProperty<string>("externalId");

            if (!IsActiveDirectoryGroup || externalId is null)
            {
                // This is not an AD group or the info is incomplete
                return null;
            }

            try
            {
                return new Guid(Convert.FromBase64String(externalId)).ToString();
            }
            catch
            {
                // Do not cause any error if the conversion failed
                return null;
            }
        }
    }

    [JsonIgnore]
    public string? DistinguishedName => GetProperty<string>("dn");

    [JsonIgnore]
    public string? SamAccountName => GetProperty<string>("samAccountName");

    public OktaGroup(Group group, string domainName) : base(group.Id, domainName, NodeKind)
    {
        // Set common group properties
        SetProperty("created", group.Created);
        SetProperty("lastUpdated", group.LastUpdated);
        SetProperty("lastMembershipUpdated", group.LastMembershipUpdated);

        // Types: APP_GROUP, BUILT_IN, OKTA_GROUP
        SetProperty(GroupTypePropertyName, group.Type.Value);

        // Sample objectClass value: okta:windows_security_principal
        SetProperty("objectClass", group.ObjectClass?.FirstOrDefault());

        // For synchronized groups (AD or LDAP or SCIM), cache the source app ID for later processing.
        SourceApplicationId = group.Source?.Id;

        // Set profile-specific properties
        if (group.Profile.ActualInstance is OktaUserGroupProfile groupProfile)
        {
            Name = groupProfile.Name;
            DisplayName = groupProfile.Name;

            SetProperty(DescriptionPropertyName, groupProfile.Description);
        }
        else if (group.Profile.ActualInstance is OktaActiveDirectoryGroupProfile adGroupProfile)
        {
            Name = adGroupProfile.Name;
            DisplayName = adGroupProfile.Name;
            IsActiveDirectoryGroup = true;

            SetProperty(DescriptionPropertyName, adGroupProfile.Description);
            SetProperty(ObjectSidPropertyName, adGroupProfile.ObjectSid);
            SetProperty("distinguishedName", adGroupProfile.Dn);
            SetProperty("samAccountName", adGroupProfile.SamAccountName);
            SetProperty("domainQualifiedName", adGroupProfile.WindowsDomainQualifiedName);
            SetProperty("groupScope", adGroupProfile.GroupScope);
            SetProperty("groupType", adGroupProfile.GroupType);

            // Base-64 encoded GUID (objectGUID) of the Windows group
            SetProperty("objectGuid", DecodeObjectGuid(adGroupProfile.ExternalId));
        }
        else
        {
            throw new NotSupportedException($"Unknown profile type encountered on group {group.Id}.");
        }
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);

    /// <summary>
    /// Creates a node representing an Okta Org2Org group.
    /// </summary>
    public static OpenGraphEdgeNode? CreateEdgeNode(string? name, string? domainName)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(domainName))
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "name", name }, // Example: "Sales Team"
            { "domainName", domainName } // Example: contoso.okta.com
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }

    /// <summary>
    /// Decodes a base64-encoded GUID string to its standard string representation.
    /// </summary>
    private static string DecodeObjectGuid(string base64Guid)
    {
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
}
