using System;

namespace SpecterOps.OktaHound.Database;

[Flags]
public enum CollectionTarget
{
    None = 0,
    Organization = 1 << 0,
    Users = 1 << 1,
    Groups = 1 << 2,
    AgentPools = 1 << 3,
    Agents = 1 << 4,
    Devices = 1 << 5,
    ResourceSets = 1 << 6,
    Realms = 1 << 7,
    BuiltinRoles = 1 << 8,
    CustomRoles = 1 << 9,
    Applications = 1 << 10,
    ApiTokens = 1 << 11,
    AuthorizationServers = 1 << 12,
    IdentityProviders = 1 << 13,
    ApiServiceIntegrations = 1 << 14,
    Policies = 1 << 15,
    RoleAssignments = 1 << 16,
    ClientSecrets = 1 << 17,
    Jwks = 1 << 18,
    ApplicationGrants = 1 << 19,
    ApplicationUserAssignments = 1 << 20,
    ApplicationGroupAssignments = 1 << 21,
    UserGroupMemberships = 1 << 22,
    PrivilegedUsers = 1 << 23,
    UserFactors = 1 << 24,
    All = Organization
        | Users
        | Groups
        | AgentPools
        | Agents
        | Devices
        | ResourceSets
        | Realms
        | BuiltinRoles
        | CustomRoles
        | Applications
        | ApiTokens
        | AuthorizationServers
        | IdentityProviders
        | ApiServiceIntegrations
        | Policies
        | RoleAssignments
        | ClientSecrets
        | Jwks
        | ApplicationGrants
        | ApplicationUserAssignments
        | ApplicationGroupAssignments
        | UserGroupMemberships
        | PrivilegedUsers
        | UserFactors,
    AllButFactors = All & ~UserFactors
}
