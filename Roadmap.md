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
      - [x] User synchronization
        - [x] Push
        - [ ] Pull
      - [x] Group synchronization
        - [x] Push
        - [x] Pull
      - [x] Password synchronization
      - [x] Single Sign-On relationships
        - [x] OIDC
        - [x] SAML
    - [x] Google Workspace
      - [ ] Hybrid edges
    - [x] Jamf
      - [x] Jamf Pro SAML
      - [ ] Jamf Pro SWA
    - [x] 1Password Business SWA
    - [x] Snowflake
    - [x] Okta Org2Org
      - [x] User synchronization
        - [x] Push
        - [ ] Pull
      - [x] Group synchronization
        - [x] Push
        - [x] Pull
      - [x] Password synchronization
      - [x] Single Sign-On relationships
        - [x] OIDC
        - [x] SAML
  - [x] API Tokens
  - [x] Built-In Roles
  - [x] Custom Roles
  - [x] Resource Sets
  - [x] Authorization Servers
  - [x] API Service Integrations
  - [x] Directory Integrations
    - [x] Active Directory
      - [x] User synchronization
        - [x] Push
        - [x] Pull
      - [x] Group synchronization
        - [x] Push
        - [x] Pull
      - [x] Agentless SSO account
      - [x] Agent location
      - [ ] Password synchronization
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
