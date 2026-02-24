## Overview

Okta provides a handful of [built-in administrative roles](https://help.okta.com/en-us/content/topics/security/administrators-admin-comparison.htm) that can be assigned to users, groups, and applications to delegate administrative tasks. These roles have predefined permissions and cannot be modified.

The following roles are organization-wide:

- Super Administrator
- Organization Administrator
- API Access Management Administrator
- Mobile Administrator
- Workflows Administrator
- Report Administrator
- Read-only Administrator

The most powerful role is the **Super Administrator**, which has full access to all features and settings in the Okta organization.

The following roles can either be scoped to specific resources or assigned organization-wide:

- Group Administrator (AKA User Administrator)
- Group Membership Administrator
- Help Desk Administrator
- Application Administrator

> [!NOTE]
> Although the Workflows Administrator role is a built-in role, the Okta API treats it as a custom role that is scoped to the built-in `Workflows Resource Set`.

In `OktaHound`, built-in roles are represented as `Okta_Role` nodes.

## Built-In Role Identifiers

When working with roles using the Okta API, the built-in roles are referenced by the following identifiers:

| Role Identifier             | Role Name                           |
|-----------------------------|-------------------------------------|
| SUPER_ADMIN                 | Super Administrator                 |
| ORG_ADMIN                   | Organization Administrator          |
| USER_ADMIN                  | Group Administrator                 |
| GROUP_MEMBERSHIP_ADMIN      | Group Membership Administrator      |
| APP_ADMIN                   | Application Administrator           |
| API_ACCESS_MANAGEMENT_ADMIN | API Access Management Administrator |
| ~~API_ADMIN~~               | API Administrator (Deprecated?)     |
| HELP_DESK_ADMIN             | Help Desk Administrator             |
| MOBILE_ADMIN                | Mobile Administrator                |
| WORKFLOWS_ADMIN             | Workflows Administrator             |
| REPORT_ADMIN                | Report Administrator                |
| READ_ONLY_ADMIN             | Read-Only Administrator             |

To make the role identifiers unique, the `OktaHound` collector adds the organization domain name as a suffix to each role's ID, e.g., `SUPER_ADMIN@contoso.okta.com`.

## Built-In Role Permissions

Unlike custom roles, built-in roles have fixed permissions that cannot be changed.
However, the exact OAuth 2.0 scopes granted to each built-in role are not publicly documented by Okta and cannot even be retrieved via the API.

> [!WARNING]
> Research on the exact permissions of built-in roles in Okta is still ongoing.
