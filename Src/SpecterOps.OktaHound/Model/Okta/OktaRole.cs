using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaRole : OktaNode
{
    public const string NodeKind = "Okta_Role";
    public const string ApplicationAdministratorEdgeKind = "Okta_AppAdmin";
    public const string GroupMembershipAdministratorEdgeKind = "Okta_GroupMembershipAdmin";
    public const string GroupAdministratorEdgeKind = "Okta_GroupAdmin";
    public const string SuperAdministratorEdgeKind = "Okta_SuperAdmin";
    public const string MobileAdministratorEdgeKind = "Okta_MobileAdmin";
    public const string OrganizationAdministratorEdgeKind = "Okta_OrgAdmin";
    public const string HelpDeskAdministratorEdgeKind = "Okta_HelpDeskAdmin";
    public const string HasRoleEdgeKind = "Okta_HasRole";

    /// <summary>
    /// Represents a collection of all Okta built-in role identifiers.
    /// </summary>
    /// <remarks>
    /// This array includes roles defined in the <see cref="RoleType"/> enumeration as well as
    /// additional roles that are not part of the enumeration. It is important to keep this list updated with any new
    /// built-in roles that are introduced to ensure comprehensive role management.
    /// </remarks>
    public static readonly string[] BuiltInRoles = [
        RoleType.APIACCESSMANAGEMENTADMIN.Value,
        RoleType.APPADMIN.Value,
        RoleType.GROUPMEMBERSHIPADMIN.Value,
        RoleType.HELPDESKADMIN.Value,
        RoleType.MOBILEADMIN.Value,
        RoleType.ORGADMIN.Value,
        RoleType.READONLYADMIN.Value,
        RoleType.REPORTADMIN.Value,
        RoleType.SUPERADMIN.Value,
        RoleType.USERADMIN.Value, // USER_Admin counter-intuitively maps to Group Administrator
        RoleType.APIADMIN.Value,
        RoleType.ACCESSCERTIFICATIONSADMIN.Value,
        RoleType.ACCESSREQUESTSADMIN.Value,
        RoleType.WORKFLOWSADMIN.Value
    ];

    public OktaRole(IamRole role, string domainName) : base(MakeRoleIdUnique(role.Id, domainName), domainName, NodeKind)
    {
        if (role.Id == RoleType.CUSTOM)
        {
            throw new ArgumentException("Use the OktaCustomRole class for custom roles.");
        }

        Name = role.Label;
        DisplayName = role.Label;

        // Most built-in roles do not have descriptions, with the exception of WORKFLOWS_ADMIN.
        SetProperty("description", role.Description);
    }

    public static OpenGraphEdgeNode CreateEdgeNode(StandardRole roleAssignment, string domainName)
    {
        var roleId = MakeRoleIdUnique(roleAssignment.Type.Value, domainName);
        return CreateEdgeNode(roleId);
    }

    public static string MakeRoleIdUnique(string roleId, string domainName)
    {
        // Add domain name suffix to built-in roles to avoid conflicts
        return $"{roleId}@{domainName}";
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
