# Okta_ClientSecret Node

## Overview

Client secrets are used by API service integrations and OIDC applications to authenticate with Okta and obtain access tokens.

![Okta client secret creation](../Screenshots/app-client-secret-creation.png)

An application can have up to two client secrets configured, to allow for secret rotation.

![Okta client secret rotation](../Screenshots/app-client-secret-rotation.png)

Client secrets are represented as `Okta_ClientSecret` nodes in BloodHound.

## Okta_SecretOf Edges

The traversable `Okta_SecretOf` edges represent the relationships between applications ([Okta_Application](Okta_Application.md) and [Okta_APIServiceIntegration](Okta_APIServiceIntegration.md)) and their client secrets:

```mermaid
graph LR
    is1("Okta_APIServiceIntegration Elastic Agent")
    is2("Okta_APIServiceIntegration Falcon Shield")
    cs1("Okta_ClientSecret pdWB5I2I1LJ_cUAzD9fB1w")
    cs2("Okta_ClientSecret lLRrn0i2tIa5YowaQuTdtQ")
    cs3("Okta_ClientSecret EpGPhXPYLxqY2JEWRjTSAQ")
    cs1 -- Okta_SecretOf --> is1
    cs2 -- Okta_SecretOf --> is2
    cs3 -- Okta_SecretOf --> is2
```

