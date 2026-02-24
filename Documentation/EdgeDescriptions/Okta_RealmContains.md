## General Information

The traversable `Okta_RealmContains` edges represent containment relationships between realms and the users assigned to those realms.

```mermaid
graph LR
	r1("Okta_Realm EU")
	r2("Okta_Realm US")
	u1("Okta_User john\@contoso.com")
	u2("Okta_User alice\@contoso.com")
	u3("Okta_User bob\@contoso.com")
	r1 -- Okta_RealmContains --> u1
	r1 -- Okta_RealmContains --> u2
	r2 -- Okta_RealmContains --> u3
```

> [!WARNING]
> Okta Realms are currently not supported by `OktaHound` due to licensing restrictions.
