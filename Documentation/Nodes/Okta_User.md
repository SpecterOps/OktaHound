# Okta_User Node

## Overview

User objects (AKA People) represent individuals who have access to the Okta organization. Each user has a unique identifier, username in the email address format, and various attributes such as email, first name, last name, and status.

In `OktaHound`, users are represented as `Okta_User` nodes.

## User Status

User status can have [multiple values](https://developer.okta.com/docs/api/openapi/okta-management/management/tag/User), as illustrated below:

![Okta user status](https://developer.okta.com/docs/api/images/users/okta-user-status.png)

To simplify analysis in BloodHound, the `OktaHound` collector maps the **Status** attribute to the virtual boolean **Enabled** attribute as follows:

| Okta User Status | Enabled | Explanation                      |
|------------------|---------|----------------------------------|
| ACTIVE           | ✅     | User can authenticate.           |
| PASSWORD_EXPIRED | ✅     | User's password has expired but can still authenticate. |
| LOCKED_OUT       | ✅     | User is locked out but can still authenticate after unlocking. |
| PROVISIONED      | ✅     | User is provisioned but cannot authenticate yet. |
| RECOVERY         | ✅     | User is in recovery mode and cannot authenticate. |
| SUSPENDED        | ❌     | User is suspended and cannot authenticate. |
| STAGED           | ❌     | User is staged and cannot authenticate yet. |
| DEPROVISIONED    | ❌     | User is deprovisioned and cannot authenticate. |

> [!WARNING]
> This mapping is a simplification and may not cover all edge cases.
> Always refer to the actual **Status** attribute for precise user state information.

## Authentication Factors

Okta supports various authentication factors for multi-factor authentication (MFA),
such as SMS, email, push notifications, and hardware tokens.
In case of mobile and desktop applications, these authentication factors are associated with the [Device](Okta_Device.md) entities.
Other authentication factors, such as YubiKeys and Google Authenticator, are not represented as separate nodes in BloodHound,
but the number of enrolled factors is stored in the `authenticationFactors` attribute of the `Okta_User` nodes.

## Synchronization with External Directories

Users can be synchronized from external directories such as Active Directory (AD) or LDAP. When synchronized, certain attributes may be mapped from the external directory to the Okta user profile.

![Additional Active Directory attributes](../Screenshots/user-ad-attributes.png)

## Okta_ManagerOf Edges

Okta uses the `Manager` and `ManagerId` user profile attributes to represent managerial relationships. Unfortunately, these attributes can have any arbitrary value and their referential integrity is not enforced by Okta. They are not even synchronized from external directories by default.

Our recommendation is to map the `ManagerId` attribute to the login of the manager in Okta. When synchronizing users from Active Directory,
the `getManagerUser("active_directory").login` mapping expression can be used to achieve this. Such values are automatically recognized by `OktaHound`.

The **non-traversable** `Okta_ManagerOf` edges represent the organizational structure in BloodHound:

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User steve\@contoso.com")
    u3("Okta_User mary\@contoso.com")
    u4("Okta_User bob\@contoso.com")
    u5("Okta_User alice\@contoso.com")
    u1 -. Okta_ManagerOf .-> u2
    u1 -. Okta_ManagerOf .-> u3
    u3 -. Okta_ManagerOf .-> u4
    u3 -. Okta_ManagerOf .-> u5
```

## Okta_UserPush Edges

The non-traversable `Okta_UserPush` edges represent user provisioning relationships from Okta to external applications. When configured, Okta can automatically create, update, or deactivate user accounts in integrated applications using protocols like SCIM or LDAP.

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@contoso.com")
    app1("Okta_Application GitHub Enterprise Cloud")
    app2("Okta_Application Salesforce")
    u1 -. Okta_UserPush .-> app1
    u2 -. Okta_UserPush .-> app1
    u2 -. Okta_UserPush .-> app2
```

## Okta_UserPull Edges

The `Okta_UserPull` edges represent user import relationships from external applications to Okta.

```mermaid
graph LR
    app1("Okta_Application Workday")
    u1("Okta_User john\@contoso.com")
    u2("Okta_User alice\@contoso.com")
    app1 -- Okta_UserPull --> u1
    app1 -- Okta_UserPull --> u2
```

## Okta_UserSync Edges

The non-traversable hybrid `Okta_UserSync` edges represent bidirectional user synchronization relationships between Okta and external directories or applications. These edges indicate that user accounts are linked and synchronized between systems.

```mermaid
graph LR
    subgraph ad["Active Directory"]
        adu1("User john\@contoso.com")
    end
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        adu1 -. Okta_UserSync .-> u1
    end
    subgraph snowflake["Snowflake"]
        snu1("SNOWUser john\@contoso.com")
        u1 -. Okta_UserSync .-> snu1
    end
```

## Okta_OutboundSSO Edges

The traversable hybrid `Okta_OutboundSSO` edges represent Single Sign-On relationships between Okta users and their linked accounts in external applications using federated authentication (SAML 2.0 or OIDC).

```mermaid
graph LR
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        u2("Okta_User alice\@contoso.com")
    end
    subgraph github["GitHub"]
        ghu1("GHUser john\@contoso.com")
        ghu2("GHUser alice\@contoso.com")
    end
    subgraph jamf["Jamf"]
        jamfu1("jamf_Account john\@contoso.com")
    end
    subgraph snowflake["Snowflake"]
        snu1("SNOWUser john\@contoso.com")
    end
    u1 -- Okta_OutboundSSO --> ghu1
    u1 -- Okta_OutboundSSO --> jamfu1
    u2 -- Okta_OutboundSSO --> ghu2
    u1 -- Okta_OutboundSSO --> snu1
```

## Okta_SWA Edges

The non-traversable hybrid `Okta_SWA` edges represent Secure Web Authentication relationships between Okta users and their linked accounts in external applications. SWA stores user credentials in Okta and automatically fills them in, which is less secure than federated SSO.

```mermaid
graph LR
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        u2("Okta_User alice\@contoso.com")
    end
    subgraph op["1Password Business"]
        opu1("OPUser john\@contoso.com")
        opu2("OPUser alice\@contoso.com")
    end
    u1 -. Okta_SWA .-> opu1
    u2 -. Okta_SWA .-> opu2
```
