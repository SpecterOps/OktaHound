using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpecterOps.OktaHound.Database;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task DeleteUsers(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Users, "users", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteGroups(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Groups, "groups", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApplications(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Applications, "applications", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteDevices(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.DeviceOwners, "device owners", cancellationToken).ConfigureAwait(false);
        await DeleteEntities(dbContext.Devices, "devices", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteResourceSets(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ResourceSets, "resource sets", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRealms(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Realms, "realms", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteBuiltinRoles(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.BuiltinRoles, "built-in roles", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCustomRoles(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.CustomRoles, "custom roles", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRoleAssignments(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.RoleAssignments, "role assignments", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApiTokens(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ApiTokens, "api tokens", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAgentPools(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Agents, "agents", cancellationToken).ConfigureAwait(false);
        await DeleteEntities(dbContext.AgentPools, "agent pools", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAuthorizationServers(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.AuthorizationServers, "authorization servers", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteIdentityProviders(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.IdentityProviderGovernedGroups, "identity provider governed groups", cancellationToken).ConfigureAwait(false);
        await DeleteEntities(dbContext.IdentityProviders, "identity providers", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApiServiceIntegrations(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ApiServiceIntegrations, "api service integrations", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePolicies(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Policies, "policies", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteClientSecrets(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ClientSecrets, "client secrets", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteJWKs(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.JWKs, "JWKs", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApplicationGrants(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ApplicationGrants, "application grants", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserGroupMemberships(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.UserGroupMemberships, "user group memberships", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAppUserAssignments(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ApplicationUserAssignments, "application user assignments", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAppGroupAssignments(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.ApplicationGroupAssignments, "application group assignments", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePrivilegedUsers(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.PrivilegedUsers, "privileged users", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteUserFactors(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.UserFactors, "user factors", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOrganizations(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await DeleteEntities(dbContext.Organizations, "organizations", cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteEntities<T>(DbSet<T> set, string entityName, CancellationToken cancellationToken)
        where T : class
    {
        _logger.LogInformation("Deleting pre-existing {EntityName} from the database...", entityName);
        int deletedCount = await set.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Deleted {DeletedCount} pre-existing {EntityName} from the database.", deletedCount, entityName);
    }
}
