using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpecterOps.OktaHound.Database;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task DeleteUsers(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Users, "users", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteGroups(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Groups, "groups", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApplications(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Applications, "applications", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteDevices(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Devices, "devices", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteResourceSets(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.ResourceSets, "resource sets", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRealms(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Realms, "realms", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteBuiltinRoles(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.BuiltinRoles, "built-in roles", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCustomRoles(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.CustomRoles, "custom roles", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRoleAssignments(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.RoleAssignments, "role assignments", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApiTokens(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.ApiTokens, "api tokens", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAgentPools(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);

        // Agents are collected together with agent pools, so we need to delete them both to avoid foreign key constraint violations
        await DeleteEntities(dbContext.Agents, "agents", cancellationToken).ConfigureAwait(false);
        await DeleteEntities(dbContext.AgentPools, "agent pools", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAuthorizationServers(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.AuthorizationServers, "authorization servers", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteIdentityProviders(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.IdentityProviders, "identity providers", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteApiServiceIntegrations(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.ApiServiceIntegrations, "api service integrations", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePolicies(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.Policies, "policies", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteClientSecrets(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.ClientSecrets, "client secrets", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteJWKs(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await DeleteEntities(dbContext.JWKs, "JWKs", cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOrganizations(CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
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
