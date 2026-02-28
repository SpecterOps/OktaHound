using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SpecterOps.OktaHound.Database;

public sealed class AppDbContext : DbContext
{
    public const string DatabaseFileName = "oktahound.db";

    public string DatabasePath { get; }

    public DbSet<OktaUser> Users => Set<OktaUser>();

    public DbSet<OktaGroup> Groups => Set<OktaGroup>();

    public DbSet<OktaApplication> Applications => Set<OktaApplication>();

    public DbSet<OktaDevice> Devices => Set<OktaDevice>();

    public DbSet<OktaResourceSet> ResourceSets => Set<OktaResourceSet>();

    public DbSet<OktaRealm> Realms => Set<OktaRealm>();

    public DbSet<OktaBuiltinRole> BuiltinRoles => Set<OktaBuiltinRole>();

    public DbSet<OktaCustomRole> CustomRoles => Set<OktaCustomRole>();

    public DbSet<OktaRoleAssignment> RoleAssignments => Set<OktaRoleAssignment>();

    public DbSet<OktaApiToken> ApiTokens => Set<OktaApiToken>();

    public DbSet<OktaAgentPool> AgentPools => Set<OktaAgentPool>();

    public DbSet<OktaAgent> Agents => Set<OktaAgent>();

    public DbSet<OktaAuthorizationServer> AuthorizationServers => Set<OktaAuthorizationServer>();

    public DbSet<OktaIdentityProvider> IdentityProviders => Set<OktaIdentityProvider>();

    public DbSet<OktaApiServiceIntegration> ApiServiceIntegrations => Set<OktaApiServiceIntegration>();

    public DbSet<OktaPolicy> Policies => Set<OktaPolicy>();

    public DbSet<OktaClientSecret> ClientSecrets => Set<OktaClientSecret>();

    public DbSet<OktaJWK> JWKs => Set<OktaJWK>();

    public DbSet<OktaDeviceOwner> DeviceOwners => Set<OktaDeviceOwner>();

    public DbSet<OktaIdentityProviderGovernedGroup> IdentityProviderGovernedGroups => Set<OktaIdentityProviderGovernedGroup>();

    public DbSet<CollectionStatus> CollectionStatuses => Set<CollectionStatus>();

    public DbSet<OktaOrganization> Organizations => Set<OktaOrganization>();

