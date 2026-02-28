using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpecterOps.OktaHound.Database;

namespace SpecterOps.OktaHound;

partial class OktaClient
{
    public async Task ExportUsers(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Users, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportGroups(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Groups, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportApplications(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Applications, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportDevices(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Devices, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportResourceSets(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.ResourceSets, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportRealms(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Realms, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportBuiltinRoles(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.BuiltinRoles, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportCustomRoles(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.CustomRoles, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportRoleAssignments(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.RoleAssignments, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportApiTokens(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.ApiTokens, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportAgentPools(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.AgentPools, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportAgents(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Agents, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportAuthorizationServers(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.AuthorizationServers, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportIdentityProviders(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.IdentityProviders, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportApiServiceIntegrations(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.ApiServiceIntegrations, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportPolicies(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Policies, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportClientSecrets(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.ClientSecrets, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportJWKs(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.JWKs, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportOrganizations(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(_outputDirectory);
        await ExportEntities(dbContext.Organizations, writer, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExportEntities<T>(DbSet<T> set, Utf8JsonWriter writer, CancellationToken cancellationToken)
        where T : OpenGraphEntity
    {
        await foreach (var entity in set.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            entity.Serialize(writer);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
