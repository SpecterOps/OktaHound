## General Information

The traversable `Okta_GroupAdmin` edges represent Group Administrator (also known as User Administrator) role assignments.
Group Administrators can manage users and groups within their assigned scope.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@contoso.com")
    g1("Okta_Group Marketing")
    u1 -- Okta_GroupAdmin --> u2
    u1 -- Okta_GroupAdmin --> g1
    u2-. Okta_MemberOf .-> g1
```

Target group memberships are flattened when the assignment is evaluated.
