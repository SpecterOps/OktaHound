## General Information

The traversable `Okta_SuperAdmin` edges represent Super Administrator role assignments to the Okta organization.
Super Administrators have full access to all features and settings in the Okta organization.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    app1("Okta_Application Service Account")
    org("Okta_Organization contoso.okta.com")
    u1 -- Okta_SuperAdmin --> org
    app1 -- Okta_SuperAdmin --> org
```
