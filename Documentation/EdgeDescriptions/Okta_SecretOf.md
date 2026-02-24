## General Information

The traversable `Okta_SecretOf` edges represent the relationship between service applications or API service integrations and their associated client secrets, represented by the [Okta_ClientSecret](../Nodes/Okta_ClientSecret.md) nodes.

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
