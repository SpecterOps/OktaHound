## General Information

The traversable `Okta_AppAdmin` edges represent Application Administrator role assignments.
Application Administrators can manage application configurations, user assignments, and provisioning settings for their assigned applications.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@contoso.com")
    g1("Okta_Group Salesforce Admins")
    app1("Okta_Application GitHub")
    app2("Okta_Application Salesforce")
    is1("Okta_APIServiceIntegration Elastic Agent")
    u2 -- Okta_MemberOf --> g1
    u1 -- Okta_AppAdmin --> app1
    g1 -- Okta_AppAdmin --> app2
    u1 -- Okta_AppAdmin --> is1
```
