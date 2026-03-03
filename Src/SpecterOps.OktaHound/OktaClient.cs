using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Okta.Sdk.Client;
using SpecterOps.OktaHound.Database;
using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound;

internal partial class OktaClient
{
    /// <summary>
    /// Default Okta domain if none is specified in okta.yaml
    /// </summary>
    private const string DefaultOktaDomain = "https://subdomain.okta.com";

    /// <summary>
    /// Maximum concurrent requests per API endpoint
    /// </summary>
    private const int DefaultConcurrentApiCalls = 3;

    private static readonly HashSet<string> RequiredOktaScopes = [
        "okta.orgs.read",
        "okta.users.read",
        "okta.groups.read",
        "okta.apps.read",
        "okta.appGrants.read",
        "okta.devices.read",
        "okta.roles.read",
        "okta.apiTokens.read",
        "okta.realms.read",
        "okta.realmAssignments.read",
        "okta.agentPools.read",
        "okta.idps.read",
        "okta.authorizationServers.read",
        "okta.oauthIntegrations.read",
        "okta.policies.read",
        "okta.logs.read"
    ];

    private OktaGraph? _graph;

    private readonly OpenGraph _adGraph = new("Base");

    private readonly OpenGraph _hybridEdgeGraph = new();

    private readonly Configuration _oktaConfig;
    private readonly string _outputDirectory;

    /// <summary>
    /// Represents the logger instance used for logging operations within the application.
    /// </summary>
    private readonly ILogger _logger;

    private readonly int _concurrentApiCalls;

    public string OktaDomain { get; private set; } = string.Empty;

    public OktaClient(string outputDirectory, ILogger? logger, Configuration? oktaConfig = null, int concurrentApiCalls = DefaultConcurrentApiCalls)
    {
        if (concurrentApiCalls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(concurrentApiCalls));
        }

        _concurrentApiCalls = concurrentApiCalls;

        // Drop events if no logger is provided
        _logger = logger ?? NullLogger.Instance;

        // Store the output directory for later use in the DbContext
        _outputDirectory = outputDirectory;

        // Connect using the okta.yaml configuration file,
        // located in the app directory or in ~/.okta/.
        // Merge with the optionally provided configuration.
        _logger.LogInformation("Loading Okta configuration...");
        this._oktaConfig = Configuration.GetConfigurationOrDefault(oktaConfig);

        // Check the authentication type
        if (this._oktaConfig.AuthorizationMode == AuthorizationMode.SSWS)
        {
            _logger.LogWarning("Using API Token (SSWS) authentication. It is recommended to use OAuth 2.0 with an API Service App for better security and auditing.");
        }

        // Override the scopes to ensure we have the required permissions.
        this._oktaConfig.Scopes = RequiredOktaScopes;

