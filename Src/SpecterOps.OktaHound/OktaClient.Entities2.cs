using System.Net;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Database;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task<bool> CollectOrganization(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteOrganizations(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (this._oktaConfig.OktaDomain == DefaultOktaDomain)
            {
                _logger.LogCritical("Using default Okta domain {DefaultOktaDomain}. Please update the okta.yaml file with your Okta domain.", DefaultOktaDomain);

                // Do not continue with the default settings
                return false;
            }

            _logger.LogDebug(
                "Authenticating to {OktaDomain} using {AuthorizationMode}...",
                _oktaConfig.OktaDomain,
                _oktaConfig.AuthorizationMode);
            _logger.LogInformation("Fetching Okta organization information...");

            // Fetch organization information
            OrgSettingGeneralApi orgApi = new(_oktaConfig);
            OrgSetting orgSettings = await orgApi.GetOrgSettingsAsync(cancellationToken).ConfigureAwait(false);

            // Create the Okta_Organization node
            OktaOrganization orgNode = new(orgSettings, OktaDomain);
            dbContext.Organizations.Add(orgNode);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully retrieved Okta organization information for {OrganizationName}.", orgNode.DisplayName);

            // Success
            return true;
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch the Okta organization settings.", e.ErrorCode, status);
            return false;
        }
    }

    public async Task CollectUsers(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching users...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteUsers(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            UserApi userApi = new(_oktaConfig);
            int userCount = 0;

            await foreach (var user in userApi.ListUsers(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing user {Login} ({UserId})...", user.Profile.Login, user.Id);
                userCount++;

                // Create the Okta_User node
                OktaUser userNode = new(user, OktaDomain);
                dbContext.Users.Add(userNode);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {UserCount} users.", userCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta users.", e.ErrorCode, status);
        }
    }

    public async Task CollectGroups(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching groups...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteGroups(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            GroupApi groupApi = new(_oktaConfig);
            int groupCount = 0;

            await foreach (var group in groupApi.ListGroups(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing group {GroupName} ({GroupId})...", GetGroupName(group), group.Id);
                groupCount++;

                // Create the Okta_Group node
                OktaGroup groupNode = new(group, OktaDomain);
                dbContext.Groups.Add(groupNode);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {GroupCount} groups.", groupCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta groups.", e.ErrorCode, status);
        }
    }

    public async Task CollectAgentPools(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching agent pools...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteAgentPools(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            AgentPoolsApi agentPoolsApi = new(_oktaConfig);
            int agentPoolCount = 0;

            await foreach (var agentPool in agentPoolsApi.ListAgentPools(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing agent pool {AgentPoolName} ({AgentPoolId})...", agentPool.Name, agentPool.Id);
                agentPoolCount++;

                // Create the Okta_AgentPool node
                OktaAgentPool agentPoolNode = new(agentPool, OktaDomain);
                await dbContext.AgentPools.AddAsync(agentPoolNode, cancellationToken).ConfigureAwait(false);

                int agentCount = 0;

                foreach (var agent in agentPool.Agents ?? [])
                {
                    // Process each agent in the current agent pool
                    _logger.LogDebug("Processing agent {AgentName} ({AgentId})...", agent.Name, agent.Id);
                    agentCount++;

                    // Create the Okta_Agent node
                    OktaAgent agentNode = new(agent, OktaDomain);
                    await dbContext.Agents.AddAsync(agentNode, cancellationToken).ConfigureAwait(false);
                }

                // Save the pool and its agents
                _logger.LogTrace("Successfully retrieved {AgentCount} agents in pool {AgentPoolName}.", agentCount, agentPool.Name);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {AgentPoolCount} agent pools.", agentPoolCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta agent pools.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaDevices(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching devices...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteDevices(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            DeviceApi deviceApi = new(_oktaConfig);
            int deviceCount = 0;

            await foreach (var device in deviceApi.ListDevices(expand: DeviceExpandParameter.UserSummary, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing device {DeviceName} ({DeviceId})...", device.Profile.DisplayName, device.Id);
                deviceCount++;

                // Create the Okta_Device node
                OktaDevice deviceNode = new(device, OktaDomain);
                await dbContext.Devices.AddAsync(deviceNode, cancellationToken).ConfigureAwait(false);

                // Add device ownership relationships
                var ownerIds = (device.Embedded.Users ?? [])
                    .Select(embeddedUser => embeddedUser.User.Id)
                    .Where(ownerId => !string.IsNullOrWhiteSpace(ownerId))
                    .Distinct()
                    .ToList();

                var deviceOwners = dbContext.DeviceOwners;
                foreach (var ownerId in ownerIds)
                {
                    deviceOwners.Add(new OktaDeviceOwner
                    {
                        DeviceId = deviceNode.Id,
                        OwnerId = ownerId
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {DeviceCount} devices.", deviceCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta devices.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaResourceSets(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching custom resource sets...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteResourceSets(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            RoleCResourceSetApi resourceSetApi = new(_oktaConfig);
            int resourceSetCount = 0;

            await foreach (ResourceSet resourceSet in resourceSetApi.ListAllResourceSets(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing resource set {ResourceSetLabel} ({ResourceSetId})...", resourceSet.Label, resourceSet.Id);
                resourceSetCount++;

                // Create the Okta_ResourceSet node
                OktaResourceSet resourceSetNode = new(resourceSet, OktaDomain);
                await dbContext.ResourceSets.AddAsync(resourceSetNode, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {ResourceSetCount} resource sets.", resourceSetCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta resource sets.", e.ErrorCode, status);
        }
    }


    public async Task CollectOktaRealms(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching realms...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteRealms(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            RealmApi realmApi = new(_oktaConfig);
            int realmCount = 0;

            await foreach (var realm in realmApi.ListRealms(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing realm {RealmName} ({RealmId})...", realm.Profile.Name, realm.Id);
                realmCount++;

                OktaRealm realmNode = new(realm, OktaDomain);
                await dbContext.Realms.AddAsync(realmNode, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

    public async Task CollectOktaBuiltInRoles(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching built-in roles...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteBuiltinRoles(dbContext, cancellationToken).ConfigureAwait(false);
        }

        int roleCount = 0;
        RoleECustomApi roleApi = new(_oktaConfig);

        foreach (string roleId in OktaBuiltinRole.BuiltInRoles)
        {
            try
            {
                _logger.LogDebug("Fetching info about the built-in role {RoleId}...", roleId);
                IamRole role = await roleApi.GetRoleAsync(roleId, cancellationToken).ConfigureAwait(false);

                // Create the Okta_Role node
                OktaBuiltinRole roleNode = new(role, OktaDomain);
                await dbContext.BuiltinRoles.AddAsync(roleNode, cancellationToken).ConfigureAwait(false);
                roleCount++;
            }
            catch (ApiException e)
            {
                var status = (HttpStatusCode)e.ErrorCode;

                if (status == HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Could not load the {RoleId} role. The corresponding feature might not be licensed or enabled.", roleId);
                }
                else
                {
                    _logger.LogError("Error {ErrorCode} ({Status}) received while trying to load the {RoleId} role.", e.ErrorCode, status, roleId);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (roleCount > 0)
        {
            _logger.LogInformation("Successfully processed {RoleCount} built-in roles.", roleCount);
        }
        else
        {
            _logger.LogCritical("No built-in roles were processed. Please ensure that the API token has the required permissions.");
        }
    }

    public async Task CollectOktaCustomRoles(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching custom roles...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteCustomRoles(dbContext, cancellationToken).ConfigureAwait(false);
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

                // Create the Okta_CustomRole node
                OktaCustomRole roleNode = new(role, OktaDomain);
                await dbContext.CustomRoles.AddAsync(roleNode, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {RoleCount} custom roles.", roleCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta custom roles.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaApplications(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching applications...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteApplications(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            ApplicationApi appApi = new(_oktaConfig);
            int applicationCount = 0;

            await foreach (var app in appApi.ListApplications(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing application {AppLabel} ({AppId})...", app.Label, app.Id);
                applicationCount++;

                // Create the Okta_Application node
                OktaApplication appNode = new(app, OktaDomain);
                await dbContext.Applications.AddAsync(appNode, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {ApplicationCount} applications.", applicationCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta applications.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaApiTokens(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API tokens...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteApiTokens(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            ApiTokenApi tokenApi = new(_oktaConfig);
            int tokenCount = 0;

            await foreach (var token in tokenApi.ListApiTokens(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing API token {TokenName} ({TokenId})...", token.Name, token.Id);
                tokenCount++;

                // Create the Okta_ApiToken node
                OktaApiToken tokenNode = new(token, OktaDomain);
                await dbContext.ApiTokens.AddAsync(tokenNode, cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {TokenCount} API tokens.", tokenCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta API tokens.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaAuthorizationServers(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching authorization servers...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteAuthorizationServers(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            AuthorizationServerApi authorizationServerApi = new(_oktaConfig);
            int authorizationServerCount = 0;

            await foreach (var authorizationServer in authorizationServerApi.ListAuthorizationServers(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing authorization server {AuthorizationServerName} ({AuthorizationServerId})...", authorizationServer.Name, authorizationServer.Id);
                authorizationServerCount++;

                // Create the Okta_AuthorizationServer node
                OktaAuthorizationServer authorizationServerNode = new(authorizationServer, OktaDomain);
                await dbContext.AuthorizationServers.AddAsync(authorizationServerNode, cancellationToken).ConfigureAwait(false);

                // TODO: List associated trusted servers
                // AuthorizationServerAssocApi authorizationServerAssocApi = new(_oktaConfig);
                // authorizationServerAssocApi.ListAssociatedServersByTrustedType(authorizationServerNode.Id, trusted: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {AuthorizationServerCount} authorization servers.", authorizationServerCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta authorization servers.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaIdentityProviders(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching identity providers...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteIdentityProviders(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            IdentityProviderApi identityProviderApi = new(_oktaConfig);
            int identityProviderCount = 0;

            await foreach (var identityProvider in identityProviderApi.ListIdentityProviders(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing identity provider {IdentityProviderName} ({IdentityProviderId})...", identityProvider.Name, identityProvider.Id);
                identityProviderCount++;

                // Create the Okta_IdentityProvider node
                OktaIdentityProvider identityProviderNode = new(identityProvider, OktaDomain);
                await dbContext.IdentityProviders.AddAsync(identityProviderNode, cancellationToken).ConfigureAwait(false);

                // Governed Groups
                // Hardcoded group assignemnts
                List<string> assignedGroupIds = identityProvider.Policy?.Provisioning?.Groups?.Assignments?.ToList() ?? [];

                // Group assignments sourced from SAML claims
                List<string> dynamicGroupIds = identityProvider.Policy?.Provisioning?.Groups?.Filter?.ToList() ?? [];

                // Combine both assignment types. The UI only allows one or the other.
                List<string> governedGroupIds = [.. assignedGroupIds, .. dynamicGroupIds];

                foreach (var groupId in governedGroupIds)
                {
                    dbContext.IdentityProviderGovernedGroups.Add(new OktaIdentityProviderGovernedGroup
                    {
                        IdentityProviderId = identityProviderNode.Id,
                        GroupId = groupId
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed {IdentityProviderCount} identity providers.", identityProviderCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta identity providers.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaApiServiceIntegrations(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching API service integrations...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeleteApiServiceIntegrations(dbContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            ApiServiceIntegrationsApi apiServiceApi = new(_oktaConfig);
            int serviceCount = 0;

            await foreach (var service in apiServiceApi.ListApiServiceIntegrationInstances(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Processing API service integration {ServiceName} ({ServiceId})...", service.Name, service.Id);
                serviceCount++;

                // Create the Okta_ApiServiceIntegration node
                OktaApiServiceIntegration serviceNode = new(service, OktaDomain);

                if (!string.IsNullOrWhiteSpace(service.CreatedBy))
                {
                    serviceNode.CreatedById = service.CreatedBy;
                }

                await dbContext.ApiServiceIntegrations.AddAsync(serviceNode, cancellationToken).ConfigureAwait(false);

                // TODO: Add attack path edges for service integrations
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);


            _logger.LogInformation("Successfully processed {ServiceCount} API service integrations.", serviceCount);
        }
        catch (ApiException e)
        {
            var status = (HttpStatusCode)e.ErrorCode;
            _logger.LogError("Error {ErrorCode} ({Status}) received while trying to fetch Okta API service integrations.", e.ErrorCode, status);
        }
    }

    public async Task CollectOktaPolicies(bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching policies...");
        using var dbContext = new AppDbContext(_outputDirectory);

        if (clearPreexistingTables)
        {
            await DeletePolicies(dbContext, cancellationToken).ConfigureAwait(false);
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

                    // Create the Okta_Policy node
                    OktaPolicy policyNode = new(policy, OktaDomain);
                    await dbContext.Policies.AddAsync(policyNode, cancellationToken).ConfigureAwait(false);
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

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully processed {PolicyCount} policies.", policyCount);
    }
}
