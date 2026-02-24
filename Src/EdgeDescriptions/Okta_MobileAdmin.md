## General Information

The traversable `Okta_MobileAdmin` edges represent Mobile Administrator role assignments.
Mobile Administrators can manage mobile device settings and configurations within their assigned scope.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    d1("Okta_Device Alice's iPhone")
    d2("Okta_Device Bob's MacBook")
    u1 -- Okta_MobileAdmin --> d1
    u1 -- Okta_MobileAdmin --> d2
```
