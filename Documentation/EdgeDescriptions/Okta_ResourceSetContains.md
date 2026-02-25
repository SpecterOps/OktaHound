## Edge Schema

- Source: [Okta_ResourceSet](../NodeDescriptions/Okta_ResourceSet.md)
- Destination: [Okta_User](../NodeDescriptions/Okta_User.md), [Okta_Group](../NodeDescriptions/Okta_Group.md), [Okta_Application](../NodeDescriptions/Okta_Application.md), [Okta_ApiServiceIntegration](../NodeDescriptions/Okta_ApiServiceIntegration.md), [Okta_Device](../NodeDescriptions/Okta_Device.md), [Okta_AuthorizationServer](../NodeDescriptions/Okta_AuthorizationServer.md), [Okta_IdentityProvider](../NodeDescriptions/Okta_IdentityProvider.md), [Okta_Policy](../NodeDescriptions/Okta_Policy.md)

## General Information

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
