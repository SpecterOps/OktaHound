# Custom BloodHound Edges for Okta

## Intra-Organization Edges

The following table summarizes the custom edge kinds used by `OktaHound`:

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_Contains] | [Okta_Organization] | [Okta_User], [Okta_Group], [Okta_Device], [Okta_Application], [Okta_ApiServiceIntegration], [Okta_ResourceSet], [Okta_Role], [Okta_CustomRole], [Okta_RoleAssignment], [Okta_Realm], [Okta_AgentPool], [Okta_IdentityProvider], [Okta_AuthorizationServer], [Okta_Policy] | ✅ |
| [Okta_RealmContains] | [Okta_Realm] | [Okta_User] | ✅ |
| [Okta_ManagerOf] | [Okta_User] | [Okta_User] | ❌ |
| [Okta_DeviceOf] | [Okta_Device] | [Okta_User] | ❌ |
| [Okta_ApiTokenFor] | [Okta_ApiToken] | [Okta_User] | ✅ |
| [Okta_SecretOf] | [Okta_ClientSecret] | [Okta_Application], [Okta_ApiServiceIntegration] | ✅ |
| [Okta_KeyOf] | [Okta_JWK] | [Okta_Application] | ✅ |
| [Okta_HasAgent] | [Okta_AgentPool] | [Okta_Agent] | ❓ |
| [Okta_CreatorOf] | [Okta_User], [Okta_Application], [Okta_ApiServiceIntegration] | [Okta_ApiServiceIntegration] | ❌ |
| [Okta_AppAssignment] | [Okta_User], [Okta_Group]  | [Okta_Application] | ❌ |
| [Okta_ResourceSetContains] | [Okta_ResourceSet] | [Okta_User], [Okta_Group], [Okta_Application], [Okta_ApiServiceIntegration], [Okta_Device], [Okta_AuthorizationServer], [Okta_IdentityProvider], [Okta_Policy] | ✅ |
| [Okta_MemberOf] | [Okta_User] | [Okta_Group] | ✅ |
| [Okta_HasRole] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Role], [Okta_CustomRole] | ❌ |
| [Okta_ResetPassword] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_User] | ✅ |
| [Okta_ResetFactors] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_User] | ✅ |
| [Okta_AddMember] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Group] | ✅ |
| [Okta_ManageApp] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Application] | ✅ |
| [Okta_HasRoleAssignment] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_RoleAssignment] | ❌ |
| [Okta_ScopedTo] | [Okta_RoleAssignment] | [Okta_Organization], [Okta_User], [Okta_Group], [Okta_ResourceSet], [Okta_Application], [Okta_ApiServiceIntegration], [Okta_Device], [Okta_AuthorizationServer] | ❌ |
| [Okta_AppAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Application], [Okta_ApiServiceIntegration] | ✅ |
| [Okta_GroupAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_User], [Okta_Group] | ✅ |
| [Okta_GroupMembershipAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Group] | ✅ |
| [Okta_HelpDeskAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_User] | ✅ |
| [Okta_SuperAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Organization] | ✅ |
| [Okta_OrgAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_User], [Okta_Group], [Okta_Device] | ✅ |
| [Okta_MobileAdmin] | [Okta_User], [Okta_Group], [Okta_Application] | [Okta_Device] | ✅ |
| [Okta_IdentityProviderFor] | [Okta_IdentityProvider] | [Okta_User] | ✅ |
| [Okta_IdpGroupAssignment] | [Okta_IdentityProvider] | [Okta_Group] | ❌ |
| [Okta_GroupPush] | [Okta_Group] | [Okta_Application] | ❌ |
| [Okta_GroupPull] | [Okta_Application] | [Okta_Group] | ✅ |
| [Okta_ReadPasswordUpdates] | [Okta_Application] | [Okta_User] | ✅ |
| [Okta_UserPush] | [Okta_User] | [Okta_Application] | ❌ |
| [Okta_UserPull] | [Okta_Application] | [Okta_User] | ❓ |
| [Okta_PolicyMapping] | [Okta_Policy] | [Okta_Application] | ❌ |

