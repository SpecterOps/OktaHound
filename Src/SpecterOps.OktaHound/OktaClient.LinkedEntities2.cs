using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.ActiveDirectory;
using SpecterOps.OktaHound.Model.Entra;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task CollectOktaApplicationGrants(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application grants...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        ApplicationGrantsApi appGrantsApi = new(_oktaConfig);
        int grantCount = 0;

        var serviceApps = await dbContext.Applications
            .Where(application => application.IsService)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var appNode in serviceApps)
        {
            try
            {
                _logger.LogDebug("Fetching consent grants for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);
                int appGrantCount = 0;

                await foreach (var grant in appGrantsApi.ListScopeConsentGrants(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    if (!string.IsNullOrWhiteSpace(grant.ScopeId))
                    {
                        var grantNode = new Database.OktaApplicationGrant(grant, appNode.Id);
                        await dbContext.ApplicationGrants.AddAsync(grantNode, cancellationToken).ConfigureAwait(false);
                        appGrantCount++;
                        grantCount++;
                    }
                }

                _logger.LogTrace("The {AppName} ({AppId}) application has {GrantCount} grants.", appNode.Name, appNode.Id, appGrantCount);

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                if (status == HttpStatusCode.NotFound)
                {
                    // Ignore error 404, which is returned for applications without any consent grants.
                    _logger.LogTrace("The {AppName} ({AppId}) application is not granted any OAuth scopes.", appNode.Name, appNode.Id);
                }
                else
                {
                    // TODO: This currently fails if the app is not assigned the Super Admin role.
                    // Okta Community contacted: https://support.okta.com/help/s/question/0D5KZ00001ZNRgb0AH/scope-oktaappgrantsread-requires-super-admin?language=en_US
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch consent grants for the {AppName} ({AppId}) application.", e.ErrorCode, status, appNode.Name, appNode.Id);
                }
            }
        }

        _logger.LogInformation("Successfully fetched {GrantCount} application grants.", grantCount);
    }

    public async Task CollectOktaAppUserAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application user assignments...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        ApplicationUsersApi appUsersApi = new(_oktaConfig);
        int assignmentCount = 0;

        var applications = await dbContext.Applications
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var appNode in applications)
        {
            try
            {
                _logger.LogDebug("Fetching user assignments for application {AppName} ({AppId})...", appNode.Name, appNode.Id);
                int appAssignmentCount = 0;

                await foreach (var appUserAssignment in appUsersApi.ListApplicationUsers(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    await dbContext.ApplicationUserAssignments
                    .AddAsync(new Database.OktaApplicationUserAssignment(appUserAssignment, appNode.Id), cancellationToken)
                    .ConfigureAwait(false);

                    appAssignmentCount++;
                    assignmentCount++;
                }

                _logger.LogDebug("Fetched {AssignmentCount} user assignments for application {AppName} ({AppId}).", appAssignmentCount, appNode.Name, appNode.Id);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta app {AppName} ({AppId}) user assignments.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }

        _logger.LogInformation("Successfully fetched {AssignmentCount} application user assignments.", assignmentCount);
    }

    public async Task CollectOktaAppGroupAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application group assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            // The "/api/v1/apps*" endpoint has a much lower rate limit than other APIs, so we do not parallelize the calls.
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.Applications, concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching group assignments for application {AppName} ({AppId})...", appNode.Name, appNode.Id);
                ApplicationGroupsApi appGroupsApi = new(_oktaConfig);

                // Try to resolve the domain SIDs of apps representing AD domains
                string? domainSid = appNode.ActiveDirectoryDomainSid;

                await foreach (var appGroupAssignment in appGroupsApi.ListApplicationGroupAssignments(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // TODO: Only consider active group assignments

                    _logger.LogTrace("Group {GroupId} is assigned the {AppName} ({AppId}) application.", appGroupAssignment.Id, appNode.Name, appNode.Id);

                    // Create the (:Okta_Group)-[:Okta_AppAssignment]->(:Okta_Application) edge
                    var groupNode = OktaGroup.CreateEdgeNode(appGroupAssignment.Id);
                    _graph.AddEdge(groupNode, appNode, OktaApplication.AppAssignmentEdgeKind);

                    if (domainSid is null && appNode.IsActiveDirectory)
                    {
                        // Try to fetch the group and cache its domain SID
                        OktaGroup? group = _graph.GetGroup(appGroupAssignment.Id);
                        domainSid = group?.DomainSid;
                    }

                    // Sync edges are handled when fetching the group itself.
                }

                if (domainSid is not null && appNode.ActiveDirectoryDomainSid is null)
                {
                    // Add AD domain SID, so that the Domain node can later be created.
                    appNode.ActiveDirectoryDomainSid = domainSid;
                }

                _logger.LogTrace("Finished fetching group assignments for application {AppName} ({AppId}).", appNode.Name, appNode.Id);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta app {AppName} ({AppId}) group assignments.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application group assignments.");
    }

    public async Task CollectOktaAppGroupPushMappings(CancellationToken cancellationToken = default)
    {
        // Group Push lets you push Okta groups and their members to provisioning-enabled third - party apps.
        _logger.LogInformation("Fetching application group push mappings...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            // The "/api/v1/apps*" endpoint has a much lower rate limit than other APIs, so we do not parallelize the calls.
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken
        };

        // Only target AD and SCIM applications
        await Parallel.ForEachAsync(_graph.Applications.Where(app => app.SupportsSCIM || app.IsActiveDirectory), concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching group push mappings for application {AppName} ({AppId})...", appNode.Name, appNode.Id);
                GroupApi groupApi = new(_oktaConfig);
                GroupPushMappingApi groupPushApi = new(_oktaConfig);

                await foreach (var pushMapping in groupPushApi.ListGroupPushMappings(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("Application {AppName} ({AppId}) is configured for outbound sync of group {GroupId}.",
                        appNode.Name,
                        appNode.Id,
                        pushMapping.SourceGroupId);

                    // Create the (:Okta_Group)-[:Okta_GroupPush]->(:Okta_Application) edge
                    var groupNode = OktaGroup.CreateEdgeNode(pushMapping.SourceGroupId);
                    _graph.AddEdge(groupNode, appNode, OktaGroup.GroupPushEdgeKind);

                    // TODO: Wrap this call into another try/catch block
                    // Fetch pushed group details using a standalone app group API request,
                    // as the pushed group targets are not returned in the group push mapping object nor in group listing.
                    Group targetGroup = await groupApi.GetGroupAsync(pushMapping.TargetGroupId, cancellationToken: cancellationToken).ConfigureAwait(false);

                    /*
                    The result only contains group name, but no ID.
                    "id": "00gxg1fe4vh7hOAcu697",
                    "created": "2025-11-14T08:01:28.000Z",
                    "lastUpdated": "2025-11-14T08:01:28.000Z",
                    "lastMembershipUpdated": "2025-11-14T08:01:28.000Z",
                    "objectClass": [
                        "okta:user_group"
                        ],
                    "type": "APP_GROUP",
                    "profile": {
                        "name": "Test Okta Snowflake Users",
                        "description": "Test group push from Okta to Snowflake"
                    },
                    "source": {
                        "id": "0oaxfbsrccSXeiKWB697"
                    },
                    */
                    if (targetGroup.Profile.ActualInstance is OktaUserGroupProfile targetGroupProfile)
                    {
                        // Even AD pushed groups are mapped to OktaUserGroupProfile instead of OktaActiveDirectoryGroupProfile
                        // Create a hybrid edge
                        // Example: (:Okta_Group)-[:Okta_MembershipSync]->(:Group)
                        // Example: (:Okta_Group)-[:Okta_MembershipSync]->(:Okta_Group)
                        var targetGroupNode = appNode.CreateHybridGroupEdgeNode(targetGroupProfile);
                        if (targetGroupNode is not null)
                        {
                            _hybridEdgeGraph.AddEdge(groupNode, targetGroupNode, OktaGroup.MembershipSyncEdgeKind);

                            if (appNode.IsActiveDirectory)
                            {
                                // TODO: For AD, add the group node to the AD graph
                                // _adGraph.AddNode(new ActiveDirectoryGroup(targetGroup, appNode.ActiveDirectoryDomain ?? appNode.Name));
                            }
                        }
                    }
                }

                _logger.LogTrace("Finished fetching group push mappings for application {AppName} ({AppId}).", appNode.Name, appNode.Id);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta app {AppName} ({AppId}) group push mappings.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application group push mappings.");
    }

    public async Task CollectOktaResourceSetMemberships(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching resource set memberships...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        RoleCResourceSetResourceApi resourceSetApi = new(_oktaConfig);
        var resourceSets = await dbContext.ResourceSets
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var resourceSetNode in resourceSets)
        {
            try
            {
                _logger.LogDebug("Fetching membership of the {ResourceSetName} ({ResourceSetId}) resource set...", resourceSetNode.Name, resourceSetNode.OriginalId);
                List<string> resourceUris = [];

                await foreach (ResourceSetResource resource in resourceSetApi.ListAllResourceSetResources(resourceSetNode.OriginalId, cancellationToken).ConfigureAwait(false))
                {
                    string? resourceUri = resource.Links?.Self?.Href;

                    if (resourceUri is null)
                    {
                        // Several resource types, including "Identity and access management",
                        // "Workflows", "Customizations", or "Support Cases", are not exposed through the API as entities.
                        // Example "orn": "orn:okta:iam:00ow0o8if0CNwsKmk697:contained_resources"
                        // Example "orn": "orn:okta:support:00ow0o8if0CNwsKmk697:cases"
                        // Example "orn": "orn:okta:workflow:00ow0o8if0CNwsKmk697:flows"
                        // Example "orn": "orn:okta:idp:00ow0o8if0CNwsKmk697:customizations"
                        // TODO: Consider adding these virtual entities with ORNs as nodes to the graph.
                        // Skip processing this resource set assignment
                        continue;
                    }

                    resourceUris.Add(resourceUri);
                }

                resourceSetNode.ResourceUris = resourceUris;
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the membership of the {ResourceSetName} ({ResourceSetId}) resource set.", e.ErrorCode, status, resourceSetNode.Name, resourceSetNode.OriginalId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching resource set memberships.");
    }

    public IEnumerable<OpenGraphEdgeNode> ResolveResourceSetMembership2(string resourceUrl)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        if (resourceUrl.EndsWith("/api/v1/users", StringComparison.InvariantCulture))
        {
            // All users in the organization
            // Example: https://integrator-5415459.okta.com/api/v1/users
            return _graph.Users.Select(user => user.ToEdgeNode());
        }
        else if (resourceUrl.EndsWith("/api/v1/groups", StringComparison.InvariantCulture))
        {
            // All groups in the organization
            // Example: https://integrator-5415459.okta.com/api/v1/groups
            return _graph.Groups.Select(group => group.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/users/", StringComparison.InvariantCulture))
        {
            // Specific user
            // Example: https://integrator-5415459.okta.com/api/v1/users/4g65ds4gsd4g65dsf4
            string userId = resourceUrl.Split('/').Last();
            return [OktaUser.CreateEdgeNode(userId)];
        }
        else if (resourceUrl.Contains("/api/v1/groups/", StringComparison.InvariantCulture) &&
                 resourceUrl.EndsWith("/users", StringComparison.InvariantCulture))
        {
            // Members of a specific group
            // Example: https://integrator-5415459.okta.com/api/v1/groups/00gw2t2qcta3zvASN697/users
            string groupId = resourceUrl.Split('/')[^2];
            return _graph.GetGroupMembers([groupId]).Select(user => user.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/groups/", StringComparison.InvariantCulture))
        {
            // Specific group
            // Example: https://integrator-5415459.okta.com/api/v1/groups/00gw2t2qcta3zvASN697
            string groupId = resourceUrl.TrimEnd('/').Split('/').Last();
            return [OktaGroup.CreateEdgeNode(groupId)];
        }
        else if (resourceUrl.EndsWith("/api/v1/apps", StringComparison.InvariantCulture))
        {
            // All apps in the organization
            // Example: https://integrator-5415459.okta.com/api/v1/apps
            return _graph.ApplicationsAndApiServiceIntegrations.Select(app => app.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/apps?filter=name+eq+\"") &&
                 resourceUrl.EndsWith("\"", StringComparison.InvariantCulture))
        {
            // Application or API service integration type by name
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"testdome"
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"github"
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"githubcloud"
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"ghecenterprisesaml"
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"aws"
            // Example: https://integrator-5415459.okta.com/api/v1/apps?filter=name+eq+"office365"
            string appType = resourceUrl.Split('"')[^2];
            return _graph.GetAppsAndIntegrations(appType).Select(app => app.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/apps/", StringComparison.InvariantCulture))
        {
            // Specific application or API service integration instance
            // Example: https://integrator-5415459.okta.com/api/v1/apps/0oawyp12cjglrkfId697
            string appId = resourceUrl.Split('/').Last();
            // Use generic OktaNode ID matching, as the object kind could be OktaApplication or OktaApiServiceIntegration
            var appNode = OktaNode.CreateEdgeNode(appId);
            return [appNode];
        }
        else if (resourceUrl.EndsWith("/api/v1/authorizationServers", StringComparison.InvariantCulture))
        {
            // All authorization servers
            // Example: https://integrator-5415459.okta.com/api/v1/authorizationServers
            return _graph.AuthorizationServers.Select(authServer => authServer.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/authorizationServers/", StringComparison.InvariantCulture))
        {
            // Specific authorization server
            // Example: https://integrator-5415459.okta.com/api/v1/authorizationServers/GeGRTEr7f3yu2n7grw22
            string authServerId = resourceUrl.Split('/').Last();
            var authServerNode = OktaAuthorizationServer.CreateEdgeNode(authServerId);
            return [authServerNode];
        }
        else if (resourceUrl.EndsWith("/api/v1/devices", StringComparison.InvariantCulture))
        {
            // All devices
            // Example: https://integrator-5415459.okta.com/api/v1/devices
            return _graph.Devices.Select(device => device.ToEdgeNode());
        }
        else if (resourceUrl.EndsWith("/api/v1/idps", StringComparison.InvariantCulture))
        {
            // All identity providers
            // Example: https://integrator-5415459.okta.com/api/v1/idps
            return _graph.IdentityProviders.Select(idp => idp.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/idps/", StringComparison.InvariantCulture))
        {
            // Specific identity provider
            // Example: https://integrator-5415459.okta.com/api/v1/idps/0oaulob4BFVa4zQvt0g3
            string identityProviderId = resourceUrl.Split('/').Last();
            var identityProviderNode = OktaIdentityProvider.CreateEdgeNode(identityProviderId);
            return [identityProviderNode];
        }
        else if (resourceUrl.EndsWith("/api/v1/policies", StringComparison.InvariantCulture))
        {
            // All policies (not currently supported by Okta)
            // Example: https://integrator-5415459.okta.com/api/v1/policies
            return _graph.Policies.Select(policy => policy.ToEdgeNode());
        }
        else if (resourceUrl.Contains("/api/v1/policies/", StringComparison.InvariantCulture))
        {
            // Specific policy type
            // Example: https://integrator-5415459.okta.com/api/v1/policies/ACCESS_POLICY
            string policyType = resourceUrl.Split('/').Last();

            // TODO: Consider storing policies in a dictionary with the policy type as a key
            return _graph.Policies.Where(policy => policy.PolicyType == policyType).Select(policy => policy.ToEdgeNode());
        }

        // Return an empty set if we could not resolve the URL
        _logger.LogWarning("Could not resolve resource set member from URL: {ResourceUrl}", resourceUrl);
        return [];
    }

    public async Task CollectOktaCustomRolePermissions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching custom role permissions...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.CustomRoles, concurrency, async (customRoleNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching permissions of the {RoleName} ({RoleId}) custom role...", customRoleNode.Name, customRoleNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                RoleECustomPermissionApi roleApi = new(_oktaConfig);

                Permissions permissions = await roleApi.ListRolePermissionsAsync(customRoleNode.Id, cancellationToken).ConfigureAwait(false);

                // Ignore permission conditions and add their list to the role properties.
                customRoleNode.Permissions = [.. permissions._Permissions.Select(item => item.Label)];
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the permissions of the {RoleName} ({RoleId}) custom role.", e.ErrorCode, status, customRoleNode.Name, customRoleNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching custom role permissions.");
    }

    public async Task CollectOktaPrivilegedUsers(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching the list of users with role assignments...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);

        try
        {
            int privilegedUserCount = 0;
            RoleAssignmentAUserApi roleAssignmentApi = new(_oktaConfig);

            await foreach (RoleAssignedUser privilegedUser in roleAssignmentApi.ListAllUsersWithRoleAssignments(limit: null, cancellationToken).ConfigureAwait(false))
            {
                var privilegedUserNode = new Database.OktaPrivilegedUser(privilegedUser);
                await dbContext.PrivilegedUsers.AddAsync(privilegedUserNode, cancellationToken).ConfigureAwait(false);
                privilegedUserCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {PrivilegedUserCount} users with role assignments.", privilegedUserCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while fetching the list of users with role assignments.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaPolicyRules(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Okta policy rules...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        // Agentless DSSO needs to be enabled for a domain and at least one active policy rule targeting it must exist.
        bool dssoEnabled = false;

        await Parallel.ForEachAsync(_graph.Policies, concurrency, async (policyNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching rules associated with the {PolicyName} ({PolicyId}) policy...", policyNode.Name, policyNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                PolicyApi policyApi = new(_oktaConfig);

                await foreach (var policyRule in policyApi.ListPolicyRules(policyNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // For now, we just process the Agentless Desktop SSO rules.
                    if (policyRule is IdpDiscoveryPolicyRule idpDiscoveryRule)
                    {
                        // TODO: There is currently no enum value corresponding to "AgentlessDSSO".
                        var foundDssoProvider = idpDiscoveryRule.Actions?.Idp?.Providers.FirstOrDefault(provider => provider.Type?.Value == "AgentlessDSSO");

                        if (foundDssoProvider is not null)
                        {
                            _logger.LogTrace("The Agentless Desktop SSO is {Status} in rule {RuleName} ({RuleId}).", idpDiscoveryRule.Status, idpDiscoveryRule.Name, idpDiscoveryRule.Id);
                            if (idpDiscoveryRule.Status == LifecycleStatus.ACTIVE)
                            {
                                dssoEnabled = true;
                            }
                        }
                    }
                    // TODO: Process other policy rule types as needed
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch rules associated with the {PolicyName} ({PolicyId}) policy.", e.ErrorCode, status, policyNode.Name, policyNode.Id);
            }
        }).ConfigureAwait(false);

        // Save the value in the organization node
        _graph.Organization.AgentlessDssoEnabled = dssoEnabled;

        _logger.LogInformation("Finished fetching Okta policy rules.");
    }

    [Obsolete]
    public async Task CollectOktaPolicyMappings(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Okta policy mappings...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.Policies, concurrency, async (policyNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Retrieving resource mapping for the {PolicyName} ({PolicyId}) policy...", policyNode.Name, policyNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                PolicyApi policyApi = new(_oktaConfig);

                // TODO: Switch to ListPolicyMappings() and process policy mapping URLs
                await foreach (var application in policyApi.ListPolicyApps(policyNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // Create the (:Okta_Policy)-[:Okta_PolicyMapping]->(:Okta_Application) edge
                    _logger.LogTrace("The {PolicyName} ({PolicyId}) policy is mapped to the {AppName} ({AppId}) application.", policyNode.Name, policyNode.Id, application.Label, application.Id);
                    var appNode = OktaApplication.CreateEdgeNode(application.Id);
                    _graph.AddEdge(policyNode, appNode, OktaPolicy.PolicyMappingEdgeKind);
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                // Ignore errors 404 and 400, which are returned for policies with no app mappings.
                if (status != HttpStatusCode.NotFound && status != HttpStatusCode.BadRequest)
                {
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to retrieve resource mapping for the {PolicyName} ({PolicyId}) policy.", e.ErrorCode, status, policyNode.Name, policyNode.Id);
                }
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching Okta policy mappings.");
    }

    public async Task CollectOktaApplicationSecrets(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application secrets...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        ApplicationSSOPublicKeysApi ssoApi = new(_oktaConfig);
        int secretCount = 0;
        var serviceApps = await dbContext.Applications
            .Where(application => application.IsService)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var appNode in serviceApps)
        {
            try
            {
                _logger.LogDebug("Fetching the list of secrets configured for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);

                await foreach (var secret in ssoApi.ListOAuth2ClientSecrets(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // Create the Okta_ClientSecret node
                    _logger.LogTrace("The {AppName} ({AppId}) application has an {Status} client secret {SecretId}.", appNode.Name, appNode.Id, secret.Status?.Value?.ToLowerInvariant(), secret.Id);
                    Database.OktaClientSecret secretNode = new(secret, appNode.Id, appNode.DomainName);
                    await dbContext.ClientSecrets.AddAsync(secretNode, cancellationToken).ConfigureAwait(false);
                    secretCount++;
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list secrets for the {AppName} ({AppId}) application.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully fetched {SecretCount} application client secrets.", secretCount);
    }

    public async Task CollectOktaApplicationJsonWebKeys(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application JSON Web Keys...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        ApplicationSSOPublicKeysApi publicKeyApi = new(_oktaConfig);
        int keyCount = 0;

        var serviceApps = await dbContext.Applications
            .Where(application => application.IsService)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var appNode in serviceApps)
        {
            try
            {
                _logger.LogDebug("Fetching the list of JSON Web Keys configured for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);
                int appKeyCount = 0;

                OAuth2ClientJsonWebKeySet appKeys = await publicKeyApi.ListJwkAsync(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var jwk in appKeys.Keys ?? [])
                {
                    // We are not interested in encryption keys
                    if (jwk.ActualInstance is OAuth2ClientJsonSigningKeyResponse signingKey)
                    {
                        Database.OktaJWK? keyNode = null;

                        if (signingKey.ActualInstance is OAuth2ClientJsonWebKeyECResponse ecKey)
                        {
                            keyNode = new(ecKey, appNode.Id, appNode.DomainName);
                            _logger.LogTrace("Fetched EC key {KeyId} for the {AppName} ({AppId}) application.", keyNode.Id, appNode.Name, appNode.Id);
                        }
                        else if (signingKey.ActualInstance is OAuth2ClientJsonWebKeyRsaResponse rsaKey)
                        {
                            keyNode = new(rsaKey, appNode.Id, appNode.DomainName);
                            _logger.LogTrace("Fetched RSA key {KeyId} for the {AppName} ({AppId}) application.", keyNode.Id, appNode.Name, appNode.Id);
                        }
                        else
                        {
                            _logger.LogWarning("The {AppName} ({AppId}) application has a JSON Web Key with an unsupported key type ({KeyType}).", appNode.Name, appNode.Id, signingKey.GetType().Name);
                            continue;
                        }

                        _logger.LogTrace("The {AppName} ({AppId}) application has a JSON Web Key ({KeyId}).", appNode.Name, appNode.Id, keyNode.Id);
                        await dbContext.JWKs.AddAsync(keyNode, cancellationToken).ConfigureAwait(false);
                        appKeyCount++;
                        keyCount++;
                    }
                }

                _logger.LogDebug("Fetched {KeyCount} JSON Web Keys for the {AppName} ({AppId}) application.", appKeyCount, appNode.Name, appNode.Id);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list JSON Web Keys for the {AppName} ({AppId}) application.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }
        _logger.LogInformation("Successfully fetched {KeyCount} application JSON Web Keys.", keyCount);
    }

    public async Task CollectOktaApiServiceIntegrationSecrets(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API service integration secrets...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        ApiServiceIntegrationsApi integrationsApi = new(_oktaConfig);
        int secretCount = 0;

        foreach (var serviceNode in dbContext.ApiServiceIntegrations)
        {
            _logger.LogDebug("Fetching the list of secrets configured for the {ServiceName} ({ServiceId}) API service integration...", serviceNode.Name, serviceNode.Id);

            try
            {
                await foreach (var secret in integrationsApi.ListApiServiceIntegrationInstanceSecrets(serviceNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // Create the Okta_ClientSecret node
                    _logger.LogTrace("The {ServiceName} ({ServiceId}) API service integration has an {Status} client secret {SecretId}.", serviceNode.Name, serviceNode.Id, secret.Status?.Value?.ToLowerInvariant(), secret.Id);
                    Database.OktaClientSecret secretNode = new(secret, serviceNode.Id, serviceNode.DomainName);
                    await dbContext.ClientSecrets.AddAsync(secretNode, cancellationToken).ConfigureAwait(false);
                    secretCount++;
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list secrets for the {ServiceName} ({ServiceId}) API service integration.", e.ErrorCode, status, serviceNode.Name, serviceNode.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully fetched {SecretCount} API service integration secrets.", secretCount);
    }

    public async Task CollectOktaGroupMemberships(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching group memberships...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        GroupApi groupApi = new(_oktaConfig);
        int membershipCount = 0;

        var groups = await dbContext.Groups
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var groupNode in groups)
        {
            try
            {
                _logger.LogDebug("Fetching the list of members for group {GroupName} ({GroupId})...", groupNode.Name, groupNode.Id);
                int groupMembershipCount = 0;

                await foreach (var groupMember in groupApi.ListGroupUsers(groupNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("User {UserId} is a member of the {GroupName} ({GroupId}) group.", groupMember.Id, groupNode.Name, groupNode.Id);

                    await dbContext.UserGroupMemberships.AddAsync(new Database.OktaUserGroupMembership
                    {
                        UserId = groupMember.Id,
                        GroupId = groupNode.Id
                    }, cancellationToken).ConfigureAwait(false);

                    groupMembershipCount++;
                    membershipCount++;
                }

                _logger.LogDebug("Group {GroupName} ({GroupId}) has {MembershipCount} members.", groupNode.Name, groupNode.Id, groupMembershipCount);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch group {GroupName} ({GroupId}) members.", e.ErrorCode, status, groupNode.Name, groupNode.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully fetched {MembershipCount} group memberships.", membershipCount);
    }

    public async Task CollectOktaUserAuthenticationFactors(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user authentication factors...");
        using var dbContext = new Database.AppDbContext(_outputDirectory);
        UserFactorApi authFactorsApi = new(_oktaConfig);
        var users = await dbContext.Users.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var userNode in users)
        {
            try
            {
                _logger.LogTrace("Fetching the list of authentication factors for user {UserName} ({UserId})...", userNode.Name, userNode.Id);

                // Get user's authentication factors count
                int count = 0;

                await foreach (var factor in authFactorsApi.ListFactors(userNode.Id, cancellationToken).ConfigureAwait(false))
                {
                    count++;
                    var factorNode = new Database.OktaUserFactor(factor, userNode.Id);
                    await dbContext.UserFactors.AddAsync(factorNode, cancellationToken).ConfigureAwait(false);
                }

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogTrace("User {UserName} has {FactorCount} authentication factors.", userNode.Name, count);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch authentication factors for user {UserName} ({UserId}).", e.ErrorCode, status, userNode.Name, userNode.Id);
            }
        }

        _logger.LogInformation("Finished fetching user authentication factors.");
    }

    public async Task CollectOktaIdentityProviderUsers(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching identity provider users...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.IdentityProviders, concurrency, async (idpNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching the list of users associated with the {IdpName} ({IdpId}) identity provider...", idpNode.Name, idpNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                IdentityProviderUsersApi identityProviderApi = new(_oktaConfig);

                await foreach (var idpUser in identityProviderApi.ListIdentityProviderApplicationUsers(idpNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("User {UserId} is associated with the {IdpName} ({IdpId}) identity provider.", idpUser.Id, idpNode.Name, idpNode.Id);

                    // Create the (:Okta_IdentityProvider)-[:Okta_IdentityProviderFor]->(:Okta_User) edge
                    var userNode = OktaUser.CreateEdgeNode(idpUser.Id);
                    OpenGraphEdge idpEdge = new(idpNode, userNode, OktaIdentityProvider.IdpForEdgeKind);
                    _graph.AddEdge(idpEdge);

                    // Add user's external identity to the OktaIdentityProviderFor edge
                    idpEdge.SetProperty(OktaIdentityProvider.ExternalIdPropertyName, idpUser.ExternalId);

                    // For Entra ID tenants, also create an (:AZUser)-[:Okta_InboundSSO]->(:Okta_User) edge
                    // We currently only create this edge for users who are already linked to the IdP, ignoring possible auto-linking configuration.
                    if (idpNode.TenantId is not null && idpUser.ExternalId is not null)
                    {
                        OpenGraphEdgeNode entraUserNode = EntraIdUser.CreateEdgeNode(idpUser.ExternalId, idpNode.TenantId);
                        _hybridEdgeGraph.AddEdge(entraUserNode, userNode, OktaIdentityProvider.InboundSsoEdgeKind);
                    }
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list users associated with the {IdpName} ({IdpId}) identity provider.", e.ErrorCode, status, idpNode.Name, idpNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching identity provider users.");
    }
}
