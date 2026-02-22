using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaGraphElements(OktaOrganization organization) : OpenGraphElements
{
    [JsonIgnore]
    protected override IEnumerable<OpenGraphNode> EnumerateNodes =>
        UsersById.Select(item => (OpenGraphNode)item.Value).
        Concat(GroupsById.Select(item => (OpenGraphNode)item.Value)).
        Concat(AppsById.Select(item => (OpenGraphNode)item.Value)).
        Concat(RolesById.Select(item => (OpenGraphNode)item.Value)).
        Concat(CustomRolesById.Select(item => (OpenGraphNode)item.Value)).
        Concat(IdentityProviders.Cast<OpenGraphNode>()).
        Concat(Devices.Cast<OpenGraphNode>()).
        Concat(AuthorizationServers.Cast<OpenGraphNode>()).
        Concat(ResourceSets.Cast<OpenGraphNode>()).
        Concat(ApiServiceIntegrations.Cast<OpenGraphNode>()).
        Concat(Realms.Cast<OpenGraphNode>()).
        Concat(Policies.Cast<OpenGraphNode>()).
        Concat(RoleAssignments.Cast<OpenGraphNode>()).
        Concat(GenericNodes).
        Append(Organization);

    [JsonIgnore]
    public override int NodeCount =>
        UsersById.Count +
        GroupsById.Count +
        AppsById.Count +
        RolesById.Count +
        CustomRolesById.Count +
        RoleAssignments.Count +
        IdentityProviders.Count +
        Devices.Count +
        AuthorizationServers.Count +
        ResourceSets.Count +
        ApiServiceIntegrations.Count +
        Realms.Count +
        Policies.Count +
        GenericNodes.Count + 1; // +1 for the Okta_Organization node

    /// <summary>
    /// Represents a thread-safe collection of users, keyed by their unique identifiers.
    /// </summary>
    [JsonIgnore()]
    public readonly ConcurrentDictionary<string, OktaUser> UsersById = new();

    /// <summary>
    /// Represents a thread-safe collection of users, keyed by their login names.
    /// </summary>
    [JsonIgnore()]
    public readonly ConcurrentDictionary<string, OktaUser> UsersByLogin = new();
    /// <summary>
    /// Represents a thread-safe collection of groups, keyed by their unique identifiers.
    /// </summary>
    [JsonIgnore()]
    public readonly ConcurrentDictionary<string, OktaGroup> GroupsById = new();

    /// <summary>
    /// A thread-safe collection of Okta applications, keyed by their unique identifiers.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentDictionary<string, OktaApplication> AppsById = new();

    /// <summary>
    /// Represents a thread-safe collection of built-in roles, keyed by their string identifiers.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentDictionary<string, OktaBuiltinRole> RolesById = new();

    /// <summary>
    /// Represents a thread-safe collection of custom roles, keyed by their string identifiers.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentDictionary<string, OktaCustomRole> CustomRolesById = new();

    /// <summary>
    /// Represents a thread-safe collection of Okta role assignments.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaRoleAssignment> RoleAssignments = [];

    /// <summary>
    /// Represents a thread-safe collection of identity providers, keyed by their string identifiers.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaIdentityProvider> IdentityProviders = [];

    /// <summary>
    /// Represents a thread-safe collection of devices.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaDevice> Devices = [];

    /// <summary>
    /// Represents a thread-safe collection of authorization servers.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaAuthorizationServer> AuthorizationServers = [];

    /// <summary>
    /// Represents a thread-safe collection of custom resource sets.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaResourceSet> ResourceSets = [];

    /// <summary>
    /// Represents a thread-safe collection of realms.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaRealm> Realms = [];

    /// <summary>
    /// Represents a thread-safe collection of policies.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaPolicy> Policies = [];

    /// <summary>
    /// Represents a thread-safe collection of API service integrations.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OktaApiServiceIntegration> ApiServiceIntegrations = [];

    /// <summary>
    /// Represents the Okta organization associated with the current context.
    /// </summary>
    [JsonIgnore]
    public readonly OktaOrganization Organization = organization;
}
