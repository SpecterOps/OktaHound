## General Information

The traversable `Okta_ReadPasswordUpdates` edges represent applications that can read password updates over SCIM.

```mermaid
graph LR
  org("Okta_Organization contoso.okta.com")
  app("Okta_Application SCIM App")
  user("Okta_User john\@contoso.com")
  user2("Okta_User steve\@contoso.com")
  app -- Okta_ReadPasswordUpdates --> user
  user -- Okta_SuperAdmin --> org
  user2 -- Okta_AppAdmin --> app
```
