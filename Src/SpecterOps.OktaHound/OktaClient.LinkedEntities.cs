using System.Net;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.ActiveDirectory;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task FetchOktaApplicationGrants(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application grants...");

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

        // Only OIDC applications can have scopes assigned. Services appear to be a subset of OIDC apps.
        await Parallel.ForEachAsync(_graph.Applications.Where(app => app.IsService || app.IsOIDCApplication), concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching consent grants for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                ApplicationGrantsApi appGrantsApi = new(_oktaConfig);

                List<string> scopeIds = [];

                await foreach (var grant in appGrantsApi.ListScopeConsentGrants(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    if (!string.IsNullOrWhiteSpace(grant.ScopeId))
                    {
                        scopeIds.Add(grant.ScopeId);
                    }
                }

                _logger.LogTrace("The {AppName} ({AppId}) application has {ScopeCount} grants.", appNode.Name, appNode.Id, scopeIds.Count);

                if (scopeIds.Count > 0)
                {
                    appNode.Permissions = scopeIds;
                }
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
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application grants.");
    }

    public async Task FetchOktaAppUserAssignments(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application user assignments...");

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
                _logger.LogDebug("Fetching user assignments for application {AppName} ({AppId})...", appNode.Name, appNode.Id);
                ApplicationUsersApi appUsersApi = new(_oktaConfig);

                // If this app represents an AD domain, derive its SID from any associated user account
                string? domainSid = null;
                string? domainFQDN = appNode.ActiveDirectoryDomain ?? appNode.Name;

                await foreach (var appUserAssignment in appUsersApi.ListApplicationUsers(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    // Pre-create the OktaUser edge node
                    var oktaUser = OktaUser.CreateEdgeNode(appUserAssignment.Id);

                    // Login of the user in the target application
                    string? targetUserName = appUserAssignment.Credentials?.UserName;

                    // Sync direction can be inbound or outbound
                    bool inbound = false;

                    // Handle hybrid paths first, regardless if the user is assigned directly or through a group membership.
                    if (appUserAssignment.SyncState == AppUserSyncState.SYNCHRONIZED)
                    {
                        // This is a hybrid user
                        if (appNode.IsActiveDirectory)
                        {
                            // This user is synced to/from AD

                            // Heuristics: Outbound synced AD users are scoped through groups.
                            // Inbound synced users are assigned directly to the AD app.
                            // Only inbound users have objectSid in the appUser objects.
                            // ExternalId has BASE64 of objectGuid, regardless of the direction.
                            inbound = appUserAssignment.Scope == AppUser.ScopeEnum.USER;

                            // Create the User AD node
                            ActiveDirectoryUser adUser = new(appUserAssignment, domainFQDN);
                            _adGraph.AddNode(adUser);

                            // Cache the domain SID
                            domainSid ??= adUser.DomainSid;

                            if (inbound)
                            {
                                _logger.LogTrace("User {UserName} ({UserId}) is synchronized FROM Active Directory.", adUser.Name, adUser.Id);

                                // Create the (:User)-[:Okta_UserSync]->(:Okta_User) hybrid edge
                                _hybridEdgeGraph.AddEdge(adUser, oktaUser, OktaUser.UserSyncEdgeKind);
                            }
                            else
                            {
                                _logger.LogTrace("User {UserName} ({UserId}) is synchronized TO Active Directory.", adUser.Name, adUser.Id);

                                // Create the (:Okta_User)-[:Okta_UserSync]->(:User) hybrid edge
                                _hybridEdgeGraph.AddEdge(oktaUser, adUser, OktaUser.UserSyncEdgeKind);
                            }

                            OpenGraphEdgeNode? domainNode = null;

                            if (domainSid is not null)
                            {
                                // We can uniquely match the AD domain node by its SID
                                domainNode = ActiveDirectoryDomain.CreateEdgeNode(domainSid);
                            }
                            else
                            {
                                // If only outbound sync is configured for the domain or we are early in the processing,
                                // the domain SID might not be known yet. We thus identify the domain by FQDN instead of SID,
                                // which is not 100% reliable.
                                domainNode = ActiveDirectoryDomain.CreateEdgeNode(appNode.DomainName, "name");
                            }

                            // Add the (:Domain)-[:Contains]->(:User) edge to the AD graph
                            _adGraph.AddEdge(domainNode, adUser, ActiveDirectoryDomain.ContainsEdgeKind);
                        }
                        else if (appNode.IsLdapInterface)
                        {
                            // This user is imported from LDAP
                            inbound = true;
                        }
                        else
                        {
                            // TODO: Handle sync directions for SCIM users.
                            inbound = false;

                            // This user is probably synced using SCIM to/from the target app.
                            OpenGraphEdgeNode? scimUser = appNode.CreateHybridUserNode(targetUserName);
                            if (scimUser is not null)
                            {
                                if (inbound)
                                {
                                    // Create the ()-[:Okta_UserSync]->(:Okta_User) hybrid edge
                                    _hybridEdgeGraph.AddEdge(scimUser, oktaUser, OktaUser.UserSyncEdgeKind);
                                }
                                else
                                {
                                    // Create the (:Okta_User)-[:Okta_UserSync]->() hybrid edge
                                    _hybridEdgeGraph.AddEdge(oktaUser, scimUser, OktaUser.UserSyncEdgeKind);
                                }
                            }
                        }

                        // Connect the user to the syncing application as well
                        if (inbound)
                        {
                            // Create the (:Okta_Application)-[:Okta_UserPull]->(:Okta_User) edge
                            _graph.AddEdge(appNode, oktaUser, OktaUser.UserPullEdgeKind);
                        }
                        else if (!appNode.IsOutboundSyncIgnored)
                        {
                            // Create the (:Okta_User)-[:Okta_UserPush]->(:Okta_Application) hybrid edge
                            _graph.AddEdge(oktaUser, appNode, OktaUser.UserPushEdgeKind);
                        }
                    }

                    // SSO edges for SAML, OIDC, and SWA app users.
                    // For users that are not synchronized using SCIM and do not actually exist in the target tenant,
                    // these edges might become disconnected (missing target nodes). This is expected.
                    OpenGraphEdge? hybridAuthEdge = appNode.CreateHybridUserSignOnEdge(appUserAssignment.Id, targetUserName);

                    if (hybridAuthEdge is not null)
                    {
                        // Add the Okta_OutboundSSO or Okta_SWA edge to the graph.
                        // Example: (:Okta_User)-[:Okta_OutboundSSO]->(:jamf_jamf_Account)
                        // Example: (:Okta_User)-[:Okta_SWA]->(:jamf_jamf_Account)
                        // Example: (:Okta_User)-[:Okta_OutboundSSO]->(:GHUser)
                        _logger.LogTrace("User {UserId} is mapped as {TargetUserName} in the {AppName} application",
                            appUserAssignment.Id,
                            targetUserName,
                            appNode.Name);
                        _hybridEdgeGraph.AddEdge(hybridAuthEdge);
                    }

                    // The (:Okta_Group)-[:Okta_AppAssignment]->(:Okta_Application) edges are handled separately.
                    if (appUserAssignment.Scope == AppUser.ScopeEnum.USER && !appNode.IsAssignmentIgnored)
                    {
                        // TODO: Consider only active user assignments?
                        _logger.LogTrace("User {UserId} is assigned the {AppName} ({AppId}) application.", appUserAssignment.Id, appNode.Name, appNode.Id);

                        // Create the (:Okta_User)-[:Okta_AppAssignment]->(:Okta_Application) edge
                        _graph.AddEdge(oktaUser, appNode, OktaApplication.AppAssignmentEdgeKind);
                    }

                    if (appNode.SupportsPasswordUpdates)
                    {
                        // Create the (:Okta_Application)-[:Okta_ReadPasswordUpdates]->(:Okta_User) edge
                        _graph.AddEdge(appNode, oktaUser, OktaApplication.ReadPasswordUpdatesEdgeKind);
                    }
                }

                if (domainSid is not null && appNode.ActiveDirectoryDomainSid is null)
                {
                    // Add AD domain SID, so that the Domain node can later be created.
                    appNode.ActiveDirectoryDomainSid = domainSid;
                }

                _logger.LogTrace("Finished fetching user assignments for application {AppName} ({AppId}).", appNode.Name, appNode.Id);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta app {AppName} ({AppId}) user assignments.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application user assignments.");
    }

    public async Task FetchOktaAppGroupAssignments(CancellationToken cancellationToken = default)
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

    public async Task FetchOktaAppGroupPushMappings(CancellationToken cancellationToken = default)
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

    public async Task FetchOktaResourceSetMemberships(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching resource set memberships...");

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
                _logger.LogDebug("Fetching membership of the {ResourceSetName} ({ResourceSetId}) resource set...", resourceSetNode.Name, resourceSetNode.OriginalId);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                RoleCResourceSetResourceApi resourceSetApi = new(_oktaConfig);

                // TODO: Implement resource set pagination if needed
                ResourceSetResources resources = await resourceSetApi.ListResourceSetResourcesAsync(resourceSetNode.OriginalId, cancellationToken).ConfigureAwait(false);

                foreach (ResourceSetResource resource in resources.Resources)
                {
                    string? resourceUrl = resource.Links?.Self?.Href;

                    if (resourceUrl is null)
                    {
                        // Several resource types, including "Identity and access management",
                        // "Workflows", "Customizations", or "Support Cases", cannot be resolved using the SDK.
                        // Example "orn": "orn:okta:iam:00ow0o8if0CNwsKmk697:contained_resources"
                        // Example "orn": "orn:okta:support:00ow0o8if0CNwsKmk697:cases"
                        // Example "orn": "orn:okta:workflow:00ow0o8if0CNwsKmk697:flows"
                        // Example "orn": "orn:okta:idp:00ow0o8if0CNwsKmk697:customizations"

                        // TODO: File an SDK bug request for missing ORN in the result.

                        // Skip processing this resource set assignment
                        continue;
                    }

                    foreach (var memberNode in ResolveResourceSetMembership(resourceUrl))
                    {
                        _logger.LogTrace(
                            "Resource set {ResourceSetName} ({ResourceSetId}) contains member {MemberId}.",
                            resourceSetNode.Name,
                            resourceSetNode.OriginalId,
                            memberNode.Value);

                        // Create the (:Okta)-[:Okta_ResourceSetContains]->(:Okta_ResourceSet) edge
                        _graph.AddEdge(resourceSetNode, memberNode, OktaResourceSet.ContainsEdgeKind);
                    }
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the membership of the {ResourceSetName} ({ResourceSetId}) resource set.", e.ErrorCode, status, resourceSetNode.Name, resourceSetNode.OriginalId);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching resource set memberships.");
    }

    public IEnumerable<OpenGraphEdgeNode> ResolveResourceSetMembership(string resourceUrl)
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

    public async Task FetchOktaCustomRolePermissions(CancellationToken cancellationToken = default)
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

    public async Task FetchOktaPrivilegedUsers(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching the list of users with role assignments...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            int privilegedUserCount = 0;
            RoleAssignmentAUserApi roleAssignmentApi = new(_oktaConfig);

            // TODO: Paginate results if needed
            var privilegedUsers = await roleAssignmentApi.ListUsersWithRoleAssignmentsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (RoleAssignedUser privilegedUser in privilegedUsers?.Value ?? [])
            {
                privilegedUserCount++;

                // Mark the user as privileged in the graph
                OktaUser? userNode = _graph.GetUserById(privilegedUser.Id);

                if (userNode is not null)
                {
                    _logger.LogDebug("User {UserName} ({UserId}) has a role assigned.", userNode.Name, userNode.Id);
                    userNode.HasRoleAssignments = true;
                }
                else
                {
                    _logger.LogWarning(
                        "Could not find user {UserId} in the graph while processing the list of users with role assignments. The user might have been deactivated.",
                        privilegedUser.Id);
                }
            }

            _logger.LogInformation("Successfully processed {PrivilegedUserCount} users with role assignments.", privilegedUserCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while fetching the list of users with role assignments.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaPolicyRules(CancellationToken cancellationToken = default)
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
    public async Task FetchOktaPolicyMappings(CancellationToken cancellationToken = default)
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

    public async Task FetchOktaApplicationSecrets(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application secrets...");

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

        // Only the secrets of service applications may be used in direct attack paths
        await Parallel.ForEachAsync(_graph.Applications.Where(app => app.IsService), concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching the list of secrets configured for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                ApplicationSSOPublicKeysApi ssoApi = new(_oktaConfig);

                await foreach (var secret in ssoApi.ListOAuth2ClientSecrets(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("The {AppName} ({AppId}) application has an {Status} client secret {SecretId}.", appNode.Name, appNode.Id, secret.Status?.Value?.ToLowerInvariant(), secret.Id);

                    // Create the OktaClientSecret node
                    OktaClientSecret secretNode = new(secret, _graph.Organization.DomainName);
                    _graph.AddNode(secretNode);

                    // Create the (:Okta_ClientSecret)-[:Okta_SecretOf]->(:Okta_Application) edge
                    _graph.AddEdge(secretNode, appNode, OktaClientSecret.SecretOfEdgeKind);
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list secrets for the {AppName} ({AppId}) application.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application client secrets.");
    }

    public async Task FetchOktaApplicationJsonWebKeys(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching application JSON Web Keys...");

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

        // Only the JWT keys of service applications may be used in direct attack paths
        await Parallel.ForEachAsync(_graph.Applications.Where(app => app.IsService), concurrency, async (appNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching the list of JSON Web Keys configured for the {AppName} ({AppId}) application...", appNode.Name, appNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                ApplicationSSOPublicKeysApi publicKeyApi = new(_oktaConfig);
                OAuth2ClientJsonWebKeySet appKeys = await publicKeyApi.ListJwkAsync(appNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var jwk in appKeys.Keys ?? [])
                {
                    // We are not interested in encryption keys
                    if (jwk.ActualInstance is OAuth2ClientJsonSigningKeyResponse signingKey)
                    {
                        OktaJWK? keyNode = null;

                        if (signingKey.ActualInstance is OAuth2ClientJsonWebKeyECResponse ecKey)
                        {
                            keyNode = new(ecKey, _graph.Organization.DomainName);
                        }
                        else if (signingKey.ActualInstance is OAuth2ClientJsonWebKeyRsaResponse rsaKey)
                        {
                            keyNode = new(rsaKey, _graph.Organization.DomainName);
                        }
                        else
                        {
                            _logger.LogWarning("The {AppName} ({AppId}) application has a JSON Web Key with an unsupported key type ({KeyType}).", appNode.Name, appNode.Id, signingKey.GetType().Name);
                            continue;
                        }

                        _logger.LogTrace("The {AppName} ({AppId}) application has a JSON Web Key ({KeyId}).", appNode.Name, appNode.Id, keyNode.Id);

                        // Create the OktaJWK node
                        _graph.AddNode(keyNode);

                        // Create the (:Okta_JWK)-[:Okta_KeyOf]->(:Okta_Application) edge
                        _graph.AddEdge(keyNode, appNode, OktaJWK.KeyOfEdgeKind);
                    }
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list JSON Web Keys for the {AppName} ({AppId}) application.", e.ErrorCode, status, appNode.Name, appNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching application JSON Web Keys.");
    }

    public async Task FetchOktaApiServiceIntegrationSecrets(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API service integration secrets...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.ApiServiceIntegrations, concurrency, async (serviceNode, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching the list of secrets configured for the {ServiceName} ({ServiceId}) API service integration...", serviceNode.Name, serviceNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                ApiServiceIntegrationsApi integrationsApi = new(_oktaConfig);

                await foreach (var secret in integrationsApi.ListApiServiceIntegrationInstanceSecrets(serviceNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("The {ServiceName} ({ServiceId}) API service integration has an {Status} client secret {SecretId}.", serviceNode.Name, serviceNode.Id, secret.Status?.Value?.ToLowerInvariant(), secret.Id);

                    if (secret.Status != APIServiceIntegrationInstanceSecret.StatusEnum.ACTIVE)
                    {
                        // TODO: Consider adding inactive client secrets as well
                        continue;
                    }

                    // Create the OktaClientSecret node
                    OktaClientSecret secretNode = new(secret, _graph.Organization.DomainName);
                    _graph.AddNode(secretNode);

                    // Create the (:Okta_ClientSecret)-[:Okta_SecretOf]->(:Okta_ApiServiceIntegration) edge
                    _graph.AddEdge(secretNode, serviceNode, OktaClientSecret.SecretOfEdgeKind);
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to list secrets for the {ServiceName} ({ServiceId}) API service integration.", e.ErrorCode, status, serviceNode.Name, serviceNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching API service integration secrets.");
    }

    public async Task FetchOktaGroupMemberships(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching group memberships...");

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
                _logger.LogDebug("Fetching the list of members for group {GroupName} ({GroupId})...", groupNode.Name, groupNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                GroupApi groupApi = new(_oktaConfig);

                await foreach (var groupMember in groupApi.ListGroupUsers(groupNode.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogTrace("User {UserId} is a member of the {GroupName} ({GroupId}) group.", groupMember.Id, groupNode.Name, groupNode.Id);

                    // Create the (:Okta_User)-[:Okta_MemberOf]->(:Okta_Group) edge
                    var memberNode = OktaUser.CreateEdgeNode(groupMember.Id);
                    _graph.AddEdge(memberNode, groupNode, OktaGroup.MemberOfEdgeKind);
                }
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch group {GroupName} ({GroupId}) members.", e.ErrorCode, status, groupNode.Name, groupNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching group memberships.");
    }

    public async Task FetchOktaUserAuthenticationFactors(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user authentication factors...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_graph.Users, concurrency, async (userNode, cancellationToken) =>
        {
            try
            {
                _logger.LogTrace("Fetching the list of authentication factors for user {UserName} ({UserId})...", userNode.Name, userNode.Id);

                // The API client is not thread-safe, so it needs to be instantiated for each thread
                UserFactorApi authFactorsApi = new(_oktaConfig);

                // Get user's authentication factors count
                int count = 0;

                await foreach (var factor in authFactorsApi.ListFactors(userNode.Id, cancellationToken).ConfigureAwait(false))
                {
                    count++;
                }

                _logger.LogTrace("User {UserName} has {FactorCount} authentication factors.", userNode.Name, count);
                userNode.Properties["authenticationFactors"] = count;
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;
                _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch authentication factors for user {UserName} ({UserId}).", e.ErrorCode, status, userNode.Name, userNode.Id);
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Finished fetching user authentication factors.");
    }

    public async Task FetchOktaIdentityProviderUsers(CancellationToken cancellationToken = default)
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
                        // TODO: Optionally augment userPrincipalName matching with TenantId
                        OpenGraphEdgeNode entraUserNode = new(idpUser.ExternalId, "AZUser", matchBy: "name");
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