[Okta_Contains]: Nodes/Okta_Organization.md#oktacontains-edges
[Okta_RealmContains]: Nodes/Okta_Realm.md#oktarealmcontains-edges
[Okta_ManagerOf]: Nodes/Okta_User.md#oktamanagerof-edges
[Okta_DeviceOf]: Nodes/Okta_Device.md#oktadeviceof-edges
[Okta_ApiTokenFor]: Nodes/Okta_ApiToken.md#oktaapitokenfor-edges
[Okta_SecretOf]: Nodes/Okta_ClientSecret.md#oktasecretof-edges
[Okta_KeyOf]: Nodes/Okta_JWK.md#oktakeyof-edges
[Okta_HasAgent]: Nodes/Okta_AgentPool.md#oktahasagent-edges
[Okta_CreatorOf]: Nodes/Okta_ApiServiceIntegration.md#oktacreatorof-edges
[Okta_AppAssignment]: Nodes/Okta_Application.md#oktaappassignment-edges
[Okta_ResourceSetContains]: Nodes/Okta_ResourceSet.md#oktaresourcesetcontains-edges
[Okta_MemberOf]: Nodes/Okta_Group.md#oktamemberof-edges
[Okta_HasRole]: Nodes/Okta_Role.md#oktahasrole-edges
[Okta_ResetPassword]: Nodes/Okta_CustomRole.md#oktaresetpassword-edges
[Okta_ResetFactors]: Nodes/Okta_CustomRole.md#oktaresetfactors-edges
[Okta_AddMember]: Nodes/Okta_CustomRole.md#oktaaddmember-edges
[Okta_ManageApp]: Nodes/Okta_CustomRole.md#oktamanageapp-edges
[Okta_HasRoleAssignment]: Nodes/Okta_RoleAssignment.md#oktahasroleassignment-and-oktascopedto-edges
[Okta_ScopedTo]: Nodes/Okta_RoleAssignment.md#oktahasroleassignment-and-oktascopedto-edges
[Okta_IdentityProviderFor]: Nodes/Okta_IdentityProvider.md#oktaidentityproviderfor-edges
[Okta_IdpGroupAssignment]: Nodes/Okta_IdentityProvider.md#oktaidpgroupassignment-edges
[Okta_GroupPush]: Nodes/Okta_Group.md#oktagrouppush-edges
[Okta_GroupPull]: Nodes/Okta_Group.md#oktagrouppull-edges
[Okta_ReadPasswordUpdates]: Nodes/Okta_Application.md#oktareadpasswordupdates-edges
[Okta_MembershipSync]: Nodes/Okta_Group.md#oktamembershipsync-edges
[Okta_UserPush]: Nodes/Okta_User.md#oktauserpush-edges
[Okta_UserPull]: Nodes/Okta_User.md#oktauserpull-edges
[Okta_UserSync]: Nodes/Okta_User.md#oktausersync-edges
[Okta_OutboundSSO]: Nodes/Okta_User.md#oktaoutboundsso-edges
[Okta_SWA]: Nodes/Okta_User.md#oktaswa-edges
[Okta_OutboundOrgSSO]: Nodes/Okta_Application.md#oktaoutboundorgsso-edges
[Okta_OrgSWA]: Nodes/Okta_Application.md#oktaorgswa-edges
[Okta_SuperAdmin]: Nodes/Okta_Role.md#oktasuperadmin-edges
[Okta_OrgAdmin]: Nodes/Okta_Role.md#oktaorgadmin-edges
[Okta_AppAdmin]: Nodes/Okta_Role.md#oktaappadmin-edges
[Okta_GroupAdmin]: Nodes/Okta_Role.md#oktagroupadmin-edges
[Okta_GroupMembershipAdmin]: Nodes/Okta_Role.md#oktagroupmembershipadmin-edges
[Okta_HelpDeskAdmin]: Nodes/Okta_Role.md#oktahelpdeskadmin-edges
[Okta_MobileAdmin]: Nodes/Okta_Role.md#oktamobileadmin-edges
[Okta_PolicyMapping]: Nodes/Okta_Policy.md#oktapolicymapping-edges
[Okta_InboundOrgSSO]: Nodes/Okta_IdentityProvider.md#hybrid-identity-edges
[Okta_InboundSSO]: Nodes/Okta_IdentityProvider.md#hybrid-identity-edges

## Hybrid Edges

Hybrid edges connect Okta entities to entities from other supported BloodHound collectors, such as Active Directory, Snowflake, GitHub, 1Password, and Jamf.

### Active Directory

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [User]         | [Okta_User]        | ❌         |
|                | [Okta_User]     | [User]            | ❌         |
| [Okta_MembershipSync] | [Group]  | [Okta_Group]       | ✅         |
|                      | [Okta_Group] | [Group]        | ✅         |

### Microsoft Entra ID (Azure Active Directory)

