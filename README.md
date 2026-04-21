# OktaHound

[![Applies to BloodHound Community Edition](https://mintlify.s3.us-west-1.amazonaws.com/specterops/assets/community-edition-pill-tag.svg)](https://specterops.io/bloodhound-community-edition/)

[![Apache License 2.0](https://img.shields.io/badge/License-Apache%20License%202.0-green)](LICENSE)
[![.NET 10.0+](https://img.shields.io/badge/Runtime-10.0%2B-007FFF.svg?logo=.net)](https://dotnet.microsoft.com/en-us/)
[![Windows, macOS, and Linux Support](https://img.shields.io/badge/OS-Windows%20%7C%20macOS%20%7C%20Linux-blue)](https://github.com/SpecterOps/OktaHound/releases)
[![CI Build](https://github.com/SpecterOps/OktaHound/actions/workflows/autobuild.yml/badge.svg)](https://github.com/SpecterOps/OktaHound/actions/workflows/autobuild.yml)

## Overview

OktaHound is a BloodHound OpenGraph collector for Okta. It is an alternative to the SpecterOps-supported [OpenHound Okta collector](https://bloodhound.specterops.io/openhound/collectors/okta/overview).

OktaHound works with the [BloodHound Okta Extension](https://bloodhound.specterops.io/opengraph/extensions/okta/overview) and produces OpenGraph JSON output that can be uploaded to BloodHound.

## Authentication Options

OktaHound supports two ways to authenticate to the Okta API:

- `OAuth 2.0 service application` using a client ID and private key. This is the recommended option.
- `SSWS API token` using a token tied to an Okta administrator account.

For service application setup, follow the [OpenHound Okta app registration guide](https://bloodhound.specterops.io/openhound/collectors/okta/okta-app-registration). The same registration process applies to OktaHound.

## Configure OktaHound

Download the latest release for your platform from the [releases page](https://github.com/SpecterOps/OktaHound/releases), or build from source with `dotnet build`.

Create an `okta.yaml` file in the same directory as the `OktaHound` executable.

For OAuth 2.0 private key authentication, start from `okta.sample.oauth.yaml`:

```yaml
okta:
  client:
    oktaDomain: "https://TODO.okta.com"
    authorizationMode: "PrivateKey"
    clientId: "TODO"
    privateKey:
      "d": "TODO"
      "p": "TODO"
      "q": "TODO"
      "dp": "TODO"
      "dq": "TODO"
      "qi": "TODO"
      "kty": "RSA"
      "e": "AQAB"
      "kid": "TODO"
      "n": "TODO"
```

For SSWS token authentication, start from `okta.sample.token.yaml`:

```yaml
okta:
  client:
    oktaDomain: "https://TODO.okta.com"
    authorizationMode: "SSWS"
    token: "TODO"
```

## Run the Collector

Run OktaHound with the `collect` command:

```shell
OktaHound collect --output ./output --verbosity Trace
```

Useful options:

- `--skip-mfa` skips collecting user authentication factors.
- `--zip` compresses each exported JSON file after it is written.
- `--export-ad-nodes` writes the optional Active Directory subgraph output.
- `--domain` and `--token` can be used to override `okta.yaml` when using SSWS authentication.

By default, the collector writes output files to `./output`:

- `okta-graph.json`
- `okta-graph-ad.json` when `--export-ad-nodes` is used and AD nodes exist
- `okta-graph-hybrid.json` when hybrid edges exist

## Use the Collected Data

To use the collected Okta data in BloodHound, follow the [Okta Extension getting started guide](https://bloodhound.specterops.io/opengraph/extensions/okta/getting-started) to install the extension and import the Okta Cypher queries and Privilege Zone rules.

After that, upload the generated JSON files to BloodHound.

## Node and Edge Documentation

See [Okta Extension - Schema](https://bloodhound.specterops.io/opengraph/extensions/okta/schema).
