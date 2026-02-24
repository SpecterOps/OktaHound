## General Information

The traversable `Okta_HelpDeskAdmin` edges represent Help Desk Administrator role assignments.
Help Desk Administrators can perform password resets, unlock accounts, and reset MFA factors for users within their assigned scope.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    g1("Okta_Group Help Desk")
    u2("Okta_User alice\@contoso.com")
    u3("Okta_User bob\@contoso.com")
    u1 -- Okta_HelpDeskAdmin --> u2
    g1 -- Okta_HelpDeskAdmin --> u3
```