        // Extract the domain name from the Okta URL for later use in node creation
        OktaDomain = new Uri(this._oktaConfig.OktaDomain).Host;
    }

    public async Task Collect(CollectionTarget collectionTarget = CollectionTarget.All, bool clearPreexistingTables = false, CancellationToken cancellationToken = default)
    {
        if (collectionTarget.HasFlag(CollectionTarget.Organization))
        {
            await CollectOrganization(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Users))
        {
            await CollectUsers(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Groups))
        {
            await CollectGroups(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.AgentPools) || collectionTarget.HasFlag(CollectionTarget.Agents))
        {
            await CollectAgentPools(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Devices))
        {
            await CollectOktaDevices(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ResourceSets))
        {
            await CollectOktaResourceSets(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Realms))
        {
            await CollectOktaRealms(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.BuiltinRoles))
        {
            await CollectOktaBuiltInRoles(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.CustomRoles))
        {
            await CollectOktaCustomRoles(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Applications))
        {
            await CollectOktaApplications(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ApiTokens))
        {
            await CollectOktaApiTokens(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.AuthorizationServers))
        {
            await CollectOktaAuthorizationServers(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.IdentityProviders))
        {
            await CollectOktaIdentityProviders(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ApiServiceIntegrations))
        {
            await CollectOktaApiServiceIntegrations(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Policies))
        {
            await CollectOktaPolicies(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.RoleAssignments))
        {
            // await CollectOktaRoleAssignments(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ClientSecrets))
        {
            await CollectOktaApplicationSecrets(clearPreexistingTables, cancellationToken);
            await CollectOktaApiServiceIntegrationSecrets(cancellationToken: cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.Jwks))
        {
            await CollectOktaApplicationJsonWebKeys(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ApplicationGrants))
        {
            await CollectOktaApplicationGrants(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ApplicationUserAssignments))
        {
            await CollectOktaAppUserAssignments(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.ApplicationGroupAssignments))
        {
            await CollectOktaAppGroupAssignments(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.UserGroupMemberships))
        {
            await CollectOktaGroupMemberships(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.PrivilegedUsers))
        {
            await CollectOktaPrivilegedUsers(clearPreexistingTables, cancellationToken);
        }

        if (collectionTarget.HasFlag(CollectionTarget.UserFactors))
        {
            await CollectOktaUserAuthenticationFactors(clearPreexistingTables, cancellationToken);
        }
    }

    public async Task InitializeOktaGraph(CancellationToken cancellationToken = default)
    {
        // OktaOrganization? orgNode = await FetchOktaOrganization(cancellationToken);

        if (true) // orgNode is null
        {
            // Drop any pre-existing graph to indicate failure.
            _graph = null;
        }
        else
        {
            // Create an empty BloodHound OpenGraph associated with the Org.
            _logger.LogDebug("Initializing the graph...");
            _graph = new(null); // OrgNode
        }
    }

    public async Task<(OktaGraph? oktaGraph, OpenGraph adGraph, OpenGraph hybridEdges)> FetchOktaGraph(bool skipMfa = false, CancellationToken cancellationToken = default)
    {
        // Fetch the Okta organization information first
        await InitializeOktaGraph(cancellationToken).ConfigureAwait(false);

        if (_graph is null)
        {
            // Do not proceed if the organization information could not be fetched
            return (null, _adGraph, _hybridEdgeGraph);
        }

        // Enumerate the basic Okta entities and edges
        Task usersTask = FetchOktaUsers(cancellationToken).
            ContinueWith(_ => FetchOktaPrivilegedUsers(cancellationToken), cancellationToken).
            Unwrap();

        Task factorsTask;

        if (skipMfa)
        {
            _logger.LogInformation("Skipping user authentication factors collection.");
            factorsTask = Task.CompletedTask;
        }
        else
        {
            factorsTask = usersTask.
                ContinueWith(_ => FetchOktaUserAuthenticationFactors(cancellationToken), cancellationToken).
                Unwrap();
        }

        Task appsTask = FetchOktaApplications(cancellationToken);

        // Apps must be fetched before groups to enablke hybrid edge creation
        Task groupsTask = appsTask.
            ContinueWith(_ => FetchOktaGroups(cancellationToken), cancellationToken).
            Unwrap();

        Task groupMembershipsTask = groupsTask.
            ContinueWith(_ => FetchOktaGroupMemberships(cancellationToken), cancellationToken).
            Unwrap();

        Task appRelationshipsTask = appsTask.
            ContinueWith(_ => FetchOktaAppUserAssignments(cancellationToken), cancellationToken).
            Unwrap().
            ContinueWith(_ => FetchOktaApplicationGrants(cancellationToken), cancellationToken).
            Unwrap().
            ContinueWith(_ => FetchOktaApplicationSecrets(cancellationToken), cancellationToken).
            Unwrap().
            ContinueWith(_ => FetchOktaApplicationJsonWebKeys(cancellationToken), cancellationToken).
            Unwrap();

        // Both app and group nodes must be loaded before the assignments can be processed.
        Task appGroupsTask = Task.WhenAll(groupsTask, appRelationshipsTask).
            ContinueWith(_ => FetchOktaAppGroupAssignments(cancellationToken), cancellationToken).
            Unwrap().
            ContinueWith(_ => FetchOktaAppGroupPushMappings(cancellationToken), cancellationToken).
            Unwrap();

        Task apiServicesTask = FetchOktaApiServiceIntegrations(cancellationToken).
            ContinueWith(_ => FetchOktaApiServiceIntegrationSecrets(cancellationToken), cancellationToken).
            Unwrap();

        Task identityProvidersTask = FetchOktaIdentityProviders(cancellationToken).
            ContinueWith(_ => FetchOktaIdentityProviderUsers(cancellationToken), cancellationToken).
            Unwrap();

        Task customRolesTask = FetchOktaCustomRoles(cancellationToken).
            ContinueWith(_ => FetchOktaCustomRolePermissions(cancellationToken), cancellationToken).
            Unwrap();

        Task policiesTask = FetchOktaPolicies(cancellationToken).
            ContinueWith(_ => FetchOktaPolicyRules(cancellationToken), cancellationToken).
            Unwrap().
            ContinueWith(_ => FetchOktaPolicyMappings(cancellationToken), cancellationToken).
            Unwrap();

        Task devicesTask = FetchOktaDevices(cancellationToken);
        Task realmsTask = FetchOktaRealms(cancellationToken);
        Task rolesTask = FetchOktaBuiltInRoles(cancellationToken);
        Task resourceSetsTask = FetchOktaResourceSets(cancellationToken);
        Task apiTokensTask = FetchOktaApiTokens(cancellationToken);
        Task agentPoolsTask = FetchOktaAgentPools(cancellationToken);
        Task authorizationServersTask = FetchOktaAuthorizationServers(cancellationToken);

        // Event logs require both users and devices to be loaded first
        // TODO: Implement event log processing
        // Task eventLogTask = Task.WhenAll(usersTask, devicesTask).
        //    ContinueWith(_ => FetchOktaLogs(cancellationToken), cancellationToken).
        //    Unwrap();

        // Wait for all basic fetch tasks to complete
        await Task.WhenAll(
            usersTask,
            groupsTask,
            groupMembershipsTask,
            appRelationshipsTask,
            appsTask,
            appGroupsTask,
            devicesTask,
            realmsTask,
            rolesTask,
            customRolesTask,
            resourceSetsTask,
            apiTokensTask,
            agentPoolsTask,
            authorizationServersTask,
            identityProvidersTask,
            apiServicesTask,
            policiesTask
            ).ConfigureAwait(false);

        // Fetch advanced relationships between Okta entities.
        // All RBAC-relevant entities and group memberships must already be collected.
        await FetchOktaResourceSetMemberships(cancellationToken).ConfigureAwait(false);
        await FetchOktaUserRoleAssignments(cancellationToken).ConfigureAwait(false);
        await FetchOktaGroupRoleAssignments(cancellationToken).ConfigureAwait(false);
        await FetchOktaAppRoleAssignments(cancellationToken).ConfigureAwait(false);
        await FetchOktaResourceSetRoleAssignments(cancellationToken).ConfigureAwait(false);

        // Keep the FetchOktaUserAuthenticationFactors running in parallel with all other tasks.
        // It iterates over all users, making it the longest running task.

        await factorsTask.ConfigureAwait(false);

        // Perform post-processsing tasks
        CreateDomainNodes();
        AddPermissionEdges();
        CreateManagerEdges();

        return (_graph, _adGraph, _hybridEdgeGraph);
    }

    public async Task ExportOktaGraph(Utf8JsonWriter writer, CancellationToken cancellationToken = default)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("metadata");
        var metadata = new OpenGraphMetadata("Okta");
        JsonSerializer.Serialize(writer, metadata, TestSerializationContext.Default.OpenGraphMetadata);

        writer.WritePropertyName("graph");
        writer.WriteStartObject();

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();

        await ExportOrganizations(writer, cancellationToken).ConfigureAwait(false);
        await ExportUsers(writer, cancellationToken).ConfigureAwait(false);
        await ExportGroups(writer, cancellationToken).ConfigureAwait(false);
        await ExportApplications(writer, cancellationToken).ConfigureAwait(false);
        await ExportDevices(writer, cancellationToken).ConfigureAwait(false);
        await ExportResourceSets(writer, cancellationToken).ConfigureAwait(false);
        await ExportRealms(writer, cancellationToken).ConfigureAwait(false);
        await ExportBuiltinRoles(writer, cancellationToken).ConfigureAwait(false);
        await ExportCustomRoles(writer, cancellationToken).ConfigureAwait(false);
        await ExportRoleAssignments(writer, cancellationToken).ConfigureAwait(false);
        await ExportApiTokens(writer, cancellationToken).ConfigureAwait(false);
        await ExportAgentPools(writer, cancellationToken).ConfigureAwait(false);
        await ExportAgents(writer, cancellationToken).ConfigureAwait(false);
        await ExportAuthorizationServers(writer, cancellationToken).ConfigureAwait(false);
        await ExportIdentityProviders(writer, cancellationToken).ConfigureAwait(false);
        await ExportApiServiceIntegrations(writer, cancellationToken).ConfigureAwait(false);
        await ExportPolicies(writer, cancellationToken).ConfigureAwait(false);
        await ExportClientSecrets(writer, cancellationToken).ConfigureAwait(false);
        await ExportJWKs(writer, cancellationToken).ConfigureAwait(false);

        writer.WriteEndArray(); // Nodes

        writer.WritePropertyName("edges");
        writer.WriteStartArray();



        writer.WriteEndArray(); // Edges

        writer.WriteEndObject(); // Graph
        writer.WriteEndObject(); // Root object
        writer.Flush();
    }
}
