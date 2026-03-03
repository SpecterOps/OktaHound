using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpecterOps.OktaHound.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationGroupAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizationsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsersCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgentPoolsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgentsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    DevicesCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResourceSetsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    RealmsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    BuiltinRolesCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomRolesCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApplicationsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApiTokensCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthorizationServersCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    IdentityProvidersCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApiServiceIntegrationsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    PoliciesCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleAssignmentsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClientSecretsCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    JwksCollected = table.Column<bool>(type: "INTEGER", nullable: false),
                    SingletonId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DomainName = table.Column<string>(type: "TEXT", nullable: false),
                    Subdomain = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AgentlessDssoEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.UniqueConstraint("AK_Organizations_Name", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "AgentPools",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    OperationalStatus = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActiveDirectoryAgentPool = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentPools_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationType = table.Column<string>(type: "TEXT", nullable: true),
                    ClientType = table.Column<string>(type: "TEXT", nullable: true),
                    SignOnMode = table.Column<string>(type: "TEXT", nullable: true),
                    Features = table.Column<string>(type: "TEXT", nullable: true),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true),
                    UserNameMapping = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    GrantTypes = table.Column<string>(type: "TEXT", nullable: true),
                    GitHubOrganizationName = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveDirectoryDomain = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveDirectoryDomainSid = table.Column<string>(type: "TEXT", nullable: true),
                    EntraIdDiscoveryEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    EntraOnMicrosoftDomain = table.Column<string>(type: "TEXT", nullable: true),
                    EntraTenantId = table.Column<string>(type: "TEXT", nullable: true),
                    EntraClientId = table.Column<string>(type: "TEXT", nullable: true),
                    JamfDomain = table.Column<string>(type: "TEXT", nullable: true),
                    SnowflakeSubdomain = table.Column<string>(type: "TEXT", nullable: true),
                    OnePasswordRegionType = table.Column<string>(type: "TEXT", nullable: true),
                    OnePasswordSubDomain = table.Column<string>(type: "TEXT", nullable: true),
                    FilterGroupsByOU = table.Column<bool>(type: "INTEGER", nullable: true),
                    CustomProperties = table.Column<string>(type: "TEXT", nullable: false),
                    SupportsSCIM = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupportsPasswordUpdates = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsService = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true),
                    HasRoleAssignments = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOrgAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationServers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: true),
                    IssuerMode = table.Column<string>(type: "TEXT", nullable: true),
                    Audiences = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationServers_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "BuiltinRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalId = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuiltinRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuiltinRoles_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "CustomRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomRoles_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalId = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    ResourceType = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    OsVersion = table.Column<string>(type: "TEXT", nullable: true),
                    Registered = table.Column<bool>(type: "INTEGER", nullable: true),
                    SecureHardwarePresent = table.Column<bool>(type: "INTEGER", nullable: true),
                    JailBreak = table.Column<bool>(type: "INTEGER", nullable: true),
                    ObjectSid = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    UDID = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IssuerMode = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProtocolType = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    AutomaticUserProvisioning = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityProviders_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyType = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: true),
                    System = table.Column<bool>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Realms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Domains = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Realms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Realms_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "ResourceSets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ResourceUris = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceSets_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RoleType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    AssignmentType = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAssignments_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OperationalStatus = table.Column<string>(type: "TEXT", nullable: true),
                    UpdateStatus = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    LastConnection = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AgentPoolId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_AgentPools_AgentPoolId",
                        column: x => x.AgentPoolId,
                        principalTable: "AgentPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agents_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationGrants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: true),
                    ScopeId = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByType = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationGrants_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastMembershipUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    OktaGroupType = table.Column<string>(type: "TEXT", nullable: true),
                    ObjectClass = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ObjectSid = table.Column<string>(type: "TEXT", nullable: true),
                    DistinguishedName = table.Column<string>(type: "TEXT", nullable: true),
                    SamAccountName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainQualifiedName = table.Column<string>(type: "TEXT", nullable: true),
                    GroupScope = table.Column<string>(type: "TEXT", nullable: true),
                    GroupType = table.Column<string>(type: "TEXT", nullable: true),
                    ObjectGuid = table.Column<string>(type: "TEXT", nullable: true),
                    SourceApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                    DomainSid = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true),
                    HasRoleAssignments = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOrgAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Applications_SourceApplicationId",
                        column: x => x.SourceApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Groups_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "JWKs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<string>(type: "TEXT", nullable: true),
                    KeyId = table.Column<string>(type: "TEXT", nullable: true),
                    KeyType = table.Column<string>(type: "TEXT", nullable: true),
                    KeyUsage = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JWKs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JWKs_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JWKs_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Login = table.Column<string>(type: "TEXT", nullable: true),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastLogin = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PasswordChanged = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Activated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UserType = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: true),
                    CountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Organization = table.Column<string>(type: "TEXT", nullable: true),
                    Division = table.Column<string>(type: "TEXT", nullable: true),
                    RealmId = table.Column<string>(type: "TEXT", nullable: true),
                    ManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    CredentialProviderType = table.Column<string>(type: "TEXT", nullable: true),
                    CredentialProviderName = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                    table.ForeignKey(
                        name: "FK_Users_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationGroupAssignments",
                columns: table => new
                {
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: true),
                    Profile = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationGroupAssignments", x => new { x.ApplicationId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_ApplicationGroupAssignments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationGroupAssignments_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderGovernedGroups",
                columns: table => new
                {
                    IdentityProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviderGovernedGroups", x => new { x.IdentityProviderId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_IdentityProviderGovernedGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IdentityProviderGovernedGroups_IdentityProviders_IdentityProviderId",
                        column: x => x.IdentityProviderId,
                        principalTable: "IdentityProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiServiceIntegrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    IntegrationType = table.Column<string>(type: "TEXT", nullable: true),
                    Permissions = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiServiceIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiServiceIntegrations_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                    table.ForeignKey(
                        name: "FK_ApiServiceIntegrations_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    ClientName = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    NetworkConnection = table.Column<string>(type: "TEXT", nullable: true),
                    TokenWindow = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiTokens_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                    table.ForeignKey(
                        name: "FK_ApiTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserAssignments",
                columns: table => new
                {
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: true),
                    TargetUserName = table.Column<string>(type: "TEXT", nullable: true),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    SyncState = table.Column<string>(type: "TEXT", nullable: true),
                    Profile = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    StatusChanged = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PasswordChanged = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastSync = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserAssignments", x => new { x.ApplicationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserAssignments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationUserAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceOwners",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOwners", x => new { x.DeviceId, x.OwnerId });
                    table.ForeignKey(
                        name: "FK_DeviceOwners_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceOwners_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrivilegedUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Orn = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivilegedUsers_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserFactors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FactorType = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    VendorName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFactors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroupMemberships",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMemberships", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroupMemberships_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserGroupMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientSecrets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ApplicationId = table.Column<string>(type: "TEXT", nullable: true),
                    ApiServiceIntegrationId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSecrets_ApiServiceIntegrations_ApiServiceIntegrationId",
                        column: x => x.ApiServiceIntegrationId,
                        principalTable: "ApiServiceIntegrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientSecrets_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientSecrets_Organizations_DomainName",
                        column: x => x.DomainName,
                        principalTable: "Organizations",
                        principalColumn: "Name");
                });

            migrationBuilder.InsertData(
                table: "CollectionStatuses",
                columns: new[] { "Id", "AgentPoolsCollected", "AgentsCollected", "ApiServiceIntegrationsCollected", "ApiTokensCollected", "ApplicationsCollected", "AuthorizationServersCollected", "BuiltinRolesCollected", "ClientSecretsCollected", "CustomRolesCollected", "DevicesCollected", "GroupsCollected", "IdentityProvidersCollected", "JwksCollected", "OrganizationsCollected", "PoliciesCollected", "RealmsCollected", "ResourceSetsCollected", "RoleAssignmentsCollected", "UsersCollected" },
                values: new object[] { 1, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_DomainName",
                table: "AgentPools",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_AgentPoolId",
                table: "Agents",
                column: "AgentPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_DomainName",
                table: "Agents",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_ApiServiceIntegrations_CreatedById",
                table: "ApiServiceIntegrations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApiServiceIntegrations_DomainName",
                table: "ApiServiceIntegrations",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_DomainName",
                table: "ApiTokens",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_UserId",
                table: "ApiTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationGrants_ApplicationId",
                table: "ApplicationGrants",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationGroupAssignments_GroupId",
                table: "ApplicationGroupAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAssignments_UserId",
                table: "ApplicationUserAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_DomainName",
                table: "Applications",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_IsService",
                table: "Applications",
                column: "IsService");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationServers_DomainName",
                table: "AuthorizationServers",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_BuiltinRoles_DomainName",
                table: "BuiltinRoles",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ApiServiceIntegrationId",
                table: "ClientSecrets",
                column: "ApiServiceIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_ApplicationId",
                table: "ClientSecrets",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSecrets_DomainName",
                table: "ClientSecrets",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionStatuses_SingletonId",
                table: "CollectionStatuses",
                column: "SingletonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_DomainName",
                table: "CustomRoles",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOwners_OwnerId",
                table: "DeviceOwners",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DomainName",
                table: "Devices",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_DomainName",
                table: "Groups",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SourceApplicationId",
                table: "Groups",
                column: "SourceApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderGovernedGroups_GroupId",
                table: "IdentityProviderGovernedGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviders_DomainName",
                table: "IdentityProviders",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_JWKs_ApplicationId",
                table: "JWKs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_JWKs_DomainName",
                table: "JWKs",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_DomainName",
                table: "Policies",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Realms_DomainName",
                table: "Realms",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSets_DomainName",
                table: "ResourceSets",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_DomainName",
                table: "RoleAssignments",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_UserFactors_UserId",
                table: "UserFactors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMemberships_GroupId",
                table: "UserGroupMemberships",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DomainName",
                table: "Users",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RealmId",
                table: "Users",
                column: "RealmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "ApplicationGrants");

            migrationBuilder.DropTable(
                name: "ApplicationGroupAssignments");

            migrationBuilder.DropTable(
                name: "ApplicationUserAssignments");

            migrationBuilder.DropTable(
                name: "AuthorizationServers");

            migrationBuilder.DropTable(
                name: "BuiltinRoles");

            migrationBuilder.DropTable(
                name: "ClientSecrets");

            migrationBuilder.DropTable(
                name: "CollectionStatuses");

            migrationBuilder.DropTable(
                name: "CustomRoles");

            migrationBuilder.DropTable(
                name: "DeviceOwners");

            migrationBuilder.DropTable(
                name: "IdentityProviderGovernedGroups");

            migrationBuilder.DropTable(
                name: "JWKs");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "PrivilegedUsers");

            migrationBuilder.DropTable(
                name: "ResourceSets");

            migrationBuilder.DropTable(
                name: "RoleAssignments");

            migrationBuilder.DropTable(
                name: "UserFactors");

            migrationBuilder.DropTable(
                name: "UserGroupMemberships");

            migrationBuilder.DropTable(
                name: "AgentPools");

            migrationBuilder.DropTable(
                name: "ApiServiceIntegrations");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "IdentityProviders");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Realms");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
