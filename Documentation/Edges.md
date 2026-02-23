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

[Okta_Contains]: <EdgeDescriptions/Okta_Contains.md>
[Okta_RealmContains]: <EdgeDescriptions/Okta_RealmContains.md>
[Okta_ManagerOf]: <EdgeDescriptions/Okta_ManagerOf.md>
[Okta_DeviceOf]: <EdgeDescriptions/Okta_DeviceOf.md>
[Okta_ApiTokenFor]: <EdgeDescriptions/Okta_ApiTokenFor.md>
[Okta_SecretOf]: <EdgeDescriptions/Okta_SecretOf.md>
[Okta_KeyOf]: <EdgeDescriptions/Okta_KeyOf.md>
[Okta_HasAgent]: <EdgeDescriptions/Okta_HasAgent.md>
[Okta_CreatorOf]: <EdgeDescriptions/Okta_CreatorOf.md>
[Okta_AppAssignment]: <EdgeDescriptions/Okta_AppAssignment.md>
[Okta_ResourceSetContains]: <EdgeDescriptions/Okta_ResourceSetContains.md>
[Okta_MemberOf]: <EdgeDescriptions/Okta_MemberOf.md>
[Okta_HasRole]: <EdgeDescriptions/Okta_HasRole.md>
[Okta_ResetPassword]: <EdgeDescriptions/Okta_ResetPassword.md>
[Okta_ResetFactors]: <EdgeDescriptions/Okta_ResetFactors.md>
[Okta_AddMember]: <EdgeDescriptions/Okta_AddMember.md>
[Okta_ManageApp]: <EdgeDescriptions/Okta_ManageApp.md>
[Okta_HasRoleAssignment]: <EdgeDescriptions/Okta_HasRoleAssignment.md>
[Okta_ScopedTo]: <EdgeDescriptions/Okta_HasRoleAssignment.md>
[Okta_IdentityProviderFor]: <EdgeDescriptions/Okta_IdentityProviderFor.md>
[Okta_IdpGroupAssignment]: <EdgeDescriptions/Okta_IdpGroupAssignment.md>
[Okta_GroupPush]: <EdgeDescriptions/Okta_GroupPush.md>
[Okta_GroupPull]: <EdgeDescriptions/Okta_GroupPull.md>
[Okta_ReadPasswordUpdates]: <EdgeDescriptions/Okta_ReadPasswordUpdates.md>
[Okta_MembershipSync]: <EdgeDescriptions/Okta_MembershipSync.md>
[Okta_UserPush]: <EdgeDescriptions/Okta_UserPush.md>
[Okta_UserPull]: <EdgeDescriptions/Okta_UserPull.md>
[Okta_UserSync]: <EdgeDescriptions/Okta_UserSync.md>
[Okta_OutboundSSO]: <EdgeDescriptions/Okta_OutboundSSO.md>
[Okta_SWA]: <EdgeDescriptions/Okta_SWA.md>
[Okta_OutboundOrgSSO]: <EdgeDescriptions/Okta_OutboundOrgSSO.md>
[Okta_OrgSWA]: <EdgeDescriptions/Okta_OrgSWA.md>
[Okta_SuperAdmin]: <EdgeDescriptions/Okta_SuperAdmin.md>
[Okta_OrgAdmin]: <EdgeDescriptions/Okta_OrgAdmin.md>
[Okta_AppAdmin]: <EdgeDescriptions/Okta_AppAdmin.md>
[Okta_GroupAdmin]: <EdgeDescriptions/Okta_GroupAdmin.md>
[Okta_GroupMembershipAdmin]: <EdgeDescriptions/Okta_GroupMembershipAdmin.md>
[Okta_HelpDeskAdmin]: <EdgeDescriptions/Okta_HelpDeskAdmin.md>
[Okta_MobileAdmin]: <EdgeDescriptions/Okta_MobileAdmin.md>
[Okta_PolicyMapping]: <EdgeDescriptions/Okta_PolicyMapping.md>
[Okta_InboundOrgSSO]: <EdgeDescriptions/Okta_InboundOrgSSO.md>
[Okta_InboundSSO]: <EdgeDescriptions/Okta_InboundOrgSSO.md>

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

[Okta_Organization]: <NodesDescriptions/Okta_Organization.md>
[Okta_User]: <NodesDescriptions/Okta_User.md>
[Okta_Group]: <NodesDescriptions/Okta_Group.md>
[Okta_Device]: <NodesDescriptions/Okta_Device.md>
[Okta_Application]: <NodesDescriptions/Okta_Application.md>
[Okta_ApiServiceIntegration]: <NodesDescriptions/Okta_ApiServiceIntegration.md>
[Okta_ResourceSet]: <NodesDescriptions/Okta_ResourceSet.md>
[Okta_Role]: <NodesDescriptions/Okta_Role.md>
[Okta_CustomRole]: <NodesDescriptions/Okta_CustomRole.md>
[Okta_RoleAssignment]: <NodesDescriptions/Okta_RoleAssignment.md>
[Okta_Realm]: <NodesDescriptions/Okta_Realm.md>
[Okta_Agent]: <NodesDescriptions/Okta_Agent.md>
[Okta_AgentPool]: <NodesDescriptions/Okta_AgentPool.md>
[Okta_IdentityProvider]: <NodesDescriptions/Okta_IdentityProvider.md>
[Okta_AuthorizationServer]: <NodesDescriptions/Okta_AuthorizationServer.md>
[Okta_Policy]: <NodesDescriptions/Okta_Policy.md>
[Okta_ClientSecret]: <NodesDescriptions/Okta_ClientSecret.md>
[Okta_JWK]: <NodesDescriptions/Okta_JWK.md>
[Okta_ApiToken]: <NodesDescriptions/Okta_ApiToken.md>
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
