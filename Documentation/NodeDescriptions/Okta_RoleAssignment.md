# Okta_RoleAssignment

## Overview

To help visualize role assignments in BloodHound, `OktaHound` creates `Okta_RoleAssignment` nodes for each role assignment in Okta. These nodes represent the relationship between a [user](Okta_User.md), [group](Okta_Group.md), or [application](Okta_Application.md) and a role ([built-in](Okta_Role.md) or [custom](Okta_CustomRole.md)).

## Properties

| Name | Source | Type | Description |
| ---- | ------ | ---- | ----------- |
| `id` | `roleAssignment.id + "_" + assignee.id` | `string` | Unique role-assignment identifier derived from role assignment and assignee IDs. |
| `name` | `roleAssignment.label` | `string` | Role name associated with this assignment. |
| `displayName` | `roleAssignment.label` | `string` | Display label used in BloodHound. |
| `oktaDomain` | Collector context (non-API) | `string` | Okta organization domain where the role assignment exists. |
| `assignmentType` | `roleAssignment.assignmentType` | `string` | Assignment scope/type (for example user or group assignment). |
| `type` | `roleAssignment.type` | `string` | Assigned role identifier (for example `WORKFLOWS_ADMIN`, `APP_ADMIN`). |
| `status` | `roleAssignment.status` | `string` | Role assignment lifecycle status. |
| `created` | `roleAssignment.created` | `datetime` | Role assignment creation timestamp. |
| `lastUpdated` | `roleAssignment.lastUpdated` | `datetime` | Last role assignment update timestamp. |

## Sample Property Values

```yaml
id: irbwnwe8vjjXl4FbX697_00uw2sodowQc75SUm697
name: Workflows Administrator
displayName: Workflows Administrator
oktaDomain: contoso.okta.com
assignmentType: USER
type: WORKFLOWS_ADMIN
status: ACTIVE
created: 2025-10-22T13:29:26+00:00
lastUpdated: 2025-10-22T13:29:26+00:00
```
