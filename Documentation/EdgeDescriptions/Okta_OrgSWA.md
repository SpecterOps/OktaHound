# Okta_OrgSWA

## Edge Schema

- Source: [Okta_Application](../NodeDescriptions/Okta_Application.md)
- Destination: [GHOrganization](https://github.com/SpecterOps/GitHound), [jamf_SSOIntegration](https://github.com/SpecterOps/JamfHound), [OPAccount](https://github.com/SpecterOps/1PassHound), [SNOWAccount](https://github.com/SpecterOps/SnowHound)

## General Information

The non-traversable `Okta_OrgSWA` edges represent the Secure Web Authentication (SWA) relationships between Okta applications and supported external organizations or tenants. SWA stores user credentials in Okta and automatically fills them in when users access the application, which is less secure than federated SSO protocols.

```mermaid
graph LR
  subgraph okta["OktaHound"]
    direction TB
    o("Okta_Organization contoso.okta.com")
    app1("Okta_Application Jamf Pro SWA")
    o -- Okta_Contains --> app1
  end
  subgraph "JamfHound"
    direction TB
    jamf("jamf_SSOIntegration contoso.jamfcloud.com-SSO")
    app1 -. Okta_OrgSWA .-> jamf
  end
```

The respective BloodHound collectors, e.g., `GitHound` for GitHub organizations and `JamfHound` for Jamf Pro tenants,
must be used to gather the external node information.