    public AppDbContext(string databaseDirectory)
    {
        DatabasePath = Path.Combine(databaseDirectory, DatabaseFileName);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collection status singleton (always 0 or 1 row)
        modelBuilder.Entity<CollectionStatus>()
            .HasKey(status => status.Id);

        modelBuilder.Entity<CollectionStatus>()
            .Property<int>(nameof(CollectionStatus.SingletonId))
            .HasDefaultValue(1);

        modelBuilder.Entity<CollectionStatus>()
            .HasIndex(nameof(CollectionStatus.SingletonId))
            .IsUnique();

        modelBuilder.Entity<CollectionStatus>()
            .HasData(new CollectionStatus { Id = CollectionStatus.SingletonId });

        // Index the Okta organization domain name
        modelBuilder.Entity<OktaOrganization>()
            .HasIndex(organization => organization.Name)
            .IsUnique();

        // Entity to organization relationships
        ConfigureOrganizationRelationship<OktaUser>(modelBuilder);
        ConfigureOrganizationRelationship<OktaGroup>(modelBuilder);
        ConfigureOrganizationRelationship<OktaApplication>(modelBuilder);
        ConfigureOrganizationRelationship<OktaDevice>(modelBuilder);
        ConfigureOrganizationRelationship<OktaResourceSet>(modelBuilder);
        ConfigureOrganizationRelationship<OktaRealm>(modelBuilder);
        ConfigureOrganizationRelationship<OktaBuiltinRole>(modelBuilder);
        ConfigureOrganizationRelationship<OktaCustomRole>(modelBuilder);
        ConfigureOrganizationRelationship<OktaRoleAssignment>(modelBuilder);
        ConfigureOrganizationRelationship<OktaApiToken>(modelBuilder);
        ConfigureOrganizationRelationship<OktaAgentPool>(modelBuilder);
        ConfigureOrganizationRelationship<OktaAgent>(modelBuilder);
        ConfigureOrganizationRelationship<OktaAuthorizationServer>(modelBuilder);
        ConfigureOrganizationRelationship<OktaIdentityProvider>(modelBuilder);
        ConfigureOrganizationRelationship<OktaApiServiceIntegration>(modelBuilder);
        ConfigureOrganizationRelationship<OktaPolicy>(modelBuilder);
        ConfigureOrganizationRelationship<OktaClientSecret>(modelBuilder);
        ConfigureOrganizationRelationship<OktaJWK>(modelBuilder);

        // Agent to agent pool relationship
        modelBuilder.Entity<OktaAgent>()
            .HasOne(agent => agent.AgentPool)
            .WithMany(pool => pool.Agents)
            .HasForeignKey(agent => agent.AgentPoolId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(true);

        // Application custom properties JSON persistence
        modelBuilder.Entity<OktaApplication>()
            .Property(application => application.CustomProperties)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new Dictionary<string, string>()
            );

        // User to realm relationship
        modelBuilder.Entity<OktaUser>()
            .HasOne(user => user.Realm)
            .WithMany(realm => realm.Users)
            .HasForeignKey(user => user.RealmId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Group to source application relationship
        modelBuilder.Entity<OktaGroup>()
            .HasOne(group => group.SourceApplication)
            .WithMany(application => application.ImportedGroups)
            .HasForeignKey(group => group.SourceApplicationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Identity provider to governed groups relationship
        modelBuilder.Entity<OktaIdentityProvider>()
            .HasMany(identityProvider => identityProvider.GovernedGroups)
            .WithMany(group => group.GoverningIdentityProviders)
            .UsingEntity<OktaIdentityProviderGovernedGroup>(
                right => right
                    .HasOne(join => join.Group)
                    .WithMany()
                    .HasForeignKey(join => join.GroupId)
                    .OnDelete(DeleteBehavior.NoAction),
                left => left
                    .HasOne(join => join.IdentityProvider)
                    .WithMany()
                    .HasForeignKey(join => join.IdentityProviderId)
                    .OnDelete(DeleteBehavior.NoAction),
                join =>
                {
                    join.HasKey(entity => new { entity.IdentityProviderId, entity.GroupId });
                }
            );

        // User to manager relationship (self-referencing, calculated from Okta's "managerId" property)
        modelBuilder.Entity<OktaUser>()
            .HasOne(user => user.Manager)
            .WithMany(manager => manager.DirectReports)
            .HasForeignKey(user => user.ManagerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // API token to user relationship
        modelBuilder.Entity<OktaApiToken>()
            .HasOne(apiToken => apiToken.User)
            .WithMany(user => user.ApiTokens)
            .HasForeignKey(apiToken => apiToken.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // JWK to application relationship
        modelBuilder.Entity<OktaJWK>()
            .HasOne(jwk => jwk.Application)
            .WithMany(application => application.JWKs)
            .HasForeignKey(jwk => jwk.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Client secret to application relationship
        modelBuilder.Entity<OktaClientSecret>()
            .HasOne(secret => secret.Application)
            .WithMany(application => application.ClientSecrets)
            .HasForeignKey(secret => secret.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Client secret to API service integration relationship
        modelBuilder.Entity<OktaClientSecret>()
            .HasOne(secret => secret.ApiServiceIntegration)
            .WithMany(serviceIntegration => serviceIntegration.ClientSecrets)
            .HasForeignKey(secret => secret.ApiServiceIntegrationId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // API service integration creator relationship
        modelBuilder.Entity<OktaApiServiceIntegration>()
            .HasOne(serviceIntegration => serviceIntegration.CreatedBy)
            .WithMany(user => user.CreatedApiServiceIntegrations)
            .HasForeignKey(serviceIntegration => serviceIntegration.CreatedById)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Device owners relationship (many-to-many)
        modelBuilder.Entity<OktaDevice>()
            .HasMany(device => device.Owners)
            .WithMany(user => user.OwnedDevices)
            .UsingEntity<OktaDeviceOwner>(
                right => right
                    .HasOne(deviceOwner => deviceOwner.Owner)
                    .WithMany()
                    .HasForeignKey(deviceOwner => deviceOwner.OwnerId)
                    .OnDelete(DeleteBehavior.NoAction),
                left => left
                    .HasOne(deviceOwner => deviceOwner.Device)
                    .WithMany()
                    .HasForeignKey(deviceOwner => deviceOwner.DeviceId)
                    .OnDelete(DeleteBehavior.NoAction),
                join =>
                {
                    join.HasKey(deviceOwner => new { deviceOwner.DeviceId, deviceOwner.OwnerId });
                });

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureOrganizationRelationship<TEntity>(ModelBuilder modelBuilder)
        where TEntity : OktaEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasOne(entity => entity.OktaOrganization)
            .WithMany()
            .HasForeignKey(entity => entity.DomainName)
            .HasPrincipalKey(organization => organization.Name)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}
