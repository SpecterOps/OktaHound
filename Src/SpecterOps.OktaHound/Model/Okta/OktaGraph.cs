using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaGraph(OktaOrganization organization) : OpenGraphBase<OktaGraphElements>(OktaGraphSerializationContext.Default, new OktaGraphElements(organization), OktaSourceKind)
{
    public const string OktaSourceKind = "Okta";

    [JsonIgnore]
    public IEnumerable<OktaUser> Users => Elements.UsersById.Select(item => item.Value);

    [JsonIgnore]
    public IEnumerable<OktaGroup> Groups => Elements.GroupsById.Select(item => item.Value);

    [JsonIgnore]
    public IEnumerable<OktaSecurityPrincipal> UsersAndGroups => [.. Users, .. Groups];

    [JsonIgnore]
    public IEnumerable<OktaNode> UsersAndGroupsAndDevices => [.. Users, .. Groups, .. Devices];

    [JsonIgnore]
    public IEnumerable<OktaApplication> Applications => Elements.AppsById.Select(item => item.Value);

    [JsonIgnore]
    public IEnumerable<OktaApiServiceIntegration> ApiServiceIntegrations => Elements.ApiServiceIntegrations;

    [JsonIgnore]
    public IEnumerable<OktaNode> ApplicationsAndApiServiceIntegrations => [.. Applications, .. ApiServiceIntegrations];

    [JsonIgnore]
    public IEnumerable<OktaIdentityProvider> IdentityProviders => Elements.IdentityProviders;

    [JsonIgnore]
    public IEnumerable<OktaDevice> Devices => Elements.Devices;

    [JsonIgnore]
    public IEnumerable<OktaRoleAssignment> RoleAssignments => Elements.RoleAssignments;

    [JsonIgnore]
    public IEnumerable<OktaCustomRole> CustomRoles => Elements.CustomRolesById.Select(item => item.Value);

    [JsonIgnore]
    public IEnumerable<OktaResourceSet> ResourceSets => Elements.ResourceSets;

    [JsonIgnore]
    public IEnumerable<OktaAuthorizationServer> AuthorizationServers => Elements.AuthorizationServers;

    [JsonIgnore]
    public IEnumerable<OktaPolicy> Policies => Elements.Policies;

    [JsonIgnore]
    public OktaOrganization Organization => Elements.Organization;

    public bool AddNode(OktaUser user)
    {
        if (user.Login is not null)
        {
            Elements.UsersByLogin.TryAdd(user.Login, user);
        }

        return Elements.UsersById.TryAdd(user.Id, user);
    }

    public bool AddNode(OktaGroup group) => Elements.GroupsById.TryAdd(group.Id, group);

    public bool AddNode(OktaApplication app) => Elements.AppsById.TryAdd(app.Id, app);

    public bool AddNode(OktaBuiltinRole role) => Elements.RolesById.TryAdd(role.Id, role);

    public void AddNode(OktaRoleAssignment roleAssignment) => Elements.RoleAssignments.Add(roleAssignment);

    public bool AddNode(OktaCustomRole customRole) => Elements.CustomRolesById.TryAdd(customRole.Id, customRole);

    public void AddNode(OktaDevice device) => Elements.Devices.Add(device);

    public void AddNode(OktaResourceSet resourceSet) => Elements.ResourceSets.Add(resourceSet);

    public void AddNode(OktaRealm realm) => Elements.Realms.Add(realm);

    public void AddNode(OktaPolicy policy) => Elements.Policies.Add(policy);

    public void AddNode(OktaApiServiceIntegration service) => Elements.ApiServiceIntegrations.Add(service);

    public void AddNode(OktaIdentityProvider identityProvider) => Elements.IdentityProviders.Add(identityProvider);

    public OktaUser? GetUserById(string userId)
    {
        Elements.UsersById.TryGetValue(userId, out var user);
        return user;
    }

    public OktaUser? GetUserByLogin(string login)
    {
        Elements.UsersByLogin.TryGetValue(login, out var user);
        return user;
    }

    public OktaGroup? GetGroup(string groupId)
    {
        Elements.GroupsById.TryGetValue(groupId, out var group);
        return group;
    }

    public IEnumerable<OktaUser> GetGroupMembers(string[] groupIds)
    {
        Elements.EdgesByKind.TryGetValue(OktaGroup.MemberOfEdgeKind, out var memberOfEdges);

        if (memberOfEdges is null)
        {
            return [];
        }

        IEnumerable<OpenGraphEdge> memberofEdgesFilteredByGroupId = memberOfEdges.Where(edge => groupIds.Contains(edge.End.Value));
        IEnumerable<string> userIds = memberofEdgesFilteredByGroupId.Select(edge => edge.Start.Value!); // We do not expect the memberId to be null

        // Get unique user IDs that are members of the specified groups
        HashSet<string> uniqueMemberIds = [];
        uniqueMemberIds.UnionWith(userIds);

        // Retrieve OktaUser objects for the unique user IDs
        return uniqueMemberIds.Select(userId => GetUserById(userId)).Where(user => user != null).Select(user => user!);
    }

    public OktaApplication? GetApplication(string appId)
    {
        bool found = Elements.AppsById.TryGetValue(appId, out OktaApplication? app);
        return found ? app : null;
    }

    // TODO: GetApplications performance could be improved by using a Dictionary.
    public IEnumerable<OktaApplication> GetApplications(string type) =>
        Elements.AppsById.Where(item => item.Value.ApplicationType == type).Select(item => item.Value);

    // TODO: GetApiServiceIntegration performance could be improved by using a Dictionary.
    // On the other hand, we do not expect many integrations to be present.
    public IEnumerable<OktaApiServiceIntegration> GetApiServiceIntegrations(string type) =>
        Elements.ApiServiceIntegrations.Where(service => service.IntegrationType == type);

    public IEnumerable<OktaNode> GetAppsAndIntegrations(string type) =>
        [.. GetApplications(type), .. GetApiServiceIntegrations(type)];

    public OktaBuiltinRole? GetBuiltInRole(RoleType roleType)
    {
        if (roleType is null)
        {
            return null;
        }

        string roleId = OktaBuiltinRole.MakeRoleIdUnique(roleType?.Value ?? "UNKNOWN_ROLE_TYPE", Organization.DomainName);
        Elements.RolesById.TryGetValue(roleId, out var role);
        return role;
    }

    public IEnumerable<OpenGraphEdgeNode> GetClientSecrets(string applicationId) =>
        Elements.EdgesByKind.TryGetValue(OktaClientSecret.SecretOfEdgeKind, out var secretOfEdges) ?
        secretOfEdges.Where(edge => edge.End.Value == applicationId)
            .Select(edge => edge.Start) :
        [];

    // TODO: GetCustomRole performance could be improved by using a Dictionary.
    public OktaCustomRole? GetCustomRole(string roleName) =>
        Elements.CustomRolesById.FirstOrDefault(item => item.Value.Name == roleName).Value;
}
