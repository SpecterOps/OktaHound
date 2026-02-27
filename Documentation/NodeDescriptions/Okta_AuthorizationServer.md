# Okta_AuthorizationServer

## Overview

Authorization servers in Okta are used to issue OAuth 2.0 access tokens for API access. They define the scopes, claims, and access policies that control how tokens are issued and what permissions they grant. Each Okta organization has a default authorization server, and administrators can create additional custom authorization servers for specific use cases.

In `OktaHound`, authorization servers are represented as `Okta_AuthorizationServer` nodes.

> [!WARNING]
> The relationships between authorization servers and applications are currently not evaluated by `OktaHound`.

## Properties

| Name | Source | Type | Description |
| ---- | ------ | ---- | ----------- |
| `id` | `server.id` | `string` | Unique authorization server identifier. |
| `name` | `server.name` | `string` | Authorization server name. |
| `displayName` | `server.name` | `string` | Display label used in BloodHound. |
| `oktaDomain` | Collector context (non-API) | `string` | Okta organization domain where the authorization server exists. |
| `description` | `server.description` | `string` | Human-readable server description. |
| `status` | `server.status` | `string` | Current lifecycle status. |
| `issuer` | `server.issuer` | `string` | Token issuer URL. |
| `issuerMode` | `server.issuerMode` | `string` | Issuer mode selected in Okta. |
| `audiences` | `server.audiences` | `string[]` | Allowed audience values for issued tokens. |
| `created` | `server.created` | `datetime` | Authorization server creation timestamp. |
| `lastUpdated` | `server.lastUpdated` | `datetime` | Last update timestamp for the server configuration. |

## Sample Property Values

```yaml
id: ausz6ipkn4u0hDzyf697
name: app creation
displayName: app creation
oktaDomain: contoso.okta.com
status: INACTIVE
issuer: https://contoso.okta.com/oauth2/ausz6ipkn4u0hDzyf697
issuerMode: DYNAMIC
audiences:
  - test
created: 2026-01-14T15:41:28+00:00
lastUpdated: 2026-01-14T16:09:30+00:00
```
