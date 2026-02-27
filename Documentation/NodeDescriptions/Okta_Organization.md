# Okta_Organization

## Overview

The Organization entity represents the Okta tenant itself. It contains general information about the organization, such as its name, domain, and settings.

In `OktaHound`, the organization is represented as a single `Okta_Organization` node.

## Properties

| Name | Source | Type | Description |
| ---- | ------ | ---- | ----------- |
| `id` | `settings.id` | `string` | Unique organization identifier. |
| `name` | `oktaDomain` | `string` | Okta organization domain name. |
| `displayName` | `settings.companyName` | `string` | Organization/company display name. |
| `oktaDomain` | Collector context (non-API) | `string` | Okta organization domain name. |
| `subdomain` | `settings.subdomain` | `string` | Okta subdomain value. |
| `status` | `settings.status` | `string` | Organization lifecycle status. |
| `created` | `settings.created` | `datetime` | Organization creation timestamp. |
| `lastUpdated` | `settings.lastUpdated` | `datetime` | Last organization metadata update timestamp. |

## Sample Property Values

```yaml
id: 00ow0o8if0CNwsKmk697
name: contoso.okta.com
displayName: Contoso
oktaDomain: contoso.okta.com
subdomain: contoso
status: ACTIVE
created: 2025-10-02T09:21:31+00:00
lastUpdated: 2025-12-09T23:04:15+00:00
```
