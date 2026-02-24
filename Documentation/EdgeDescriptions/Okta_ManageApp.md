## General Information

The traversable `Okta_ManageApp` edges correspond to the `okta.apps.manage` custom role permissions
that allow a principal (user, group, or application) to fully manage Okta applications and their members.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    g1("Okta_Group App Operators")
    app1("Okta_Application GitHub")
    app2("Okta_Application Salesforce")
    u1 -- Okta_ManageApp --> app1
    g1 -- Okta_ManageApp --> app2
```
