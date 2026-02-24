## General Information

`Okta_AgentPool` nodes are connected to their constituent `Okta_Agent` nodes via `Okta_HasAgent` edges. Active Directory Agent Pools and their agents can be visualized in BloodHound as follows:

```mermaid
graph LR
    o("Okta_Organization contoso.okta.com")
    ap1("Okta_AgentPool contoso.com")
    ap2("Okta_AgentPool adatum.com")
    a1("Okta_Agent CONTOSO-SRV1")
    a2("Okta_Agent CONTOSO-SRV2")
    a3("Okta_Agent ADATUM-SRV1")
    o -- "Okta_Contains"--> ap1
    o -- "Okta_Contains"--> ap2
    ap1 -- "Okta_HasAgent"--> a1
    ap1 -- "Okta_HasAgent"--> a2
    ap2 -- "Okta_HasAgent"--> a3
```

> [!WARNING]
> Traversable edges between the `Okta_AgentPool` and AD `Domain` nodes are not created in the current version of `OktaHound`.
> This functionality is planned for a future release.
