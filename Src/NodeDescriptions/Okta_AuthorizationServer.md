## Overview

Authorization servers in Okta are used to issue OAuth 2.0 access tokens for API access. They define the scopes, claims, and access policies that control how tokens are issued and what permissions they grant. Each Okta organization has a default authorization server, and administrators can create additional custom authorization servers for specific use cases.

In `OktaHound`, authorization servers are represented as `Okta_AuthorizationServer` nodes.

> [!WARNING]
> The relationships between authorization servers and applications are currently not evaluated by `OktaHound`.