| Edge Type            | Source Node Kinds | Target Node Kinds        | Traversable |
|----------------------|-------------------|--------------------------|-------------|
| [Okta_InboundOrgSSO]  | [AZTenant]        | [Okta_IdentityProvider]   | ✅          |
| [Okta_InboundSSO]     | [AZUser]          | [Okta_User]               | ✅          |
| [Okta_OutboundSSO]    | [Okta_User]        | [AZUser]                 | ✅          |
| [Okta_UserSync]       | [Okta_User]        | [AZUser]                 | ❌          |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [AZTenant]               | ✅          |
| [Okta_MembershipSync] | [AZGroup]         | [Okta_Group]              | ✅          |

> [!WARNING]
> Inbound hybrid user sync edge should be implemented.

### GitHub

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_OutboundSSO] | [Okta_User] | [GHUser] | ✅ |
| [Okta_SWA] | [Okta_User] | [GHUser] | ❌ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [GHOrganization] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [GHOrganization] | ❌ |

> [!WARNING]
> GitHub user synchronization has not yet been tested with `OktaHound`.

### Jamf Pro

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_OutboundSSO] | [Okta_User] | [jamf_Account] | ✅ |
| [Okta_SWA] | [Okta_User] | [jamf_Account] | ❌ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [jamf_Tenant] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [jamf_Tenant] | ❌ |

### 1Password Business

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [Okta_User] | [OPUser] | ❌ |
| [Okta_SWA] | [Okta_User] | [OPUser] | ❓ |
| [Okta_OrgSWA] | [Okta_Application] | [OPAccount] | ❌ |

> [!WARNING]
> 1Password Business user provisioning has not yet been tested with `OktaHound`.

### Snowflake

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [Okta_User] | [SNOWUser] | ❌ |
|              | [SNOWUser] | [Okta_User] | ❌ |
| [Okta_OutboundSSO] | [Okta_User] | [SNOWUser] | ✅ |
| [Okta_SWA] | [Okta_User] | [SNOWUser] | ❓ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [SNOWAccount] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [SNOWAccount] | ❌ |
| [Okta_MembershipSync] | [Okta_Group] | [SNOWGroup] | ✅ |
|                      | [SNOWGroup] | [Okta_Group] | ✅ |

### Okta Org2Org

> [!WARNING]
> Okta Org2Org integration is not currently supported by `OktaHound` due to missing license requirements.

[Okta_Organization]: Nodes/Okta_Organization.md
[Okta_User]: Nodes/Okta_User.md
[Okta_Group]: Nodes/Okta_Group.md
[Okta_Device]: Nodes/Okta_Device.md
[Okta_Application]: Nodes/Okta_Application.md
[Okta_ApiServiceIntegration]: Nodes/Okta_ApiServiceIntegration.md
[Okta_ResourceSet]: Nodes/Okta_ResourceSet.md
[Okta_Role]: Nodes/Okta_Role.md
[Okta_CustomRole]: Nodes/Okta_CustomRole.md
[Okta_RoleAssignment]: Nodes/Okta_RoleAssignment.md
[Okta_Realm]: Nodes/Okta_Realm.md
[Okta_Agent]: Nodes/Okta_Agent.md
[Okta_AgentPool]: Nodes/Okta_AgentPool.md
[Okta_IdentityProvider]: Nodes/Okta_IdentityProvider.md
[Okta_AuthorizationServer]: Nodes/Okta_AuthorizationServer.md
[Okta_Policy]: Nodes/Okta_Policy.md
[Okta_ClientSecret]: Nodes/Okta_ClientSecret.md
[Okta_JWK]: Nodes/Okta_JWK.md
[Okta_ApiToken]: Nodes/Okta_ApiToken.md
[User]: https://bloodhound.specterops.io/resources/nodes/user
[Group]: https://bloodhound.specterops.io/resources/nodes/group
[AZTenant]: https://bloodhound.specterops.io/resources/nodes/az-tenant
[AZUser]: https://bloodhound.specterops.io/resources/nodes/az-user
[AZGroup]: https://bloodhound.specterops.io/resources/nodes/az-group
[SNOWUser]: https://github.com/SpecterOps/SnowHound
[SNOWGroup]: https://github.com/SpecterOps/SnowHound
[SNOWAccount]: https://github.com/SpecterOps/SnowHound
[jamf_Tenant]: https://github.com/SpecterOps/JamfHound
[jamf_Account]: https://github.com/SpecterOps/JamfHound
[GHUser]: https://github.com/SpecterOps/GitHound
[GHOrganization]: https://github.com/SpecterOps/GitHound
[OPAccount]: https://github.com/SpecterOps/1PassHound
[OPUser]: https://github.com/SpecterOps/1PassHound
