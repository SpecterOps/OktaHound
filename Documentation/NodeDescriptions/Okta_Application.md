## Overview

Applications in Okta represent the various software applications and services that users can access through the Okta organization. Applications can be configured to use different authentication methods, such as SAML, OIDC, or SWA. These protocols can either be configured manually by administrators or automatically by adding an application from Okta's App Integration Catalog, which provides a wide range of pre-configured cloud and on-premises application templates.

With the exception of API Service applications, Okta users and groups can be assigned to applications. Users can also be synchronized TO and FROM applications in Okta, typically using the SCIM protocol. For example, when integrating with GitHub Enterprise Cloud, Okta can be configured to automatically create user accounts in GitHub when users are assigned to the GitHub application in Okta.

In `OktaHound`, applications are represented as `Okta_Application` nodes.

## User Name Mapping

User name mapping from Okta to SAML 2.0, OpenID Connect (OIDC), and Secure Web Authentication (SWA) applications is configurable in the Okta Admin Console, with the default setting being the Okta username pass-through, i.e., `${source.login}`.

| Application username format   | Mapping template                                            | Supported by OktaHound |
|-------------------------------|-------------------------------------------------------------|------------------------|
| Okta username                 | `${source.login}`                                           | Yes                    |
| Email                         | `${source.email}`                                           | Yes                    |
| Okta username prefix          | `${fn:substringBefore(source.login, "@")}`                  | Yes                    |
| Email prefix                  | `${fn:substringBefore(source.email, "@")}`                  | Yes                    |
| AD Employee ID                | `${source.employeeID}`                                      | No                     |
| AD SAM account name           | `${source.samAccountName}`                                  | No                     |
| AD SAM account name + domain  | `${source.samAccountName}@${source.instance.namingContext}` | No                     |
| AD user principal name        | `${source.userName}`                                        | No                     |
| AD user principal name prefix | `${fn:substringBefore(source.userName, "@")}`               | No                     |
| (None)                        | `NONE`                                                      | No                     |
| Custom                        | ?                                                           | No                     |

## API Service Applications

This application type is the most interesting one from the security perspective, as it represents OAuth 2.0 service (daemon) applications that can be granted machine-to-machine access to Okta APIs, without any user interaction. These applications can be assigned administrative roles, e.g., Super Admin, and OAuth 2.0 scope grants, e.g., `okta.users.manage`. Any API operation must be allowed by both the assigned roles and the granted scopes.

![Okta Application scopes and roles in BloodHound](../Images/bloodhound-app-scopes.png)

> [!WARNING]
> Research of role mapping and scope grants for API service applications in Okta is still ongoing.

## Hybrid Identities

![Okta AD agent settings](../Images/okta-ad-agent.png)

Possibly inbound and outbound control:

    - [x] AD to Okta user sync
    - [x] AD to Okta group sync
    - [ ]

