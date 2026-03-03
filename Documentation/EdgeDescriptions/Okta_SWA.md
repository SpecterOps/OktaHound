# Okta_SWA

## Edge Schema

- Source: [Okta_User](../NodeDescriptions/Okta_User.md)
- Destination: [GH_User](https://github.com/SpecterOps/GitHound), [jamf_Account](https://github.com/SpecterOps/JamfHound), [OP_User](https://github.com/SpecterOps/1PassHound), [SNOW_User](https://github.com/SpecterOps/SnowHound)

## General Information

The non-traversable hybrid `Okta_SWA` edges represent Secure Web Authentication relationships between Okta users and their linked accounts in external applications. SWA stores user credentials in Okta and automatically fills them in, which is less secure than federated SSO.

```mermaid
graph LR
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        u2("Okta_User alice\@contoso.com")
    end
    subgraph op["1Password Business"]
        opu1("OP_User john\@contoso.com")
        opu2("OP_User alice\@contoso.com")
    end
    u1 -. Okta_SWA .-> opu1
    u2 -. Okta_SWA .-> opu2
```
