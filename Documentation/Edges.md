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

[Okta_Contains]: Edges/Okta_Contains.md
[Okta_RealmContains]: Edges/Okta_RealmContains.md
[Okta_ManagerOf]: Edges/Okta_ManagerOf.md
[Okta_DeviceOf]: Edges/Okta_DeviceOf.md
[Okta_ApiTokenFor]: Edges/Okta_ApiTokenFor.md
[Okta_SecretOf]: Edges/Okta_SecretOf.md
[Okta_KeyOf]: Edges/Okta_KeyOf.md
[Okta_HasAgent]: Edges/Okta_HasAgent.md
[Okta_CreatorOf]: Edges/Okta_CreatorOf.md
[Okta_AppAssignment]: Edges/Okta_AppAssignment.md
[Okta_ResourceSetContains]: Edges/Okta_ResourceSetContains.md
[Okta_MemberOf]: Edges/Okta_MemberOf.md
[Okta_HasRole]: Edges/Okta_HasRole.md
[Okta_ResetPassword]: Edges/Okta_ResetPassword.md
[Okta_ResetFactors]: Edges/Okta_ResetFactors.md
[Okta_AddMember]: Edges/Okta_AddMember.md
[Okta_ManageApp]: Edges/Okta_ManageApp.md
[Okta_HasRoleAssignment]: Edges/Okta_HasRoleAssignment.md
[Okta_ScopedTo]: Edges/Okta_HasRoleAssignment.md
[Okta_IdentityProviderFor]: Edges/Okta_IdentityProviderFor.md
[Okta_IdpGroupAssignment]: Edges/Okta_IdpGroupAssignment.md
[Okta_GroupPush]: Edges/Okta_GroupPush.md
[Okta_GroupPull]: Edges/Okta_GroupPull.md
[Okta_ReadPasswordUpdates]: Edges/Okta_ReadPasswordUpdates.md
[Okta_MembershipSync]: Edges/Okta_MembershipSync.md
[Okta_UserPush]: Edges/Okta_UserPush.md
[Okta_UserPull]: Edges/Okta_UserPull.md
[Okta_UserSync]: Edges/Okta_UserSync.md
[Okta_OutboundSSO]: Edges/Okta_OutboundSSO.md
[Okta_SWA]: Edges/Okta_SWA.md
[Okta_OutboundOrgSSO]: Edges/Okta_OutboundOrgSSO.md
[Okta_OrgSWA]: Edges/Okta_OrgSWA.md
[Okta_SuperAdmin]: Edges/Okta_SuperAdmin.md
[Okta_OrgAdmin]: Edges/Okta_OrgAdmin.md
[Okta_AppAdmin]: Edges/Okta_AppAdmin.md
[Okta_GroupAdmin]: Edges/Okta_GroupAdmin.md
[Okta_GroupMembershipAdmin]: Edges/Okta_GroupMembershipAdmin.md
[Okta_HelpDeskAdmin]: Edges/Okta_HelpDeskAdmin.md
[Okta_MobileAdmin]: Edges/Okta_MobileAdmin.md
[Okta_PolicyMapping]: Edges/Okta_PolicyMapping.md
[Okta_InboundOrgSSO]: Edges/Okta_InboundOrgSSO.md
[Okta_InboundSSO]: Edges/Okta_InboundOrgSSO.md

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
