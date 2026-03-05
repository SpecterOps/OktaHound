# Okta_OutboundSSO

## Edge Schema

- Source: [Okta_User](../NodeDescriptions/Okta_User.md)
- Destination: [AZUser](https://bloodhound.specterops.io/resources/nodes/az-user), [GH_User](https://github.com/SpecterOps/GitHound), [jamf_Account](https://github.com/SpecterOps/JamfHound), [SNOW_User](https://github.com/SpecterOps/SnowHound), [Okta_User](../NodeDescriptions/Okta_User.md)

## General Information

The traversable hybrid `Okta_OutboundSSO` edges represent Single Sign-On relationships between Okta users and their linked accounts in external applications using federated authentication (SAML 2.0 or OIDC).

```mermaid
graph LR
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        u2("Okta_User alice\@contoso.com")
    end
    subgraph github["GitHub"]
        ghu1("GH_User john\@contoso.com")
        ghu2("GH_User alice\@contoso.com")
    end
    subgraph jamf["Jamf"]
        jamfu1("jamf_Account john\@contoso.com")
    end
    subgraph snowflake["Snowflake"]
        snu1("SNOW_User john\@contoso.com")
    end
    u1 -- Okta_OutboundSSO --> ghu1
    u1 -- Okta_OutboundSSO --> jamfu1
    u2 -- Okta_OutboundSSO --> ghu2
    u1 -- Okta_OutboundSSO --> snu1
```
