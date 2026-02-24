## General Information

The non-traversable `Okta_GroupPush` edges represent the group push assignments to applications.
This indicates group provisioning and membership synchronization from Okta to external applications.

```mermaid
graph LR
    g1("Okta_Group Engineering")
    app1("Okta_Application contoso.com")
    g1 -- Okta_GroupPush --> app1
```
