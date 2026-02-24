## Overview

Custom roles can be created with specific [permissions](https://developer.okta.com/docs/api/openapi/okta-management/guides/permissions/)
and then assigned to [users](Okta_User.md), [groups](Okta_Group.md), and [applications](Okta_Application.md) over [resource sets](Okta_ResourceSet.md).
[Complex conditions](https://help.okta.com/oie/en-us/content/topics/security/custom-admin-role/permission-conditions.htm) can be used if the custom admin role has one of the following permissions:

- okta.users.read
- okta.users.manage
- okta.users.create

Custom roles are represented as `Okta_CustomRole` and `Okta_RoleAssignment` nodes in `OktaHound`, similar to built-in roles.

## Abusable Permissions of Custom Roles in Okta

The following Okta permissions are particularly interesting from an offensive security perspective,
as they can be abused to escalate privileges in hybrid scenarios:

- okta.users.manage
- okta.users.credentials.manage
- okta.users.credentials.resetFactors
- okta.users.credentials.resetPassword
- okta.users.credentials.expirePassword
- okta.users.credentials.manageTemporaryAccessCode
- okta.groups.manage
- okta.groups.members.manage
- okta.apps.manage

> [!WARNING]
> The research on abusable Okta permissions is still ongoing.
