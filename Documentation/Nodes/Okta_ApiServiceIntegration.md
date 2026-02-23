# Okta_ApiServiceIntegration Node

## Overview

API service integrations in Okta represent OAuth 2.0 service (daemon) applications that can be granted machine-to-machine access to Okta APIs. There are some important differences between API service integrations and [regular OIDC service applications in Okta](Okta_Application.md):

| Feature                                      | Service Applications | API Service Integrations |
|----------------------------------------------|----------------------|--------------------------|
| Can be created manually:                     | ✅                  | ❌                       |
| Can be added from the OIN Catalog:           | ✅                  | ✅                       |
| Require role assignments:                    | ✅                  | ❌                       |
| Support authentication using client secrets: | ✅                  | ✅                       |
| Support authentication using private keys:   | ✅                  | ❌                       |

In `OktaHound`, API service integrations are represented as `Okta_ApiServiceIntegration` nodes.

## Integration OAuth 2.0 Scopes

Each API service integration comes with a pre-defined set of OAuth 2.0 scopes to access Okta APIs:

![Okta API service integration scopes in BloodHound](../Screenshots/bloodhound-api-service-integration-scopes.png)

## Okta_CreatorOf Edges

The non-traversable `Okta_CreatorOf` edges represent the creator relationships between API Service Integration instances and users in Okta:

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User steve\@contoso.com")
    is1("Okta_APIServiceIntegration Elastic Agent")
    is2("Okta_APIServiceIntegration Falcon Shield")
    u1 -. Okta_CreatorOf .-> is1
    u2 -. Okta_CreatorOf .-> is2
```

## Okta_SecretOf Edges

The traversable `Okta_SecretOf` edges represent the relationship between API service integrations and their associated client secrets:

```mermaid
graph LR
    o("Okta_Organization contoso.okta.com")
    cs1("Okta_ClientSecret Secret1")
    cs2("Okta_ClientSecret Secret2")
    cs3("Okta_ClientSecret Secret3")
    is1("Okta_APIServiceIntegration Elastic Agent")
    is2("Okta_APIServiceIntegration Falcon Shield")
    o -- Okta_Contains --> is1
    o -- Okta_Contains --> is2
    cs1 -- Okta_SecretOf --> is1
    cs2 -- Okta_SecretOf --> is2
    cs3 -- Okta_SecretOf --> is2
```
