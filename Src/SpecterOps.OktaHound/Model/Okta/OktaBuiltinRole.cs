using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaBuiltinRole : OktaRole
{
    [JsonIgnore]
    private readonly string _originalId;

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
    /// The original identifier of the built-in role as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public override string OriginalId => _originalId;
    
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

    public override bool IsBuiltIn => true;

    public OktaBuiltinRole(IamRole role, string domainName) : base(MakeRoleIdUnique(role.Id, domainName), domainName, NodeKind)
    {
        if (role.Id == RoleType.CUSTOM)
        {
            throw new ArgumentException("Use the OktaCustomRole class for custom roles.");
        }

        _originalId = role.Id;
        Name = role.Label;
        DisplayName = role.Label;

        // Most built-in roles do not have descriptions, with the exception of WORKFLOWS_ADMIN.
        SetProperty("description", role.Description);

        // Derive built-in role permissions based on role type since Okta API does not return permissions for built-in roles
        PopulatePermissions();
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

    // TODO: Validate the permissions for each built-in role against the actual state,
    // as the exact mapping of built-in roles to permissions is not officially documented by Okta and may be subject to change.
    private void PopulatePermissions()
    {
        if (OriginalId == RoleType.APIACCESSMANAGEMENTADMIN.Value)
        {
            Permissions = [
                // API Access Management Administrator
                "okta.authzServers.manage", // Add/delete/edit authorization server scopes, claims, and policies
                "okta.authzServers.read", // View authorization server scopes, claims, and policies
                "okta.profileMappings.manage", // Manage Profile Editor and profile mappings
                "okta.profileMappings.read", // View profile mappings/profile editor configuration
                "okta.users.read", // View users
                "okta.userTypes.read", // View user types
                "okta.groups.read", // View groups
                "okta.apps.manage", // Add/configure applications (API Access Mgmt Admin app-management scope applies to OIDC client apps)
                "okta.apps.read", // View applications/application instances (OIDC app scope for this role)
                "okta.apps.assignment.manage", // Assign user access to applications (OIDC app scope for this role)
                "okta.users.appAssignment.manage", // Manage direct user-to-app assignments (OIDC app scope for this role)
                "okta.groups.appAssignment.manage", // Manage group-to-app assignments (OIDC app scope for this role)
                "okta.apps.clientCredentials.read", // Read-only access to OAuth clients through the API
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.APPADMIN.Value)
        {
            Permissions = [
                // Application Administrator
                "okta.apps.manage", // Add/configure applications; create/modify OIDC apps (can be restricted to OIDC client apps)
                "okta.apps.read", // View applications/application instances (includes Mobile tab visibility in Admin Console)
                "okta.apps.assignment.manage", // Assign user/group access to applications
                "okta.apps.clientCredentials.read", // Read-only access to OAuth clients through the API
                "okta.users.appAssignment.manage", // Manage direct user-to-app assignments
                "okta.groups.appAssignment.manage", // Manage group-to-app assignments (separate from group membership management)
                "okta.policies.manage", // Add/update/delete app sign-on policies and rules (limitation: app sign-on policies scope)
                "okta.policies.read", // View app sign-on policies and rules
                "okta.users.read", // View users
                "okta.groups.read", // View groups
                "okta.users.userprofile.manage", // Edit profiles (limitation: applies on import where profile source isn't configured)
                "okta.users.userprofile.read", // View user profile attributes used in profile editing/mappings
                "okta.profilesources.import.run", // Create users in staged status through app import
                "okta.agents.register", // Authorize/register RADIUS Agent
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.GROUPMEMBERSHIPADMIN.Value)
        {
            Permissions = [
                // Group Membership Administrator
                "okta.users.read", // View users
                "okta.groups.read", // View groups
                "okta.groups.members.manage", // Add users to groups / remove users from groups
                "okta.users.groupMembership.manage", // Manage user group membership relationships
                "okta.groups.manage", // Manage group rules
                "okta.users.apitokens.read", // View user tokens (limitation: self only)
                "okta.users.apitokens.manage", // Create/manage user tokens (limitation: self only)
                "okta.users.apitokens.clear", // Clear user tokens (limitation: self only)
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.HELPDESKADMIN.Value)
        {
            Permissions = [
                // Help Desk Administrator
                "okta.users.read", // View users (limitation: only within managed groups)
                "okta.users.lifecycle.clearSessions", // Clear user sessions (limitation: scoped to managed groups; can include super admins)
                "okta.users.credentials.resetPassword", // Password resets (limitation: only within managed groups)
                "okta.users.credentials.resetFactors", // MFA resets (limitation: only within managed groups)
                "okta.users.risk.manage", // Reset user behavior profile (limitation: only within managed groups)
                "okta.groups.read", // View groups (limitation: only managed groups)
                "okta.users.apitokens.clear", // Clear user tokens (limitation: self and scoped members)
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.MOBILEADMIN.Value)
        {
            Permissions = [
                // Mobile Administrator
                "okta.devices.read", // View/manage device details and Mobile tab device visibility
                "okta.devices.manage", // Manage devices (deprovision/clear/remote lock/reset)
                "okta.devices.lifecycle.manage", // Device lifecycle actions from device/mobile views
                "okta.policies.read", // View mobile/sign-on/wifi policies and rules
                "okta.policies.manage", // Add/update/delete policies/rules; reorder policy priority; edit MFA factors in policies
                "okta.users.read", // View users (includes mobile tab visibility in users context)
                "okta.groups.read", // View groups
                "okta.apps.read", // View applications/application instances (includes Mobile tab on apps)
                "okta.logs.read", // View logs / System Log (admin activity visibility includes super-admin related events)
                "okta.users.apitokens.read", // View user tokens
                "okta.agents.register", // Authorize/register RADIUS Agent
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.ORGADMIN.Value)
        {
            Permissions = [
                // Organization Administrator
                "okta.reports.read", // View and run reports
                "okta.customizations.read", // View Okta settings (themes/logo/contact)
                "okta.customizations.manage", // Edit Okta settings/customizations
                "okta.logs.read", // View System Log
                "okta.authzServers.read", // View authorization server scopes/claims/policies
                "okta.authzServers.manage", // Add/delete/edit authorization server scopes/claims/policies
                "okta.users.read", // View users
                "okta.users.create", // Create users
                "okta.users.manage", // Change user types and user management operations
                "okta.users.lifecycle.manage", // User lifecycle controls (activate/deactivate/suspend/etc.)
                "okta.users.lifecycle.clearSessions", // Sign out/clear user sessions
                "okta.users.credentials.manage", // Password resets/MFA resets
                "okta.users.risk.manage", // Reset/view user behavior profile operations
                "okta.groups.read", // View groups
                "okta.groups.create", // Create groups
                "okta.groups.manage", // Manage groups and group rules
                "okta.groups.members.manage", // Add/remove users in groups
                "okta.apps.read", // View apps/application instances
                "okta.apps.manage", // Add/configure applications
                "okta.apps.assignment.manage", // Assign app access
                "okta.users.appAssignment.manage", // Manage user app assignments
                "okta.groups.appAssignment.manage", // Manage group app assignments
                "okta.apps.clientCredentials.read", // Read-only access to OAuth clients through API
                "okta.policies.read", // View sign-on/mobile/wifi policies and rules
                "okta.policies.manage", // Add/update/delete policies/rules; prioritization controls
                "okta.devices.read", // View/manage device details visibility
                "okta.devices.manage", // Manage devices
                "okta.eventhooks.read", // View hooks (event hooks)
                "okta.eventhooks.manage", // Create/configure hooks (event hooks)
                "okta.inlinehooks.read", // View hooks (inline hooks)
                "okta.inlinehooks.manage", // Create/configure hooks (inline hooks)
                "okta.identityProviders.read", // View identity providers
                "okta.identityProviders.manage", // Manage identity providers (including social IdPs)
                "okta.agents.register", // Authorize/register RADIUS Agent
                "okta.users.apitokens.manage", // Create/manage user tokens (self-scoped by role behavior)
                "okta.users.apitokens.read", // View user tokens
                "okta.users.apitokens.clear", // Clear user tokens
                "okta.realms.read", // View realms designation; setup workflow with realms
                "okta.realms.manage", // Create/update/delete realms and realm assignments
                "okta.workflows.read", // Workflow visibility used with realms setup
                "okta.support.cases.manage" // Grant/manage Okta Support access/cases
            ];
        }
        else if (OriginalId == RoleType.READONLYADMIN.Value)
        {
            Permissions = [
                // Read-only Administrator
                "okta.reports.read", // View and run reports
                "okta.customizations.read", // View Okta settings (themes, logo, contact info)
                "okta.authzServers.read", // View authorization server scopes, claims, and policies
                "okta.logs.read", // View System Log (system events)
                "okta.devices.read", // View device details / Device Trust visibility / mobile tab details
                "okta.users.read", // View users
                "okta.users.risk.read", // View user behavior profile and org behavior / ThreatInsight-related risk visibility
                "okta.userTypes.read", // View user types
                "okta.groups.read", // View groups
                "okta.apps.read", // View applications/application instances (includes Mobile tab on apps)
                "okta.policies.read", // View mobile/sign-on/wifi policies and rules
                "okta.eventhooks.read", // View hooks (event hooks)
                "okta.inlinehooks.read", // View hooks (inline hooks)
                "okta.agents.register", // Authorize/register RADIUS Agent
                "okta.users.apitokens.manage", // Create/manage user tokens (limitation: self only)
                "okta.users.apitokens.read", // View user tokens (limitation: self only)
                "okta.users.apitokens.clear", // Clear user tokens (limitation: self only)
                "okta.apps.clientCredentials.read", // Read-only access to OAuth clients through the API
                "okta.workflows.read", // View workflows (used for setting up workflow visibility with realms)
                "okta.realms.read" // View realms designation
            ];
        }
        else if (OriginalId == RoleType.REPORTADMIN.Value)
        {
            Permissions = [
                // Report Administrator
                "okta.reports.read", // View and run reports
                "okta.logs.read" // View System Log (system events)
            ];
        }
        else if (OriginalId == RoleType.SUPERADMIN.Value)
        {
            Permissions = [
                // Super Administrator has full org-wide administrative control.
                "*"
            ];
        }
        else if (OriginalId == RoleType.USERADMIN.Value)
        {
            Permissions = [
                // Group Administrator (USER_ADMIN)
                "okta.users.read", // View users (limitation: only within managed groups)
                "okta.users.create", // Create users (limitation: only within managed groups)
                "okta.users.lifecycle.delete", // Delete users (limitation: only within managed groups)
                "okta.users.lifecycle.suspend", // Suspend users (limitation: only within managed groups; can include super admins)
                "okta.users.lifecycle.deactivate", // Deactivate users (limitation: only within managed groups)
                "okta.users.lifecycle.activate", // Activate users (limitation: only within managed groups; can include super admins)
                "okta.users.manage", // Change user types / broad user management (limitation: only within managed groups)
                "okta.users.lifecycle.clearSessions", // Sign out users / clear user sessions (limitation: only within managed groups; can include super admins)
                "okta.users.userprofile.manage", // Edit profiles (limitation: only within managed groups)
                "okta.users.credentials.resetPassword", // Password resets (limitation: only within managed groups)
                "okta.users.credentials.resetFactors", // MFA resets (limitation: only within managed groups)
                "okta.users.risk.manage", // Reset user behavior profile (limitation: only within managed groups)
                "okta.groups.read", // View groups (limitation: only within managed groups)
                "okta.groups.members.manage", // Add/remove users in groups (limitation: only groups admin manages; can include super admins)
                "okta.users.groupMembership.manage", // Move users between managed groups / membership management
                "okta.groups.manage" // Manage group rules (limitation: applies only if admin has access to all users and groups)
            ];
        }
        else if (OriginalId == RoleType.APIADMIN.Value)
        {
            // This role is undocumented
            Permissions = [
                "okta.users.apitokens.manage",
                "okta.users.apitokens.read",
                "okta.users.apitokens.clear"
            ];
        }
        else if (OriginalId == RoleType.ACCESSCERTIFICATIONSADMIN.Value)
        {
            Permissions = [
                // Role: ACCESS_CERTIFICATIONS_ADMIN (Access Certifications Administrator)
                // Resource set: ACCESS_CERTIFICATIONS_IAM_POLICY
                "okta.governance.accessCertifications.manage", // Manage access certifications
                "okta.governance.collections.read", // Read governance collections
                "okta.apps.read", // Read applications
                "okta.users.read", // Read users
                "okta.groups.read" // Read groups
            ];
        }
        else if (OriginalId == RoleType.ACCESSREQUESTSADMIN.Value)
        {
            Permissions = [
                // Role: ACCESS_REQUESTS_ADMIN (Access Requests Administrator)
                // Resource set: ACCESS_REQUESTS_IAM_POLICY
                "okta.governance.accessRequests.manage", // Manage access requests
                "okta.apps.assignment.manage", // Manage app assignments
                "okta.governance.collections.read", // Read governance collections
                "okta.groups.appAssignment.manage", // Manage group app assignments
                "okta.apps.manageFirstPartyApps", // Manage first-party app configuration for access requests
                "okta.apps.manage", // Manage applications
                "okta.users.appAssignment.manage" // Manage user app assignments
            ];
        }
        else if (OriginalId == RoleType.WORKFLOWSADMIN.Value)
        {
            Permissions = [
                // Role: WORKFLOWS_ADMIN (Workflows Administrator)
                // Resource set: WORKFLOWS_IAM_POLICY
                "okta.workflows.flows.invoke", // Invoke workflows
                "okta.workflows.flows.manage", // Manage workflows
                "okta.workflows.flows.folders.manage", // Manage workflow folders
                "okta.workflows.tables.manage", // Manage workflow tables
                "okta.workflows.connections.manage", // Manage workflow connections
                "okta.workflows.connectors.manage" // Manage workflow connectors
            ];
        }
        else
        {
            // Unsupported role type - assign empty permissions to avoid null reference issues
            Permissions = [];
        }
    }
}
