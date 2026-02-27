# Okta_CustomRole

## Overview

Custom roles can be created with specific [permissions](https://developer.okta.com/docs/api/openapi/okta-management/guides/permissions/)
and then assigned to [users](Okta_User.md), [groups](Okta_Group.md), and [applications](Okta_Application.md) over [resource sets](Okta_ResourceSet.md).
[Complex conditions](https://help.okta.com/oie/en-us/content/topics/security/custom-admin-role/permission-conditions.htm) can be used if the custom admin role has one of the following permissions:

- okta.users.read
- okta.users.manage
- okta.users.create

Custom roles are represented as `Okta_CustomRole` and `Okta_RoleAssignment` nodes in `OktaHound`, similar to built-in roles.

## Properties

| Name | Source | Type | Description |
| ---- | ------ | ---- | ----------- |
| `id` | `role.id` | `string` | Unique custom role identifier. |
| `name` | `role.label` | `string` | Name of the custom role. |
| `displayName` | `role.label` | `string` | Display label used in BloodHound. |
| `oktaDomain` | Collector context (non-API) | `string` | Okta organization domain where the custom role exists. |
| `permissions` | `role.permissions` | `string[]` | Effective permission labels associated with the custom role. |
| `created` | `role.created` | `datetime` | Custom role creation timestamp. |
| `lastUpdated` | `role.lastUpdated` | `datetime` | Last update timestamp of the role definition. |

## Sample Property Values

```yaml
id: cr0wwdjuk0w96MpFr697
name: IAM Readers
displayName: IAM Readers
oktaDomain: contoso.okta.com
created: 2025-10-29T12:45:55+00:00
lastUpdated: 2025-10-30T13:35:36+00:00
permissions:
  - okta.iam.read
```

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
