# Okta_AgentPoolFor

## Edge Schema

- Source: [Okta_AgentPool](../NodeDescriptions/Okta_AgentPool.md)
- Destination: [Okta_Application](../NodeDescriptions/Okta_Application.md)

## General Information

`Okta_AgentPoolFor` edges connect an AD `Okta_AgentPool` to the backing `Okta_Application` used for directory integration.

```mermaid
graph LR
    ap1("Okta_AgentPool contoso.com")
    a1("Okta_Agent CONTOSO-SRV1")
    a2("Okta_Agent CONTOSO-SRV2")
    app1("Okta_Application AD contoso.com")
    a1 -- Okta_AgentMemberOf --> ap1
    a2 -- Okta_AgentMemberOf --> ap1
    ap1 -- Okta_AgentPoolFor --> app1
```
