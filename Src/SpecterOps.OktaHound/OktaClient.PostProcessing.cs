using Microsoft.Extensions.Logging;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.ActiveDirectory;
using SpecterOps.OktaHound.Model.Okta;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    /// <summary>
    /// Creates the Domain nodes in AD subgraph.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the Okta graph has not been initialized.</exception>
    public void CreateDomainNodes()
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph is null. Ensure data has been collected before post-processing.");
        }

        foreach (var domainApp in _graph.Applications.Where(app => app.IsActiveDirectory))
        {
            string? domainSid = domainApp.ActiveDirectoryDomainSid;
            string? domainName = domainApp.ActiveDirectoryDomain ?? domainApp.Name;

            // Domain SID is inferred from the synchronized user or group objects.
            if (domainSid is not null && domainName is not null)
            {
                _logger.LogDebug("Discovered Active Directory domain {DomainName} ({DomainSid}).", domainName, domainSid);
                ActiveDirectoryDomain domainNode = new(domainSid, domainName);
                _adGraph.AddNode(domainNode);
            }
            else
            {
                _logger.LogWarning("Could not resolve the SID of the AD domain associated with the {AppName} ({AppId}) application. There might be some sync issues.", domainApp.Name, domainApp.Id);
            }
        }
    }

    /// <summary>
    /// Creates the (:Okta_User)-[:Okta_ManagerOf]->(:Okta_User) edges in the Okta graph based on the managerId property of Okta_User nodes.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void CreateManagerEdges()
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph is null. Ensure data has been collected before post-processing.");
        }

        foreach (var node in _graph.Users ?? [])
        {
            OktaUser userNode = (OktaUser)node;

            // Get user's manager (just for displaying the org structure)
            // This assumes that getManagerUser("active_directory").login is mapped to managerId in Okta.
            // Logins must have the UPN/email format, i.e., containing '@'.
            if (userNode.ManagerId is not null && userNode.ManagerId.Contains('@'))
            {
                OktaUser? managerNode = _graph.GetUserByLogin(userNode.ManagerId);

                if (managerNode is null)
                {
                    _logger.LogWarning("Could not find the manager with login {ManagerId} for user {Id}.", userNode.ManagerId, userNode.Id);
                    continue;
                }

                // Create the (:Okta_User)-[:Okta_ManagerOf]->(:Okta_User) edge
                _logger.LogTrace("Setting {ManagerId} as the manager of user {Id}...", userNode.ManagerId, userNode.Id);
                _graph.AddEdge(managerNode, userNode, OktaUser.ManagerOfEdgeKind);
            }
        }
    }

    public void AddPermissionEdges()
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph is null. Ensure data has been collected before post-processing.");
        }

        // Evaluate the RBAC permissions, while taking into account the built-in privilege elevation barriers.
        foreach (var roleAssignment in _graph.RoleAssignments)
        {
            if (roleAssignment.RoleType == RoleType.APPADMIN)
            {
                // If the assignment has no targets, it applies to all apps in the org
                IEnumerable<OktaNode> targetApps = roleAssignment.Targets.Count > 0 ?
                    roleAssignment.Targets :
                    _graph.ApplicationsAndApiServiceIntegrations;

                foreach (var targetApp in targetApps)
                {
                    if (targetApp is OktaApplication)
                    {
                        foreach (var clientSecretNode in _graph.GetClientSecrets(targetApp.Id))
                        {
                            // The permission to read client secrets applies even to privileged apps, but not to API service integrations.
                            // Create the (:Okta)-[:Okta_ReadClientSecret]->(:Okta_ClientSecret) edge
                            _graph.AddEdge(roleAssignment.Assignee, clientSecretNode, OktaCustomRole.ReadClientSecretEdgeKind);
                        }
                    }

                    // App admins can't manage apps with admin privileges.
                    if (!(targetApp is OktaApplication appNode && appNode.HasRoleAssignments))
                    {
                        // Create the (:Okta)-[:Okta_AppAdmin]->(:Okta_Application) edge OR
                        // (:Okta)-[:Okta_AppAdmin]->(:Okta_ApiServiceIntegration) edge
                        _graph.AddEdge(roleAssignment.Assignee, targetApp, OktaBuiltinRole.ApplicationAdministratorEdgeKind);
                    }
                }
            }
            else if (roleAssignment.RoleType == RoleType.USERADMIN)
            {
                // If the assignment has no targets, it applies to all users and groups in the org
                IEnumerable<OktaSecurityPrincipal> targetUsersAndGroups = roleAssignment.Targets.Count > 0 ?
                    roleAssignment.Targets.Cast<OktaSecurityPrincipal>() :
                    _graph.UsersAndGroups;

                // Group admins can't manage users or groups with admin privileges.
                foreach (var targetPrincipal in targetUsersAndGroups.Where(principal => !principal.HasRoleAssignments))
                {
                    // Create the (:Okta)-[:Okta_GroupAdmin]->(:Okta_User) and (:Okta)-[:Okta_GroupAdmin]->(:Okta_Group) edges
                    _graph.AddEdge(roleAssignment.Assignee, targetPrincipal, OktaBuiltinRole.GroupAdministratorEdgeKind);
                }
            }
            else if (roleAssignment.RoleType == RoleType.GROUPMEMBERSHIPADMIN)
            {
                // If the assignment has no targets, it applies to all groups in the org
                IEnumerable<OktaGroup> targetGroups = roleAssignment.Targets.Count > 0 ?
                    roleAssignment.Targets.Cast<OktaGroup>() :
                    _graph.Groups;

                // Group membership admins can't manage groups with admin privileges.
                foreach (var targetGroup in targetGroups.Where(group => !group.HasRoleAssignments))
                {
                    // Create the (:Okta)-[:Okta_GroupMembershipAdmin]->(:Okta_Group) edge
                    _graph.AddEdge(roleAssignment.Assignee, targetGroup, OktaBuiltinRole.GroupMembershipAdministratorEdgeKind);
                }
            }
            else if (roleAssignment.RoleType == RoleType.HELPDESKADMIN)
            {
                // If the assignment has no targets, it applies to all users in the org
                IEnumerable<OktaUser> targetUsers = roleAssignment.Targets.Count > 0 ?
                    roleAssignment.Targets.Cast<OktaUser>() :
                    _graph.Users;

                // Help desk admins can't manage users with admin privileges.
                foreach (var targetUser in targetUsers.Where(user => !user.HasRoleAssignments))
                {
                    // Create the (:Okta)-[:Okta_HelpDeskAdmin]->(:Okta_User) edge
                    _graph.AddEdge(roleAssignment.Assignee, targetUser, OktaBuiltinRole.HelpDeskAdministratorEdgeKind);
                }
            }
            else if (roleAssignment.RoleType == RoleType.ORGADMIN)
            {
                // Org Admin have permissions on users, groups, and devices
                foreach (var targetNode in _graph.UsersAndGroupsAndDevices)
                {
                    if (targetNode is OktaSecurityPrincipal principalNode && principalNode.HasRoleAssignments)
                    {
                        // Org admins can't manage users or groups with admin privileges.
                        continue;
                    }

                    // Create the
                    // (:Okta)-[:Okta_OrgAdmin]->(:Okta_User),
                    // (:Okta)-[:Okta_OrgAdmin]->(:Okta_Group), and
                    // (:Okta)-[:Okta_OrgAdmin]->(:Okta_Device) edges
                    _graph.AddEdge(roleAssignment.Assignee, targetNode, OktaBuiltinRole.OrganizationAdministratorEdgeKind);
                }
            }
            else if (roleAssignment.RoleType == RoleType.MOBILEADMIN)
            {
                // Mobile Admins have permissions on devices
                foreach (var deviceNode in _graph.Devices)
                {
                    // Create the (:Okta)-[:Okta_MobileAdmin]->(:Okta_Device) edge
                    _graph.AddEdge(roleAssignment.Assignee, deviceNode, OktaBuiltinRole.MobileAdministratorEdgeKind);
                }
            }
            else if (roleAssignment.RoleType == RoleType.SUPERADMIN)
            {
                // Create the (:Okta)-[:Okta_SuperAdmin]->(:Okta_Organization) edge
                _graph.AddEdge(roleAssignment.Assignee, _graph.Organization, OktaBuiltinRole.SuperAdministratorEdgeKind);
            }
            else if (roleAssignment.RoleType == RoleType.ACCESSREQUESTSADMIN)
            {
                // TODO: Explore the Access Requests Admin role (Custom IAM role), which seems to be able to manage apps.
            }
            else if (roleAssignment.RoleType == RoleType.ACCESSCERTIFICATIONSADMIN)
            {
                // The okta.governance.accessCertifications.manage permission does not seem to be abusable.
            }
            else if (roleAssignment.RoleType == RoleType.APIACCESSMANAGEMENTADMIN)
            {
                // API Access Management Admins can read client secrets, which could be interesting with privileged apps.
                foreach (var app in _graph.Applications)
                {
                    foreach (var clientSecretNode in _graph.GetClientSecrets(app.Id))
                    {
                        // The permission to read client secrets applies even to privileged apps, but not to API service integrations.
                        // Create the (:Okta)-[:Okta_ReadClientSecret]->(:Okta_ClientSecret) edge
                        _graph.AddEdge(roleAssignment.Assignee, clientSecretNode, OktaCustomRole.ReadClientSecretEdgeKind);
                    }
                }
            }
            else if (roleAssignment.RoleType == RoleType.WORKFLOWSADMIN)
            {
                // TODO: Current API limitations do not allow us to detect any attack paths through the workflows
            }
            else if (roleAssignment.RoleType == RoleType.READONLYADMIN)
            {
                // Read-Only Admins do not have any permissions to modify objects.
                // But they can read client secrets, which could be interesting with privileged apps.
                foreach (var app in _graph.Applications)
                {
                    foreach (var clientSecretNode in _graph.GetClientSecrets(app.Id))
                    {
                        // The permission to read client secrets applies even to privileged apps, but not to API service integrations.
                        // Create the (:Okta)-[:Okta_ReadClientSecret]->(:Okta_ClientSecret) edge
                        _graph.AddEdge(roleAssignment.Assignee, clientSecretNode, OktaCustomRole.ReadClientSecretEdgeKind);
                    }
                }
            }
            else if (roleAssignment.RoleType == RoleType.CUSTOM)
            {
                var permissions = roleAssignment.Role.Permissions;

                if (permissions is null)
                {
                    _logger.LogWarning("Custom role {RoleName} has no permissions. Skipping edge creation for this role.", roleAssignment.Role.Name);
                    continue;
                }

                var resourceSetMembers = roleAssignment.Targets.OfType<OktaResourceSet>().SelectMany(resourceSet => resourceSet.Members);

                if (!resourceSetMembers.Any())
                {
                    _logger.LogDebug("Custom role {RoleName} has no members in its target resource sets. Skipping edge creation for this role.", roleAssignment.Role.Name);
                    continue;
                }

                // Handle user permissions
                if (permissions.Contains("okta.users.manage") ||
                    permissions.Contains("okta.users.credentials.manage") ||
                    permissions.Contains("okta.users.credentials.manageTemporaryAccessCode") ||
                    permissions.Contains("okta.users.credentials.resetFactors") ||
                    permissions.Contains("okta.users.credentials.resetPassword") ||
                    permissions.Contains("okta.users.credentials.expirePassword"))
                {
                    // Users with assigned roles can't be managed by custom roles.
                    foreach (var targetUser in resourceSetMembers.OfType<OktaUser>().Where(user => !user.HasRoleAssignments))
                    {
                        if (permissions.Contains("okta.users.manage") ||
                            permissions.Contains("okta.users.credentials.manage") ||
                            permissions.Contains("okta.users.credentials.manageTemporaryAccessCode") ||
                            permissions.Contains("okta.users.credentials.resetPassword") ||
                            permissions.Contains("okta.users.credentials.expirePassword"))
                        {
                            // Create the (:Okta)-[:Okta_ResetPassword]->(:Okta_User) edge
                            // TODO: Introduce a standalone permission for manageTemporaryAccessCode
                            _graph.AddEdge(roleAssignment.Assignee, targetUser, OktaCustomRole.ResetPasswordEdgeKind);
                        }

                        if (permissions.Contains("okta.users.manage") ||
                            permissions.Contains("okta.users.credentials.manage") ||
                            permissions.Contains("okta.users.credentials.resetFactors"))
                        {
                            // Create the (:Okta)-[:Okta_ResetFactors]->(:Okta_User) edge
                            _graph.AddEdge(roleAssignment.Assignee, targetUser, OktaCustomRole.ResetFactorsEdgeKind);
                        }
                    }
                }

                // Handle group permissions
                if (permissions.Contains("okta.groups.manage") ||
                    permissions.Contains("okta.groups.members.manage"))
                {
                    // Groups with assigned roles can't be managed by custom roles.
                    foreach (var targetGroup in resourceSetMembers.OfType<OktaGroup>().Where(group => !group.HasRoleAssignments))
                    {
                        // Create the (:Okta)-[:Okta_AddMember]->(:Okta_Group) edge
                        _graph.AddEdge(roleAssignment.Assignee, targetGroup, OktaCustomRole.AddMemberEdgeKind);
                    }
                }

                // Handle app permissions.
                if (permissions.Contains("okta.apps.manage"))
                {
                    // Apps with assigned roles can't be managed by custom roles.
                    foreach (var targetApp in resourceSetMembers.OfType<OktaApplication>().Where(app => !app.HasRoleAssignments))
                    {
                        // Create the (:Okta)-[:Okta_ManageApp]->(:Okta_Application) edge
                        _graph.AddEdge(roleAssignment.Assignee, targetApp, OktaCustomRole.ManageAppEdgeKind);
                    }
                }

                if (permissions.Contains("okta.apps.clientCredentials.read"))
                {
                    foreach (var targetApp in resourceSetMembers.OfType<OktaApplication>())
                    {
                        foreach (var clientSecretNode in _graph.GetClientSecrets(targetApp.Id))
                        {
                            // The permission to read client secrets applies even to privileged apps, but not to API service integrations.
                            // Create the (:Okta)-[:Okta_ReadClientSecret]->(:Okta_ClientSecret) edge
                            _graph.AddEdge(roleAssignment.Assignee, clientSecretNode, OktaCustomRole.ReadClientSecretEdgeKind);
                        }
                    }
                }

                // TODO: Handle the Okta_AddSelf edge
                // TODO: Handle additional custom role permissions
            }
        }

        // TODO: Handle permissions of API Service Integrations
        // TODO: Limit app permissions by grant types
        // TODO: Remove some permissions on synchronized users and groups, as they are managed externally
    }
}
