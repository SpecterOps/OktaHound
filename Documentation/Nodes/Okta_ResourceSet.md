# Okta_ResourceSet Node

## Overview

Resource sets are collections of entities that can be used to scope custom role assignments in Okta.
A resource set can contain the following object types:

- [x] [Users](Okta_User.md)
- [x] [Groups](Okta_Group.md)
- [x] [Applications](Okta_Application.md)
- [x] [API Service Integrations](Okta_ApiServiceIntegration.md)
- [x] [Devices](Okta_Device.md)
- [x] [Authorization servers](Okta_AuthorizationServer.md)
- [x] [Identity Providers](Okta_IdentityProvider.md)
- [x] [Policies](Okta_Policy.md)
  - [x] Entity risk policy
  - [x] Session protection policy
  - [x] Authentication policy
  - [x] Global session policy
  - [x] End user account management policy
- [ ] Shared Signals Framework (SSF) Receivers
- [ ] ~~Workflows~~ (Gaps in the Okta API)
- [ ] ~~Customizations~~ (Gaps in the Okta API)
- [ ] ~~Support cases~~ (Gaps in the Okta API)
- [ ] ~~Identity and Access Management Resources~~ (Gaps in the Okta API)

> [!NOTE]
> Only the marked resource types are currently supported by `OktaHound` as resource set members.
> Some resource types, such as Workflows, are not accessible via the Okta API at all.

![Okta Resource Set displayed in BloodHound](../Screenshots/bloodhound-resource-set.png)

In `OktaHound`, resource sets are represented as `Okta_ResourceSet` nodes.

> [!NOTE]
> The built-in resource set `Workflows Resource Set` has the `WORKFLOWS_IAM_POLICY` identifier in all Okta organizations.
> To make it unique, the `OktaHound` collector adds the organization domain name as a suffix to the resource set's ID, e.g., `WORKFLOWS_IAM_POLICY@contoso.okta.com`.

## Okta_ResourceSetContains Edges

The traversable `Okta_ResourceSetContains` edges represent the membership relationships between resource sets and their member entities in Okta:

```mermaid
graph LR
    rs1("Okta_ResourceSet Sales Department Resources")
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@contoso.com")
    g1("Okta_Group Sales Team")
    a1("Okta_Application GitHub")
    d1("Okta_Device John's MacBook")
    rs1 -- Okta_ResourceSetContains --> u1
    rs1 -. Okta_ResourceSetContains .-> g1
    rs1 -- Okta_ResourceSetContains --> a1
    rs1 -- Okta_ResourceSetContains --> d1
    u2 -- Okta_MemberOf --> g1
    rs1 -- Okta_ResourceSetContains --> u2
```

Note that users can also be members of resource sets indirectly through group memberships.
The intermediate group will not appear in the graph, but the user membership will be resolved by `OktaHound`.
