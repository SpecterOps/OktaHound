using System.Net;
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
    public async Task<OktaOrganization?> FetchOktaOrganization(CancellationToken cancellationToken = default)
    {
        try
        {
            if (this._oktaConfig.OktaDomain == DefaultOktaDomain)
            {
                _logger.LogCritical("Using default Okta domain {DefaultOktaDomain}. Please update the okta.yaml file with your Okta domain.", DefaultOktaDomain);

                // Do not continue with the default settings
                return null;
            }

            _logger.LogDebug(
                "Authenticating to {OktaDomain} using {AuthorizationMode}...",
                _oktaConfig.OktaDomain,
                _oktaConfig.AuthorizationMode);
            _logger.LogInformation("Fetching Okta organization information...");

            // Fetch organization information
            OrgSettingGeneralApi orgApi = new(_oktaConfig);
            OrgSetting orgSettings = await orgApi.GetOrgSettingsAsync(cancellationToken).ConfigureAwait(false);

            // Extract the domain name from the Okta URL
            string domainName = new Uri(_oktaConfig.OktaDomain).Host;

            // Return the OktaOrganization node
            OktaOrganization orgNode = new(orgSettings, domainName);
            return orgNode;
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the Okta organization settings.", e.ErrorCode, status);
            return null;
        }
    }

    public async Task FetchOktaUsers(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching users...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            UserApi userApi = new(_oktaConfig);
            int userCount = 0;

            await foreach (var user in userApi.ListUsers(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing user {Login} ({UserId})...", user.Profile.Login, user.Id);
                userCount++;

                // Create the OktaUser node
                OktaUser userNode = new(user, _graph.Organization.DomainName);
                _graph.AddNode(userNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_User) edge
                _graph.AddEdge(_graph.Organization, userNode, OktaOrganization.ContainsEdgeKind);

                if (userNode.RealmId is not null)
                {
                    // Create the (:Okta_Realm)-[:Okta_RealmContains]->(:Okta_User) edge if the user belongs to a realm
                    _logger.LogTrace("User {Login} belongs to realm {RealmId}.", userNode.Login, userNode.RealmId);
                    var realmNode = OktaRealm.CreateEdgeNode(userNode.RealmId);
                    _graph.AddEdge(realmNode, userNode, OktaRealm.RealmContainsEdgeKind);
                }
            }

            _logger.LogInformation("Successfully processed {UserCount} users.", userCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta users.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaGroups(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching groups...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            GroupApi groupApi = new(_oktaConfig);
            int groupCount = 0;

            await foreach (var group in groupApi.ListGroups(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing group {GroupName} ({GroupId})...", GetGroupName(group), group.Id);
                groupCount++;

                // Create the OktaGroup node
                OktaGroup groupNode = new(group, _graph.Organization.DomainName);
                _graph.AddNode(groupNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Group) edge
                OpenGraphEdgeNode groupEdgeNode = groupNode.ToEdgeNode();
                _graph.AddEdge(_graph.Organization, groupEdgeNode, OktaOrganization.ContainsEdgeKind);

                // Check if the group is synced from AD, in which case we know its SID
                if (groupNode.IsActiveDirectoryGroup)
                {
                    if (groupNode.DomainSid is null || groupNode.ObjectSid is null)
                    {
                        _logger.LogWarning("Could not get SID of the {GroupName} ({GroupId}) synced from AD.", groupNode.Name, groupNode.Id);
                        continue;
                    }

                    _logger.LogTrace("Group {GroupName} ({GroupSid}) is synchronized from Active Directory.", groupNode.SamAccountName, groupNode.ObjectSid);

                    // Create the Group AD node
                    ActiveDirectoryGroup adGroup = new ActiveDirectoryGroup(groupNode);
                    _adGraph.AddNode(adGroup);

                    // Create the (:Domain)-[:Contains]->(:Group) AD edge
                    var domainNode = ActiveDirectoryDomain.CreateEdgeNode(groupNode.DomainSid);
                    _adGraph.AddEdge(domainNode, adGroup, ActiveDirectoryDomain.ContainsEdgeKind);

                    // Create the (:Group)-[:Okta_MembershipSync]->(:Okta_Group) hybrid edge
                    _hybridEdgeGraph.AddEdge(adGroup, groupEdgeNode, OktaGroup.MembershipSyncEdgeKind);
                }

                if (groupNode.SourceApplicationId is not null)
                {
                    // Handle incoming membership sync from an application
                    _logger.LogTrace("Group {GroupName} ({GroupId}) is synchronized from application {SourceApplicationId}.", groupNode.Name, groupNode.Id, groupNode.SourceApplicationId);

                    // Find the source app. This assumes that apps are loaded before groups.
                    OktaApplication? appNode = _graph.GetApplication(groupNode.SourceApplicationId);
                    if (appNode is null)
                    {
                        _logger.LogWarning("Could not find the source application {SourceApplicationId} for group {GroupName} ({GroupId}).", groupNode.SourceApplicationId, groupNode.Name, groupNode.Id);
                        continue;
                    }

                    // Create the (:Okta_Application)-[:Okta_GroupPull]->(:Okta_Group) edge
                    _graph.AddEdge(appNode, groupEdgeNode, OktaGroup.GroupPullEdgeKind);

                    if (group.Profile.ActualInstance is OktaUserGroupProfile sourceGroupProfile)
                    {
                        // Create a hybrid edge for non-AD apps (AD groups are already handled above)
                        // Example: (:AZGroup)-[:Okta_MembershipSync]->(:Okta_Group)
                        // TODO: (:SNOWGroup)-[:Okta_MembershipSync]->(:Okta_Group)
                        // TODO: (:Okta_Group)-[:Okta_MembershipSync]->(:Okta_Group)
                        OpenGraphEdgeNode? hybridGroup = appNode.CreateHybridGroupEdgeNode(sourceGroupProfile);
                        if (hybridGroup is not null)
                        {
                            _hybridEdgeGraph.AddEdge(hybridGroup, groupEdgeNode, OktaGroup.MembershipSyncEdgeKind);
                        }
                    }
                }

                // Group outbound sync is handled when processing application group push mappings.
            }

            _logger.LogInformation("Successfully processed {GroupCount} groups.", groupCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta groups.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaDevices(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching devices...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            DeviceApi deviceApi = new(_oktaConfig);
            int deviceCount = 0;

            await foreach (var device in deviceApi.ListDevices(expand: DeviceExpandParameter.UserSummary, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing device {DeviceName} ({DeviceId})...", device.Profile.DisplayName, device.Id);
                deviceCount++;

                // Create the OktaDevice node
                OktaDevice deviceNode = new(device, _graph.Organization.DomainName);
                _graph.AddNode(deviceNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Device) edge
                _graph.AddEdge(_graph.Organization, deviceNode, OktaOrganization.ContainsEdgeKind);

                // Link the device to its owner
                foreach (var user in device.Embedded.Users ?? [])
                {
                    _logger.LogTrace("The {DeviceName} device is owned by user {UserId}.", device.Profile.DisplayName, user.User.Id);

                    // Create the (:Okta_Device)-[:Okta_DeviceOf]->(:Okta_User) edge
                    var userNode = OktaUser.CreateEdgeNode(user.User.Id);
                    _graph.AddEdge(deviceNode, userNode, OktaDevice.DeviceOfEdgeKind);
                }
            }

            _logger.LogInformation("Successfully processed {DeviceCount} devices.", deviceCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta devices.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaResourceSets(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching custom resource sets...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            RoleCResourceSetApi resourceSetApi = new(_oktaConfig);
            int resourceSetCount = 0;

            await foreach (ResourceSet resourceSet in resourceSetApi.ListAllResourceSets(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing resource set {ResourceSetLabel} ({ResourceSetId})...", resourceSet.Label, resourceSet.Id);
                resourceSetCount++;

                // Create the OktaResourceSet node
                OktaResourceSet resourceSetNode = new(resourceSet, _graph.Organization.DomainName);
                _graph.AddNode(resourceSetNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_ResourceSet) edge
                _graph.AddEdge(_graph.Organization, resourceSetNode, OktaOrganization.ContainsEdgeKind);
            }

            _logger.LogInformation("Successfully processed {ResourceSetCount} resource sets.", resourceSetCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta resource sets.", e.ErrorCode, status);
        }
    }


    public async Task FetchOktaRealms(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching realms...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            RealmApi realmApi = new(_oktaConfig);
            RealmAssignmentApi realmAssignmentApi = new(_oktaConfig);
            int realmCount = 0;

            await foreach (var realm in realmApi.ListRealms(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing realm {RealmName} ({RealmId})...", realm.Profile.Name, realm.Id);
                realmCount++;

                // Create the OktaRealm node
                OktaRealm realmNode = new(realm, _graph.Organization.DomainName);
                _graph.AddNode(realmNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Realm) edge
                _graph.AddEdge(_graph.Organization, realmNode, OktaOrganization.ContainsEdgeKind);
            }

            _logger.LogInformation("Successfully processed {RealmCount} realms.", realmCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;

            switch (status)
            {
                // Error 403: okta.realms.read permission not granted, perhaps of missing license
                case HttpStatusCode.Forbidden:
                // Error 401: okta.realms.read permission granted, but the feature is not licensed
                case HttpStatusCode.Unauthorized:
                    _logger.LogInformation("Could not list realms. The feature might not be licensed.");
                    break;
                default:
                    // Treat any other status code as Error instead of Warning
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta realms.", e.ErrorCode, status);
                    break;
            }
        }
    }

    public async Task FetchOktaBuiltInRoles(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching built-in roles...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        int roleCount = 0;

        ParallelOptions concurrency = new()
        {
            MaxDegreeOfParallelism = _concurrentApiCalls,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(OktaBuiltinRole.BuiltInRoles, concurrency, async (roleId, cancellationToken) =>
        {
            try
            {
                _logger.LogDebug("Fetching info about the built-in role {RoleId}...", roleId);
                RoleECustomApi roleApi = new(_oktaConfig);

                var role = await roleApi.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);

                // Create the OktaRole node
                var roleNode = new OktaBuiltinRole(role, _graph.Organization.DomainName);
                _graph.AddNode(roleNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Role) edge
                _graph.AddEdge(_graph.Organization, roleNode, OktaOrganization.ContainsEdgeKind);

                // Count the number of successfully processed roles
                Interlocked.Increment(ref roleCount);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                if (status == HttpStatusCode.NotFound)
                {
                    // Some built-in roles may be deprecated and return 404
                    _logger.LogDebug("Could not load the {RoleId} role. The corresponding feature might not be licensed or enabled.", roleId);
                }
                else
                {
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to load the {RoleId} role.", e.ErrorCode, status, roleId);
                }
            }
        }).ConfigureAwait(false);

        if (roleCount > 0)
        {
            _logger.LogInformation("Successfully processed {RoleCount} built-in roles.", roleCount);
        }
        else
        {
            _logger.LogCritical("No built-in roles were processed. Please ensure that the API token has the required permissions.");
        }
    }

    public async Task FetchOktaCustomRoles(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching custom roles...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            RoleECustomApi roleApi = new(_oktaConfig);
            int roleCount = 0;

            await foreach (IamRole role in roleApi.ListAllRoles(cancellationToken).ConfigureAwait(false))
            {
                if (OktaBuiltinRole.BuiltInRoles.Contains(role.Id))
                {
                    // Skip built-in roles, as the API typically returns WORKFLOWS_ADMIN as a custom role.
                    continue;
                }

                _logger.LogDebug("Processing custom role {RoleLabel} ({RoleId})...", role.Label, role.Id);
                roleCount++;

                // Create the OktaCustomRole node
                var roleNode = new OktaCustomRole(role, _graph.Organization.DomainName);
                _graph.AddNode(roleNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_CustomRole) edge
                _graph.AddEdge(_graph.Organization, roleNode, OktaOrganization.ContainsEdgeKind);
            }

            _logger.LogInformation("Successfully processed {RoleCount} custom roles.", roleCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta custom roles.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaApplications(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching applications...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            ApplicationApi appApi = new(_oktaConfig);
            int applicationCount = 0;

            await foreach (var app in appApi.ListApplications(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing application {AppLabel} ({AppId})...", app.Label, app.Id);
                applicationCount++;

                // Create the OktaApplication node
                OktaApplication appNode = new(app, _graph.Organization.DomainName);
                _graph.AddNode(appNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Application) edge
                var appEdgeNode = appNode.ToEdgeNode();
                _graph.AddEdge(_graph.Organization, appEdgeNode, OktaOrganization.ContainsEdgeKind);

                // Try to add the Okta_OutboundOrgSSO edge for SAML and OIDC apps.
                // A well-known BloodHound collector must exist for the target technology.
                // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:jamf_Tenant)
                // Example: (:Okta_Application)-[:Okta_OutboundOrgSSO]->(:GHOrganization)
                OpenGraphEdge? outboundTrustEdge = appNode.CreateOutboundTrustEdge();

                if (outboundTrustEdge is not null)
                {
                    _hybridEdgeGraph.AddEdge(outboundTrustEdge);
                }
            }

            _logger.LogInformation("Successfully processed {ApplicationCount} applications.", applicationCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta applications.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaApiTokens(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API tokens...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            ApiTokenApi tokenApi = new(_oktaConfig);
            int tokenCount = 0;

            await foreach (var token in tokenApi.ListApiTokens(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing API token {TokenName} ({TokenId})...", token.Name, token.Id);
                tokenCount++;

                // Create the OktaApiToken node
                OktaApiToken tokenNode = new(token, _graph.Organization.DomainName);
                _graph.AddNode(tokenNode);

                // Create edge (:Okta_ApiToken)-[:Okta_ApiTokenFor]->(:Okta_User)
                OpenGraphEdgeNode userNode = OktaUser.CreateEdgeNode(token.UserId);
                _graph.AddEdge(tokenNode, userNode, OktaApiToken.ApiTokenForEdgeKind);
            }

            _logger.LogInformation("Successfully processed {TokenCount} API tokens.", tokenCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta API tokens.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaAgentPools(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching agent pools...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            AgentPoolsApi agentPoolsApi = new(_oktaConfig);
            int agentPoolCount = 0;

            await foreach (var agentPool in agentPoolsApi.ListAgentPools(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing agent pool {AgentPoolName} ({AgentPoolId})...", agentPool.Name, agentPool.Id);
                agentPoolCount++;

                // Create the OktaAgentPool node
                OktaAgentPool agentPoolNode = new(agentPool, _graph.Organization.DomainName);
                _graph.AddNode(agentPoolNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_AgentPool) edge
                _graph.AddEdge(_graph.Organization, agentPoolNode, OktaOrganization.ContainsEdgeKind);

                if (agentPoolNode.IsActiveDirectoryAgentPool)
                {
                    _logger.LogTrace("Agent pool {AgentPoolName} ({AgentPoolId}) is an Active Directory agent pool.", agentPoolNode.Name, agentPoolNode.OriginalId);

                    // Add the (:OktaAgentPool)-[:Okta_AgentPoolFor]->(:Okta_Application) edge
                    var adApplicationNode = OktaApplication.CreateEdgeNode(agentPoolNode.OriginalId);
                    _graph.AddEdge(agentPoolNode, adApplicationNode, OktaAgentPool.AgentPoolForEdgeKind);

                    // Opportunistically create the Agentless Desktop SSO relationship
                    // TODO: Enable the DNS check
                    var onPremSsoAccount = ActiveDirectoryUser.CreateKerberosEdgeNode(_graph.Organization.DomainName, agentPoolNode.ActiveDirectoryDomain!, dnsCheck: false);

                    if (onPremSsoAccount is not null)
                    {
                        // Create the (:User)-[:Okta_KerberosSSO]->(:Okta_Application) edge for the on-prem SSO account
                        _hybridEdgeGraph.AddEdge(onPremSsoAccount, adApplicationNode, OktaAgentPool.AgentlessDesktopSSOEdgeKind);
                    }
                }

                int agentCount = 0;

                foreach (var agent in agentPool.Agents ?? [])
                {
                    // Process each agent in the current agent pool
                    _logger.LogDebug("Processing agent {AgentName} ({AgentId})...", agent.Name, agent.Id);
                    agentCount++;

                    // Create the OktaAgent node
                    OktaAgent agentNode = new(agent, agentPool.Name, _graph.Organization.DomainName);
                    _graph.AddNode(agentNode);

                    // Create the (:Okta_Agent)-[:Okta_AgentMemberOf]->(:Okta_AgentPool) edge
                    _graph.AddEdge(agentNode, agentPoolNode, OktaAgent.HasAgentMemberOfEdgeKind);

                    if (agentPoolNode.IsActiveDirectoryAgentPool)
                    {
                        // Create the hybrid (:Computer)-[:Okta_HostsAgent]->(:Okta_Agent) edge
                        var computerNode = ActiveDirectoryComputer.CreateEdgeNode(agent.Name, agentPool.Name);
                        _hybridEdgeGraph.AddEdge(computerNode!, agentNode, OktaAgent.HostsAgentEdgeKind);
                    }
                }

                _logger.LogTrace("Successfully processed {AgentCount} agents in pool {AgentPoolName}.", agentCount, agentPool.Name);
            }

            _logger.LogInformation("Successfully processed {AgentPoolCount} agent pools.", agentPoolCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta agent pools.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaAuthorizationServers(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching authorization servers...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            AuthorizationServerApi authorizationServerApi = new(_oktaConfig);
            int authorizationServerCount = 0;

            await foreach (var authorizationServer in authorizationServerApi.ListAuthorizationServers(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing authorization server {AuthorizationServerName} ({AuthorizationServerId})...", authorizationServer.Name, authorizationServer.Id);
                authorizationServerCount++;

                // Create the OktaAuthorizationServer node
                OktaAuthorizationServer authorizationServerNode = new(authorizationServer, _graph.Organization.DomainName);
                _graph.AddNode(authorizationServerNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_AuthorizationServer) edge
                _graph.AddEdge(_graph.Organization, authorizationServerNode, OktaOrganization.ContainsEdgeKind);

                // TODO: List associated trusted servers
                // AuthorizationServerAssocApi authorizationServerAssocApi = new(_oktaConfig);
                // authorizationServerAssocApi.ListAssociatedServersByTrustedType(authorizationServerNode.Id, trusted: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully processed {AuthorizationServerCount} authorization servers.", authorizationServerCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta authorization servers.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaIdentityProviders(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching identity providers...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            IdentityProviderApi identityProviderApi = new(_oktaConfig);
            int identityProviderCount = 0;

            await foreach (var identityProvider in identityProviderApi.ListIdentityProviders(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing identity provider {IdentityProviderName} ({IdentityProviderId})...", identityProvider.Name, identityProvider.Id);
                identityProviderCount++;

                // Create the OktaIdentityProvider node
                OktaIdentityProvider identityProviderNode = new(identityProvider, _graph.Organization.DomainName);
                _graph.AddNode(identityProviderNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_IdentityProvider) edge
                _graph.AddEdge(_graph.Organization, identityProviderNode, OktaOrganization.ContainsEdgeKind);

                // Create the (:Okta_IdentityProvider)-[:Okta_IdpGroupAssignment]->(:Okta_Group) edges
                foreach (var groupId in identityProviderNode.GovernedGroupIds)
                {
                    _logger.LogTrace("The {IdentityProviderName} identity provider manages the membership of the {GroupId} group.", identityProvider.Name, groupId);

                    var groupNode = OktaGroup.CreateEdgeNode(groupId);
                    _graph.AddEdge(identityProviderNode, groupNode, OktaIdentityProvider.AutoGroupAssignmentEdgeKind);
                }

                // For Entra ID tenants, create the (:AZTenant)-[:Okta_InboundOrgSSO]->(:Okta_IdentityProvider) edge
                if (identityProviderNode.TenantId is not null)
                {
                    _logger.LogTrace("The {IdentityProviderName} identity provider is an Entra ID tenant.", identityProvider.Name);
                    var entraTenantNode = EntraIdTenant.CreateEdgeNode(identityProviderNode.TenantId);
                    _hybridEdgeGraph.AddEdge(entraTenantNode, identityProviderNode, OktaIdentityProvider.InboundOrgSSOEdgeKind);
                }
            }

            _logger.LogInformation("Successfully processed {IdentityProviderCount} identity providers.", identityProviderCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta identity providers.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaApiServiceIntegrations(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API service integrations...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        try
        {
            ApiServiceIntegrationsApi apiServiceApi = new(_oktaConfig);
            int serviceCount = 0;

            await foreach (var service in apiServiceApi.ListApiServiceIntegrationInstances(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing API service integration {ServiceName} ({ServiceId})...", service.Name, service.Id);
                serviceCount++;

                // Create the OktaApiServiceIntegration node
                OktaApiServiceIntegration serviceNode = new(service, _graph.Organization.DomainName);
                _graph.AddNode(serviceNode);

                // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_ApiServiceIntegration) edge
                _graph.AddEdge(_graph.Organization, serviceNode, OktaOrganization.ContainsEdgeKind);

                if (service.CreatedBy is not null)
                {
                    _logger.LogTrace("Setting {CreatedBy} as the creator of the {ServiceName} ({ServiceId}) API service integration...", service.CreatedBy, service.Name, service.Id);

                    // Create the (:Okta)-[:Okta_CreatorOf]->(:Okta_ApiServiceIntegration) edge
                    // Although not tested, the creator could probably not only be a user, but an application as well.
                    OpenGraphEdgeNode createdByNode = new(service.CreatedBy, OktaGraph.OktaSourceKind);
                    _graph.AddEdge(createdByNode, serviceNode, OktaApiServiceIntegration.CreatorOfEdgeKind);
                }

                // TODO: Add attack path edges for service integrations
            }

            _logger.LogInformation("Successfully processed {ServiceCount} API service integrations.", serviceCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta API service integrations.", e.ErrorCode, status);
        }
    }

    public async Task FetchOktaPolicies(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching policies...");

        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        PolicyApi policyApi = new(_oktaConfig);
        int policyCount = 0;

        foreach (PolicyTypeParameter policyType in OktaPolicy.PolicyTypes)
        {
            try
            {
                _logger.LogDebug("Fetching {PolicyType} policies...", policyType.Value);

                await foreach (var policy in policyApi.ListPolicies(policyType, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Processing policy {PolicyName} ({PolicyId})...", policy.Name, policy.Id);
                    policyCount++;

                    // Create the OktaPolicy node
                    OktaPolicy policyNode = new(policy, _graph.Organization.DomainName);
                    _graph.AddNode(policyNode);

                    // Create the (:Okta_Organization)-[:Okta_Contains]->(:Okta_Policy) edge
                    _graph.AddEdge(_graph.Organization, policyNode, OktaOrganization.ContainsEdgeKind);
                }

                _logger.LogTrace("Successfully processed {PolicyType} policies.", policyType.Value);
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                if (status == HttpStatusCode.BadRequest)
                {
                    // Error 400 is expected, as not all policy types are present.
                    _logger.LogTrace("Could not fetch {PolicyType} policies. No such policy is available.", policyType.Value);
                }
                else
                {
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch {PolicyType} policies.", e.ErrorCode, status, policyType.Value);
                }
            }
        }

        _logger.LogInformation("Successfully processed {PolicyCount} policies.", policyCount);
    }

    public async Task FetchOktaLogs(CancellationToken cancellationToken = default)
    {
        if (_graph is null)
        {
            throw new InvalidOperationException("Okta graph not initialized. Please call InitializeOktaGraph() first.");
        }

        SystemLogApi logApi = new(_oktaConfig);
        int eventCount = 0;

        try
        {
            _logger.LogInformation("Fetching system log events...");

            // TODO: Apply system log filters
            await foreach (var oktaEvent in logApi.ListLogEvents(sortOrder: SortOrderParameter.DESCENDING, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                eventCount++;

                // TODO: Process log events
            }

            _logger.LogInformation("Successfully processed {EventCount} system log events.", eventCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch system log events.", e.ErrorCode, status);
        }
    }

    private string GetGroupName(Group group)
    {
        if (group.Profile.ActualInstance is OktaUserGroupProfile groupProfile)
        {
            return groupProfile.Name;
        }
        else if (group.Profile.ActualInstance is OktaActiveDirectoryGroupProfile adGroupProfile)
        {
            return adGroupProfile.Name;
        }
        else
        {
            // This could theoretically happen if another group type is added to the API.
            _logger.LogError("Could not parse the name of group {GroupId}.", group.Id);
            return "<unknown>";
        }
    }

}
