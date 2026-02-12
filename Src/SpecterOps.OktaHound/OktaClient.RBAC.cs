using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task FetchOktaUserRoleAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user to role assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.Users.Where(user => user.HasRoleAssignments), concurrency, async (userNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching role assignments for user {UserName} ({UserId})...", userNode.Name, userNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                RoleAssignmentAUserApi roleAssignmentApi = new(_oktaConfig);

                await foreach (var roleAssignment in roleAssignmentApi.ListAssignedRolesForUser(userNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    if (roleAssignment.ActualInstance is StandardRole builtInRoleAssignment)
                    {
                        if (builtInRoleAssignment.AssignmentType == RoleAssignmentType.USER)
                        {
                            // The built-in role is assigned directly to the user, as opposed to a group they are a member of.
                            await ProcessRoleAssignment(builtInRoleAssignment, userNode, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.LogTrace(
                                "User {UserName} ({UserId}) is assigned the {RoleLabel} role via group membership.",
                                userNode.Name,
                                userNode.Id,
                                builtInRoleAssignment.Label);
                        }
                    }
                    else if (roleAssignment.ActualInstance is CustomRole customRoleAssignment)
                    {
                        if (customRoleAssignment.AssignmentType == RoleAssignmentType.USER)
                        {
                            // The role is assigned directly to the user, as opposed to a group they are a member of.
                            ProcessRoleAssignment(customRoleAssignment, userNode);
                        }
                        else
                        {
                            _logger.LogTrace(
                                "User {UserName} ({UserId}) is assigned the {RoleLabel} role via group membership.",
                                userNode.Name,
                                userNode.Id,
                                customRoleAssignment.Label);
                        }
                    }
                    else
                    {
                        // This should not happen, as only 2 role types exist in Okta.
                        _logger.LogError("Unknown role type assigned to user {UserName} ({UserId}). Skipping.",
                            userNode.Name,
                            userNode.Id);
                    }
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch user {UserName} ({UserId}) role assignments.", e.ErrorCode, status, userNode.Name, userNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching user to role assignments.");
    }

    public async Task FetchOktaGroupRoleAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching group role assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.Groups, concurrency, async (groupNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching role assignments for group {GroupName} ({GroupId})...", groupNode.Name, groupNode.Id);
                bool hasRoleAssignments = false;

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                RoleAssignmentBGroupApi roleAssignmentApi = new(_oktaConfig);

                await foreach (var roleAssignment in roleAssignmentApi.ListGroupAssignedRoles(groupNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    hasRoleAssignments = true;

                    if (roleAssignment.ActualInstance is StandardRole builtInRoleAssignment)
                    {
                        await ProcessRoleAssignment(builtInRoleAssignment, groupNode, cancellationToken).ConfigureAwait(false);
                    }
                    else if (roleAssignment.ActualInstance is CustomRole customRoleAssignment)
                    {
                        ProcessRoleAssignment(customRoleAssignment, groupNode);
                    }
                    else
                    {
                        _logger.LogError(
                            "Unknown role assignment type for group {GroupName} ({GroupId}). Skipping.",
                            groupNode.Name,
                            groupNode.Id);
                    }
                }

                groupNode.HasRoleAssignments = hasRoleAssignments;
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch group {GroupName} ({GroupId}) role assignments.", e.ErrorCode, status, groupNode.Name, groupNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching group role assignments...");
    }

    public async Task FetchOktaAppRoleAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application role assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            // The "/oauth2/v1/clients/*" endpoint has a much lower rate limit than other APIs, so we do not parallelize the calls.
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken
        };

        // Only service applications can have roles assigned to them
        await Parallel.ForEachAsync(_graph.Applications.Where(app => app.IsService), concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching role assignments for application {AppName} ({AppId})...", appNode.Name, appNode.Id);
                bool hasRoleAssignments = false;
                RoleAssignmentClientApi assignmentsApi = new(_oktaConfig);

                await foreach (var roleAssignment in assignmentsApi.ListRolesForClient(appNode.Id, cancellationToken).ConfigureAwait(false))
                {
                    hasRoleAssignments = true;

                    if (roleAssignment.ActualInstance is StandardRole builtInRoleAssignment)
                    {
                        await ProcessRoleAssignment(builtInRoleAssignment, appNode, cancellationToken).ConfigureAwait(false);
                    }
                    else if (roleAssignment.ActualInstance is CustomRole customRoleAssignment)
                    {
                        ProcessRoleAssignment(customRoleAssignment, appNode);
                    }
                    else
                    {
                        _logger.LogError(
                            "Unknown role assignment type for application {AppName} ({AppId}). Skipping.",
                            appNode.Name,
                            appNode.Id);
                    }
                }

                appNode.HasRoleAssignments = hasRoleAssignments;
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                // Ignore error 404, which is returned for some applications without any role assignments.
                if (status != HttpStatusCode.NotFound)
                {
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch application {AppName} ({AppId}) role assignments.", e.ErrorCode, status, appNode.Name, appNode.Id);
                }
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application role assignments.");
    }

    public async Task ProcessRoleAssignment(StandardRole roleAssignment, OktaSecurityPrincipalNode assignee, CancellationToken cancellationToken = default)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        // Change "USER" to "User", "GROUP" to "Group", and CLIENT to Client.
        string assigneeType = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(roleAssignment.AssignmentType.Value.ToLowerInvariant());

        _logger.LogTrace(
            "{AssigneeType} {AssigneeName} ({AssigneeId}) to role {Role} assignment is {Status}.",
            assigneeType,
            assignee.Name,
            assignee.Id,
            roleAssignment.Label,
            roleAssignment.Status.Value.ToLowerInvariant());

        if (roleAssignment.Status != LifecycleStatus.ACTIVE)
        {
            // Only process active assignments
            return;
        }

        // Handle built-in roles that target either the entire organization or individual groups or apps.

        // Create the (Okta_Base)-[:Okta_HasRole]->(:Okta_Role) edge
        var roleNode = _graph.GetBuiltInRole(roleAssignment.Type) ?? throw new InvalidOperationException($"Unknown role {roleAssignment.Type.Value}");
        _graph.AddEdge(assignee, roleNode, OktaRole.HasRoleEdgeKind);

        // Create the OktaRoleAssignment node
        var roleAssignmentNode = new OktaRoleAssignment(roleAssignment, roleNode, assignee, _graph.Organization.DomainName);
        _graph.AddNode(roleAssignmentNode);

        // Create the (:Okta_Base)-[:Okta_HasRoleAssignment]->(:Okta_RoleAssignment) edge
        _graph.AddEdge(assignee, roleAssignmentNode, OktaRoleAssignment.HasRoleAssignmentEdgeKind);

        if (roleAssignment.Type == RoleType.APPADMIN)
        {
            // The Application Administrator role assignments can target individual applications.
            List<OktaNode> targetAppNodes = await FetchRoleAssignmentAppTargets(roleAssignment, assignee.Id, cancellationToken).ConfigureAwait(false);

            if (targetAppNodes.Count == 0)
            {
                // No applications were specifically targeted, so the role applies to all applications.
                _logger.LogTrace("The Application Administrator role targets all applications.");

                // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
                _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
            }
            else
            {
                // Handle specific applications in the scope
                foreach (var appNode in targetAppNodes)
                {
                    _logger.LogTrace(
                        "The Application Administrator role targets the {AppName} ({AppId}) app.",
                        appNode.Name,
                        appNode.Id);

                    // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Application) edge OR
                    // (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_ApiServiceIntegration) edge
                    _graph.AddEdge(roleAssignmentNode, appNode, OktaRoleAssignment.ScopedToEdgeKind);
                }

                // Cache the app targets for post-processing
                roleAssignmentNode.Targets.AddRange(targetAppNodes);
            }
        }
        else if (roleAssignment.Type == RoleType.USERADMIN)
        {
            // The Group Administrator role can target individual groups.
            List<OktaGroup> targetGroups = await FetchRoleAssignmentGroupTargets(roleAssignment, assignee.Id, cancellationToken).ConfigureAwait(false);

            // Group admins can't manage groups that have admin roles assigned to them.

            if (targetGroups.Count == 0)
            {
                // No groups were specifically targeted, so the role applies to all groups.
                _logger.LogTrace("The Group Administrator role targets all groups.");

                // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
                _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
            }
            else
            {
                // Handle specific groups in the scope
                foreach (var targetGroup in targetGroups)
                {
                    _logger.LogTrace(
                        "The Group Administrator role targets group {GroupName} ({GroupId}).",
                        targetGroup.Name,
                        targetGroup.Id);

                    // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Group) edge
                    var groupNode = OktaGroup.CreateEdgeNode(targetGroup.Id);
                    _graph.AddEdge(roleAssignmentNode, groupNode, OktaRoleAssignment.ScopedToEdgeKind);
                }

                // Cache the group targets for post-processing
                roleAssignmentNode.Targets.AddRange(targetGroups);

                // Cache group members for post-processing
                string[] targetGroupIds = [.. targetGroups.Select(group => group.Id)];
                roleAssignmentNode.Targets.AddRange(_graph.GetGroupMembers(targetGroupIds));
            }
        }
        else if (roleAssignment.Type == RoleType.GROUPMEMBERSHIPADMIN)
        {
            // The Group Membership role can target individual groups.
            List<OktaGroup> targetGroups = await FetchRoleAssignmentGroupTargets(roleAssignment, assignee.Id, cancellationToken).ConfigureAwait(false);

            if (targetGroups.Count == 0)
            {
                // No groups were specifically targeted, so the role applies to all groups.
                _logger.LogTrace("The Group Membership Administrator role targets all groups.");

                // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
                _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
            }
            else
            {
                // Handle specific groups in the scope
                foreach (var targetGroup in targetGroups)
                {
                    _logger.LogTrace(
                        "The Group Membership Administrator role targets group {GroupName} ({GroupId}).",
                        targetGroup.Name,
                        targetGroup.Id);

                    // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Group) edge
                    var groupNode = OktaApplication.CreateEdgeNode(targetGroup.Id);
                    _graph.AddEdge(roleAssignmentNode, groupNode, OktaRoleAssignment.ScopedToEdgeKind);
                }

                // Cache the group targets for post-processing
                roleAssignmentNode.Targets.AddRange(targetGroups);
            }
        }
        else if (roleAssignment.Type == RoleType.HELPDESKADMIN)
        {
            // The Help Desk Administrator role can target individual groups.
            List<OktaGroup> targetGroups = await FetchRoleAssignmentGroupTargets(roleAssignment, assignee.Id, cancellationToken).ConfigureAwait(false);

            if (targetGroups.Count == 0)
            {
                // No groups were specifically targeted, so the role applies to all users.
                _logger.LogTrace("The Help Desk Administrator role targets all users.");

                // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
                _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
            }
            else
            {
                // Handle specific groups in the scope
                foreach (var targetGroup in targetGroups)
                {
                    _logger.LogTrace(
                            "The Help Desk Administrator role targets group {GroupName} ({GroupId}).",
                            targetGroup.Name,
                            targetGroup.Id);

                    // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Group) edge
                    var groupNode = OktaApplication.CreateEdgeNode(targetGroup.Id);
                    _graph.AddEdge(roleAssignmentNode, groupNode, OktaRoleAssignment.ScopedToEdgeKind);
                }

                // Resolve group membership
                string[] targetGroupIds = [.. targetGroups.Select(group => group.Id)];

                // Cache the list of target group members for post-processing
                roleAssignmentNode.Targets.AddRange(_graph.GetGroupMembers(targetGroupIds));
            }
        }
        else if (roleAssignment.Type == RoleType.SUPERADMIN)
        {
            // The Super Administrator role targets the entire organization
            assignee.IsSuperAdmin = true;

            // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
            _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
        }
        else if (roleAssignment.Type == RoleType.ORGADMIN)
        {
            // The Org Administrator role targets the entire organization, with the exception of privileged entities
            assignee.IsOrgAdmin = true;

            // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
            _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
        }
        else if (roleAssignment.Type == RoleType.MOBILEADMIN)
        {
            // The Mobile Administrator role targets the entire organization

            // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
            _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
        }
        else if (roleAssignment.Type == RoleType.WORKFLOWSADMIN)
        {
            // The Workflows Administrator role always targets the built-in Workflows Resource Set.
            // The corresponding edge is created when fetching resource sets.
        }
        else
        {
            // The remaining built-in roles target the entire organization
            // We can ignore permissions of the Read-only Admins and Report Admins roles.

            // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization) edge
            _graph.AddEdge(roleAssignmentNode, _graph.Organization, OktaRoleAssignment.ScopedToEdgeKind);
        }
    }

    public void ProcessRoleAssignment(CustomRole roleAssignment, OktaSecurityPrincipalNode assignee)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        // Change "USER" to "User", "GROUP" to "Group", and CLIENT to Client.
        string assigneeType = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(roleAssignment.AssignmentType.Value.ToLowerInvariant());

        _logger.LogTrace(
            "{AssigneeType} {AssigneeName} ({AssigneeId}) to role {Role} assignment is {Status}.",
            assigneeType,
            assignee.Name,
            assignee.Id,
            roleAssignment.Label,
            roleAssignment.Status.Value.ToLowerInvariant());

        if (roleAssignment.Status != LifecycleStatus.ACTIVE)
        {
            // Only process active assignments
            return;
        }

        // Handle custom roles that target resource sets

        // Translate custom role label to ID
        // Note that a couple of built-in roles are considered custom by the API,
        // including WORKFLOWS_ADMIN, ACCESS_CERTIFICATIONS_ADMIN, and ACCESS_REQUESTS_ADMIN.
        OktaNode? customRoleNode = roleAssignment.Type == RoleType.CUSTOM ?
            _graph.GetCustomRole(roleAssignment.Label) :
            _graph.GetBuiltInRole((RoleType)roleAssignment.Role);

        if (customRoleNode is null)
        {
            _logger.LogError(
                "Could not find a matching {RoleLabel} role node for the custom role assignment. Skipping.",
                roleAssignment.Label);
            return;
        }

        // Create the (Okta_Base)-[:Okta_HasRole]->(:Okta_CustomRole) edge
        _graph.AddEdge(assignee, customRoleNode, OktaCustomRole.HasCustomRoleEdgeKind);

        // Create the OktaRoleAssignment node
        OktaRoleAssignment roleAssignmentNode = new(roleAssignment, customRoleNode, assignee, _graph.Organization.DomainName);
        _graph.AddNode(roleAssignmentNode);

        // Create the (:Okta_Base)-[:Okta_HasRoleAssignment]->(:Okta_RoleAssignment) edge
        _graph.AddEdge(assignee, roleAssignmentNode, OktaRoleAssignment.HasRoleAssignmentEdgeKind);

        // The OktaScopedTo edge is created when fetching resource sets.
    }

    public async Task<List<OktaNode>> FetchRoleAssignmentAppTargets(StandardRole roleAssignment, string assigneeId, CancellationToken cancellationToken = default)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        // Prepare empty result list
        List<CatalogApplication> targetApps = [];

        try
        {
            RoleBTargetAdminApi roleTargetUserApi = new(_oktaConfig);
            RoleBTargetBGroupApi roleTargetGroupApi = new(_oktaConfig);
            RoleBTargetClientApi roleTargetAppApi = new(_oktaConfig);

            if (roleAssignment.AssignmentType == RoleAssignmentType.USER)
            {
                await foreach (var targetApp in roleTargetUserApi.ListApplicationTargetsForApplicationAdministratorRoleForUser(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetApps.Add(targetApp);
                }
            }
            else if (roleAssignment.AssignmentType == RoleAssignmentType.GROUP)
            {
                await foreach (var targetApp in roleTargetGroupApi.ListApplicationTargetsForApplicationAdministratorRoleForGroup(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetApps.Add(targetApp);
                }
            }
            else if (roleAssignment.AssignmentType == RoleAssignmentType.CLIENT)
            {
                await foreach (var targetApp in roleTargetAppApi.ListAppTargetRoleToClient(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetApps.Add(targetApp);
                }
            }
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the scope of the {RoleAssignmentId} role assignment.", e.ErrorCode, status, roleAssignment.Id);
        }

        // Translate possible application types into specific application instances
        List<OktaNode> targetAppsTranslated = new(targetApps.Capacity);

        foreach (var targetApp in targetApps)
        {
            if (targetApp.Status != CatalogApplicationStatus.ACTIVE)
            {
                // Skip inactive app targets
                continue;
            }

            if (targetApp.Id != null)
            {
                _logger.LogTrace(
                    "The {Role} role targets the {AppName} ({AppId}) application.",
                    roleAssignment.Label,
                    targetApp.Name,
                    targetApp.Id);

                // Resolve a specific application instance
                var resolvedApp = _graph.GetApplication(targetApp.Id);

                if (resolvedApp is null)
                {
                    _logger.LogWarning(
                        "The {Role} role assignment targets the {AppName} ({AppId}) application, but it was not found in the graph.",
                        roleAssignment.Label,
                        targetApp.Name,
                        targetApp.Id);
                    continue;
                }

                targetAppsTranslated.Add(resolvedApp);
            }
            else
            {
                _logger.LogTrace(
                    "The {Role} role targets all instances of the {AppName} application.",
                    roleAssignment.Label,
                    targetApp.DisplayName);

                // Resolve to all matching applications and service integrations in the graph
                targetAppsTranslated.AddRange(_graph.GetAppsAndIntegrations(targetApp.Name));
            }
        }

        return targetAppsTranslated;
    }

    public async Task<List<OktaGroup>> FetchRoleAssignmentGroupTargets(StandardRole roleAssignment, string assigneeId, CancellationToken cancellationToken = default)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        // Prepare empty result list
        List<Group> targetGroups = [];

        try
        {
            RoleBTargetAdminApi roleTargetUserApi = new(_oktaConfig);
            RoleBTargetBGroupApi roleTargetGroupApi = new(_oktaConfig);
            RoleBTargetClientApi roleTargetAppApi = new(_oktaConfig);

            if (roleAssignment.AssignmentType == RoleAssignmentType.USER)
            {
                await foreach (var targetGroup in roleTargetUserApi.ListGroupTargetsForRole(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetGroups.Add(targetGroup);
                }
            }
            else if (roleAssignment.AssignmentType == RoleAssignmentType.GROUP)
            {
                await foreach (var targetGroup in roleTargetGroupApi.ListGroupTargetsForGroupRole(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetGroups.Add(targetGroup);
                }
            }
            else if (roleAssignment.AssignmentType == RoleAssignmentType.CLIENT)
            {
                await foreach (var targetGroup in roleTargetAppApi.ListGroupTargetRoleForClient(assigneeId, roleAssignment.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    targetGroups.Add(targetGroup);
                }
            }
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the scope of the {RoleAssignmentId} role assignment.", e.ErrorCode, status, roleAssignment.Id);
        }

        // Find the target groups in the graph
        List<OktaGroup> resolvedTargetGroups = new(targetGroups.Capacity);

        foreach (var targetGroup in targetGroups)
        {
            OktaGroup? groupNode = _graph.GetGroup(targetGroup.Id);

            if (groupNode is null)
            {
                _logger.LogWarning(
                    "The {Role} role assignment targets group {GroupName} ({GroupId}), but it was not found in the graph.",
                    roleAssignment.Label,
                    GetGroupName(targetGroup),
                    targetGroup.Id);
                continue;
            }

            resolvedTargetGroups.Add(groupNode);
        }

        return resolvedTargetGroups;
    }

    public async Task FetchOktaResourceSetRoleAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching resource set role assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.ResourceSets, concurrency, async (resourceSetNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching role assignments scoped to the {ResourceSetName} ({ResourceSetId}) resource set...", resourceSetNode.Name, resourceSetNode.OriginalId);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                RoleDResourceSetBindingApi resourceSetBindingApi = new(_oktaConfig);
                RoleDResourceSetBindingMemberApi resourceSetBindingMemberApi = new(_oktaConfig);

                // TODO: Implement resource set bindings pagination if needed
                ResourceSetBindings bindings = await resourceSetBindingApi.ListBindingsAsync(resourceSetNode.OriginalId, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (ResourceSetBindingRole role in bindings.Roles)
                {
                    // TODO: Implement binding members pagination if needed
                    ResourceSetBindingMembers members = await resourceSetBindingMemberApi.ListMembersOfBindingAsync(resourceSetNode.OriginalId, role.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                    foreach (ResourceSetBindingMember member in members.Members)
                    {
                        string roleAssignmentId = member.Id;

                        // Extract entity ID from the URL, e.g., https://integrator-5415459.okta.com/api/v1/users/00u1abcd2EFGH3IJK4LM
                        string assigneeId = member.Links.Self.Href.Split('/').Last();

                        // Create the (:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_ResourceSet) edge
                        _logger.LogTrace(
                            "Resource set {ResourceSetName} ({ResourceSetId}) has role assignment {RoleAssignmentId} for assignee {AssigneeId}.",
                            resourceSetNode.Name,
                            resourceSetNode.OriginalId,
                            roleAssignmentId,
                            assigneeId);
                        OpenGraphEdgeNode roleAssignmentNode = OktaRoleAssignment.CreateEdgeNode(roleAssignmentId, assigneeId);
                        _graph.AddEdge(roleAssignmentNode, resourceSetNode, OktaRoleAssignment.ScopedToEdgeKind);
                    }
                }

            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch role assignments scoped to the {ResourceSetName} ({ResourceSetId}) resource set.", e.ErrorCode, status, resourceSetNode.Name, resourceSetNode.OriginalId);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching resource set role assignments.");
    }
}
