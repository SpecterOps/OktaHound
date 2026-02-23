# Okta_IdentityProvider Node

## Overview

Identity Providers (IdPs) in Okta represent external authentication sources that can be used to authenticate users. These can include social identity providers (such as Google, Facebook, or Microsoft), enterprise identity providers using SAML or OIDC, or other Okta organizations in an Org2Org configuration.

When users authenticate through an external identity provider, Okta can optionally create or link user accounts, enabling federated authentication across multiple systems.

In `OktaHound`, identity providers are represented as `Okta_IdentityProvider` nodes.

> [!WARNING]
> The inbound identity provider routing rules and JIT (Just-In-Time) provisioning settings are currently not evaluated by `OktaHound`.

## Okta_IdentityProviderFor Edges

The traversable `Okta_IdentityProviderFor` edges represent the relationships between identity providers and the users who authenticate through them:

```mermaid
graph LR
    idp1("Okta_IdentityProvider Google")
    idp2("Okta_IdentityProvider Contoso SAML")
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@gmail.com")
    u3("Okta_User bob\@contoso.com")
    idp1 -- Okta_IdentityProviderFor --> u2
    idp2 -- Okta_IdentityProviderFor --> u1
    idp2 -- Okta_IdentityProviderFor --> u3
```

## Okta_IdpGroupAssignment Edges

The non-traversable `Okta_IdpGroupAssignment` edges represent groups automatically assigned to users based on identity provider attributes or user claims:

```mermaid
graph LR
    idp1("Okta_IdentityProvider Microsoft Login")
    g1("Okta_Group Contractors")
    g2("Okta_Group Employees")
    g3("Okta_Group Entra ID Users")
    idp1 -- Okta_IdpGroupAssignment --> g1
    idp1 -- Okta_IdpGroupAssignment --> g2
    idp1 -- Okta_IdpGroupAssignment --> g3
```

## Hybrid Identity Edges

The `Okta_InboundOrgSSO` and `Okta_InboundSSO` hybrid edges connect external tenants and users to Okta entities:

```mermaid
graph LR
    t1("AZTenant Contoso")
    idp1("Okta_IdentityProvider Microsoft Login")
    u1("AZUser alice\@contoso.com")
    ou1("Okta_User alice\@contoso.com")
    t1 -- Okta_InboundOrgSSO --> idp1
    u1 -- Okta_InboundSSO --> ou1
```
