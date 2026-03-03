# Hybrid Edges

Hybrid edges connect Okta entities to entities from other supported BloodHound collectors, such as Active Directory, Snowflake, GitHub, 1Password, and Jamf.

## Active Directory

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [User]         | [Okta_User]        | ❌         |
|                | [Okta_User]     | [User]            | ❌         |
| [Okta_MembershipSync] | [Group]  | [Okta_Group]       | ✅         |
|                      | [Okta_Group] | [Group]        | ✅         |
| [Okta_HostsAgent] | [Computer] | [Okta_Agent] | ✅ |
| [Okta_KerberosSSO] | [User] | [Okta_Application] | ✅ |

## Microsoft Entra ID (Azure Active Directory)

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

## GitHub

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_OutboundSSO] | [Okta_User] | [GH_User] | ✅ |
| [Okta_SWA] | [Okta_User] | [GH_User] | ❌ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [GH_Organization] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [GH_Organization] | ❌ |

> [!WARNING]
> GitHub user synchronization has not yet been tested with `OktaHound`.

## Jamf Pro

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_OutboundSSO] | [Okta_User] | [jamf_Account] | ✅ |
| [Okta_SWA] | [Okta_User] | [jamf_Account] | ❌ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [jamf_SSOIntegration] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [jamf_SSOIntegration] | ❌ |

## 1Password Business

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [Okta_User] | [OP_User] | ❌ |
| [Okta_SWA] | [Okta_User] | [OP_User] | ❓ |
| [Okta_OrgSWA] | [Okta_Application] | [OP_Account] | ❌ |

> [!WARNING]
> 1Password Business user provisioning has not yet been tested with `OktaHound`.

## Snowflake

| Edge Type | Source Node Kinds | Target Node Kinds | Traversable |
|-----------|-------------------|-------------------|-------------|
| [Okta_UserSync] | [Okta_User] | [SNOW_User] | ❌ |
|              | [SNOW_User] | [Okta_User] | ❌ |
| [Okta_OutboundSSO] | [Okta_User] | [SNOW_User] | ✅ |
| [Okta_SWA] | [Okta_User] | [SNOW_User] | ❓ |
| [Okta_OutboundOrgSSO] | [Okta_Application] | [SNOW_Account] | ✅ |
| [Okta_OrgSWA] | [Okta_Application] | [SNOW_Account] | ❌ |
| [Okta_MembershipSync] | [Okta_Group] | [SNOW_Group] | ✅ |
|                      | [SNOW_Group] | [Okta_Group] | ✅ |

## Okta Org2Org

> [!WARNING]
> Okta Org2Org integration is not currently supported by `OktaHound` due to missing license requirements.

[Okta_User]: <../Documentation/NodeDescriptions/Okta_User.md>
[Okta_Group]: <../Documentation/NodeDescriptions/Okta_Group.md>
[Okta_Application]: <../Documentation/NodeDescriptions/Okta_Application.md>
[Okta_Agent]: <../Documentation/NodeDescriptions/Okta_Agent.md>
[Okta_IdentityProvider]: <../Documentation/NodeDescriptions/Okta_IdentityProvider.md>
[User]: https://bloodhound.specterops.io/resources/nodes/user
[Group]: https://bloodhound.specterops.io/resources/nodes/group
[Computer]: https://bloodhound.specterops.io/resources/nodes/computer
[AZTenant]: https://bloodhound.specterops.io/resources/nodes/az-tenant
[AZUser]: https://bloodhound.specterops.io/resources/nodes/az-user
[AZGroup]: https://bloodhound.specterops.io/resources/nodes/az-group
[SNOW_User]: https://github.com/SpecterOps/SnowHound
[SNOW_Group]: https://github.com/SpecterOps/SnowHound
[SNOW_Account]: https://github.com/SpecterOps/SnowHound
[jamf_SSOIntegration]: https://github.com/SpecterOps/JamfHound
[jamf_Account]: https://github.com/SpecterOps/JamfHound
[GH_User]: https://github.com/SpecterOps/GitHound
[GH_Organization]: https://github.com/SpecterOps/GitHound
[OP_Account]: https://github.com/SpecterOps/1PassHound
[OP_User]: https://github.com/SpecterOps/1PassHound
[Okta_HostsAgent]: <../Documentation/EdgeDescriptions/Okta_HostsAgent.md>
[Okta_KerberosSSO]: <../Documentation/EdgeDescriptions/Okta_KerberosSSO.md>
[Okta_MembershipSync]: <../Documentation/EdgeDescriptions/Okta_MembershipSync.md>
[Okta_UserSync]: <../Documentation/EdgeDescriptions/Okta_UserSync.md>
[Okta_OutboundSSO]: <../Documentation/EdgeDescriptions/Okta_OutboundSSO.md>
[Okta_SWA]: <../Documentation/EdgeDescriptions/Okta_SWA.md>
[Okta_OutboundOrgSSO]: <../Documentation/EdgeDescriptions/Okta_OutboundOrgSSO.md>
[Okta_OrgSWA]: <../Documentation/EdgeDescriptions/Okta_OrgSWA.md>
[Okta_InboundOrgSSO]: <../Documentation/EdgeDescriptions/Okta_InboundOrgSSO.md>
[Okta_InboundSSO]: <../Documentation/EdgeDescriptions/Okta_InboundOrgSSO.md>
