# Okta_AgentMemberOf

## Edge Schema

- Source: [Okta_Agent](../NodeDescriptions/Okta_Agent.md)
- Destination: [Okta_AgentPool](../NodeDescriptions/Okta_AgentPool.md)

## General Information

`Okta_AgentMemberOf` edges represent membership of an `Okta_Agent` in an `Okta_AgentPool`.

Active Directory Agent Pools and their agents can be visualized in BloodHound as follows:

```mermaid
graph LR
    ap1("Okta_AgentPool contoso.com")
    ap2("Okta_AgentPool adatum.com")
    a1("Okta_Agent CONTOSO-SRV1")
    a2("Okta_Agent CONTOSO-SRV2")
    a3("Okta_Agent ADATUM-SRV1")
    a1 -- Okta_AgentMemberOf --> ap1
    a2 -- Okta_AgentMemberOf --> ap1
    a3 -- Okta_AgentMemberOf --> ap2
```

> [!WARNING]
> Traversable edges between the `Okta_AgentPool` and AD `Domain` nodes are not created in the current version of `OktaHound`.
> This functionality is planned for a future release.
