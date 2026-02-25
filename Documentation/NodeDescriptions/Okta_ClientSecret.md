# Okta_ClientSecret

## Overview

Client secrets are used by API service integrations and OIDC applications to authenticate with Okta and obtain access tokens.

![Okta client secret creation](../Images/app-client-secret-creation.png)

An application can have up to two client secrets configured, to allow for secret rotation.

![Okta client secret rotation](../Images/app-client-secret-rotation.png)

Client secrets are represented as `Okta_ClientSecret` nodes in BloodHound.