- AD
  - [AD Desktop SSO](https://help.okta.com/oie/en-us/content/topics/directory/ad-dsso-about-workflow.htm)
  - Sync and delegated authentication Agents
- Entra ID
- GitHub IdP
- ...

```mermaid
graph TB
  subgraph ad["Active Directory"]
    direction LR
    domain("Domain contoso.com")
    adu1("User john\@contoso.com")
    adu2("User steve\@contoso.com")
    adg1("Group IT")
    domain -- Contains --> adu1
    domain -- Contains --> adu2
    domain -- Contains --> adg1
    adu1 -- MemberOf --> adg1
  end
  subgraph okta["Okta"]
    direction LR
    org("Okta_Organization contoso.okta.com")
    u1("Okta_User john\@contoso.com")
    u2("Okta_User steve\@contoso.com")
    g1("Okta_Group IT")
    gha("Okta_Application GitHub Enterprise Cloud")
    jmfa("Okta_Application Jamf Pro SAML")
    org -- Okta_Contains --> u1
    org -- Okta_Contains --> u2
    org -- Okta_Contains --> g1
    u1 -- Okta_MemberOf --> g1
    u2 -- Okta_AppAdmin --> gha
    g1 -- Okta_AppAssignment --> gha
    u1 -- Okta_AppAssignment --> jmfa
  end
  subgraph gh["GitHub Enterprise Cloud"]
    direction LR
    ghorg("GHOrganization Contoso")
    ghu1("GHUser john\@contoso.com")
    ghorg -- GHContains --> ghu1
  end
  subgraph jamf["Jamf Pro Cloud"]
    direction LR
    jamft("jamf_Tenant contoso.jamfcloud.com")
    jmfu1("jamf_Account john\@contoso.com")
  end
  adu1 -- Okta_UserSync --> u1
  adu2 -- Okta_UserSync --> u2
  adg1 -- Okta_MembershipSync --> g1
  gha -- Okta_OutboundSSO --> ghorg
  jmfa -- Okta_OutboundSSO --> jamft
  u1 -- Okta_OutboundSSO --> ghu1
  u1 -- Okta_OutboundSSO --> jmfu1
```

## GitHub Enterprise Cloud Organizations

When integrating Okta with GitHub Enterprise Cloud, each GitHub organization connected to Okta is represented as a separate `Okta_Application` node in BloodHound.

![Properties of the GitHub Application node](../Images/bloodhound-github-properties.png)

> [!WARNING]
> User mapping between `OktaHound` and `GitHound` is not implemented at this time.

## AWS Accounts

> [!WARNING]
> Support for AWS accounts and roles in Okta is not implemented at this time.

## Jamf Pro

When integrating Okta with Jamf Pro using SAML 2.0, each Jamf Pro instance connected to Okta is represented as a separate `Okta_Application` node in BloodHound.
The differentiator is the `domainFQDN` property:

![Jamf Pro SAML application in BloodHound](../Images/bloodhound-jamf-saml-properties.png)

It is also possible to integrate Jamf Pro with Okta using Secure Web Authentication (SWA), but this option is less secure.

![Jamf Pro SWA settings](../Images/app-jamf-swa.png)

## Google Workspace

Similarly to the Jamf Pro SAML applications, each Google Workspace (formerly G Suite) instance connected to Okta using SAML 2.0 is represented as a separate `Okta_Application` node in BloodHound and is identified by the `domainFQDN` property:

![Google Workspace SAML application in BloodHound](../Images/bloodhound-google-saml-properties.png)

The SAML 2.0 protocol should always be preferred to SWA when integrating Okta with Google Workspace:

![Google Workspace sign-in protocol settings](../Images/app-google-protocol-selector.png)

## Generic SAML 2.0 Applications

The assertion consumer service (ACS) URLs of generic (non-Catalog) Okta SAML 2.0 applications are exposed via the `url` attribute in BloodHound.

![Okta SAML application in BloodHound](../Images/bloodhound-app-saml.png)

## Generic Secure Web Authentication (SWA) Applications

Secure Web Authentication (SWA) is an Okta technology that provides Single Sign-On (SSO) functionality to external web applications that don't support federated protocols. SWA applications store user credentials in Okta and automatically fill them in when users access the application through the Okta dashboard.

The app's login page URL is exposed via the `url` attribute in BloodHound.

![Okta SWA application in BloodHound](../Images/bloodhound-app-swa.png)

> [!WARNING]
> TODO: Fetch a list of stored credentials for SWA applications, if the API allows it.

## Generic OpenID Connect (OIDC) Applications

Okta supports three types of OIDC applications:

- Web Application
- Single-Page Application (SPA)
- Native Application

The default redirect URI of generic (non-Catalog) Okta OIDC single-page applications (SPAs) starts with `http://localhost:8080/`, making it hard to identify the actual application address. The optional Okta-initiated sign-in flow URL is therefore exposed in the `url` attribute in BloodHound instead, if configured.

OIDC applications can be granted OAuth 2.0 scopes to access Okta APIs on behalf of users:

![Okta application OIDC grants](../Images/app-oidc-grants.png)

## SCIM-Enabled Applications

The `features` attribute of `Okta_Application` nodes may contain the following SCIM-related values,
indicating if SCIM is enabled and which protocol capabilities are supported:

| Freature                   | Description                                      |
|---------------------------|--------------------------------------------------|
| PUSH_NEW_USERS            | Supports pushing new users from Okta to the application |
| PUSH_PASSWORD_UPDATES     | Supports pushing password updates from Okta to the application |
| PUSH_PENDING_USERS        | Supports pushing pending users from Okta to the application |
| PUSH_PROFILE_UPDATES      | Supports pushing profile updates from Okta to the application |
| PUSH_USER_DEACTIVATION   | Supports pushing user deactivation from Okta to the application |
| REACTIVATE_USERS          | Supports reactivating users in the application from Okta |
| IMPORT_NEW_USERS          | Supports importing new users into the application from Okta |
| OPP_SCIM_INCREMENTAL_IMPORTS* | Supports incremental imports of users into the application from Okta |
| IMPORT_PROFILE_UPDATES    | Supports importing profile updates into Okta from the application |
| GROUP_PUSH                | Supports pushing groups and group memberships from Okta to the application |

