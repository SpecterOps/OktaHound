# OktaHound Roadmap

## Supported Entity Types

Support for the following Okta entity types is currently implemented in `OktaHound`:

- [x] Okta Admin Management
  - [x] Organization
  - [x] People / Users
  - [x] Authentication Factors
  - [x] Groups
  - [x] Devices
  - [x] Applications
    - [x] Generic SAML 2.0 Apps
    - [x] Generic OIDC Apps
    - [x] Generic SWA Apps
      - [ ] Identifying credentials actually stored in Okta
    - [x] Service Apps
    - [x] SCIM Synchronization
      - [ ] Hybrid edges targeting the universal SCIM extension
    - [x] GitHub Enterprise Cloud
    - [ ] AWS
      - [x] AWS IAM Identity Center
        - [ ] Hybrid edges
      - [ ] AWS ClientVPN
      - [ ] AWS Console Password Sign-in
      - [ ] AWS Account Federation
    - [x] Entra ID / Azure / Microsoft 365
      - [ ] Hybrid edges
    - [x] Google Workspace
    - [x] Jamf
      - [x] Jamf Pro SAML
      - [ ] Jamf Pro SWA
    - [x] 1Password Business SWA
    - [x] Snowflake
    - [ ] Okta Org2Org (Missing license)
  - [x] API Tokens
  - [x] Built-In Roles
  - [x] Custom Roles
  - [x] Resource Sets
  - [x] Authorization Servers
  - [x] API Service Integrations
  - [x] Directory Integrations
    - [x] Active Directory
      - [x] Hybrid edges
        - [ ]
    - [ ] LDAP
  - [x] Identity Providers
  - [x] Policies
    - [ ] Entity risk policy
    - [ ] Session protection policy
    - [ ] Authentication policy
    - [ ] Global session policy
    - [ ] End user account management policy
  - [x] Agent Pools
  - [x] Agents
    - [x] Active Directory
    - [ ] ~~IWA~~ (Legacy)
    - [ ] LDAP
    - [ ] MFA
    - [ ] OPP
    - [ ] RUM
    - [ ] RADIUS
  - [ ] ~~Workflows~~ (Gaps in the Okta API)
- [ ] Okta Identity Governance
  - [ ] Realms
    - [x] Realm User Assignments
    - [ ] Realm Role Assignments
  - [ ] Group Ownership
- [ ] Okta Privileged Access
  - [ ] Service accounts
- [ ] Okta Access Gateway
- [ ] System Log Events

We decided not to collect the following Okta entity types, as they are not directly relevant to attack path modeling:

- Authenticators
- User Profile Policies
- Networks
- Behavior Detection
- Trusted Origins (CORS, iFrames)
- HealthInsight
- CAPTCHA Integrations
- Customizations (SMTP servers, branding)

> [!NOTE]
> The entities listed above should not be skipped during Okta security assessments,
> as they might still contain misconfigurations that could be exploited by attackers.

## Least Privileged Access

To read application OAuth 2.0 grants, the [Super administrators](https://help.okta.com/en-us/content/topics/security/administrators-super-admin.htm)
role must be assigned to the `OktaHound` application. We are in touch with Okta to find a better solution.

The following steps should be applicable in the future to grant least privileged access to the `OktaHound` application:

In addition to the OAuth 2.0 scopes listed above, the `OktaHound` application must be assigned the [Read-only administrators](https://help.okta.com/en-us/content/topics/security/administrators-read-only-admin.htm) role. As this built-in role does not allow reading role assignments, a custom role needs to be created with the appropriate permissions.

In accordance with the principle of least privilege, our recommendation is to create a custom role called **IAM Readers** with the **View roles, resources, and admin assignments** permission and a [resource set](https://help.okta.com/oie/en-us/content/topics/security/custom-admin-role/create-resource-set.htm) called **IAM Resources** containing **All Identity and Access Management resources**. The IAM Readers role should then be assigned to the `OktaHound` application and scoped to the IAM Resources resource set.

## API Service Integration

Our long-term goal is to [register](https://oinmanager.okta.com/) BloodHound Enterprise as an API application in [Okta's OIN Catalog](https://www.okta.com/integrations/),
to streamline the app registration process.
