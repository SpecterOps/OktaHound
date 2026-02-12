# OktaHound - Work in Progress

**Okta Data Collector for BloodHound OpenGraph**

[![Applies to BloodHound Community Edition](https://mintlify.s3.us-west-1.amazonaws.com/specterops/assets/community-edition-pill-tag.svg)](#)

[![Apache License 2.0](https://img.shields.io/badge/license-Apache%20License%202.0-green)](LICENSE)
[![.NET 8.0+](https://img.shields.io/badge/Runtime-8.0%2B-007FFF.svg?logo=.net)](#)
[![Windows, macOS, and Linux Support](https://img.shields.io/badge/OS-Windows%20%7C%20macOS%20%7C%20Linux-blue)](#)
[![CI Build](https://github.com/SpecterOps/OktaHound/actions/workflows/autobuild.yml/badge.svg)](https://github.com/SpecterOps/OktaHound/actions/workflows/autobuild.yml)

## Overview

`OktaHound` is a data collector for [Okta Platform](https://www.okta.com/products/workforce-identity/) (AKA Okta Workforce Identity Cloud) environments, that integrates with [BloodHound OpenGraph](https://specterops.io/opengraph/) to help security professionals visualize and analyze their Okta configurations.
It collects data about users, groups, applications, roles,
and other entities within an Okta organization and represents them as [nodes](Documentation/Schema.md#nodes)
and [edges](Documentation/Schema.md#edges) in BloodHound's graph database.

The other main product in Okta's portfolio is [Auth0](https://auth0.com/), previously known as Customer Identity Cloud. Auth0 is **not supported** by `OktaHound` at this time.

## Authors

### Michael Grafnetter - [SpecterOps](https://specterops.io)

[![Twitter](https://img.shields.io/twitter/follow/MGrafnetter.svg?label=Twitter%20@MGrafnetter&style=social)](https://x.com/MGrafnetter)
[![Blog](https://img.shields.io/badge/Blog-www.dsinternals.com-2A6496.svg)](https://www.dsinternals.com/en)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-grafnetter-0077B5.svg)](https://www.linkedin.com/in/grafnetter)

### Lance Cain - [SpecterOps](https://specterops.io)

[![LinkedIn](https://img.shields.io/badge/LinkedIn-lance--cain--3ab262184-0077B5.svg)](https://www.linkedin.com/in/lance-cain-3ab262184/)

### Valdemar Carøe - [SpecterOps](https://specterops.io)

[![LinkedIn](https://img.shields.io/badge/LinkedIn-valdemar--carøe-0077B5.svg)](https://www.linkedin.com/in/valdemar-carøe)

## Okta Free Trial

Okta provides a [free trial](https://developer.okta.com/signup/) plan that can be used to test the majority of `OktaHound` features.

## References

We recommend reading the following blog posts to learn more about the potential Okta attack vectors:

- [Adam Chester (SpecterOps): Identity Providers for RedTeamers](https://blog.xpnsec.com/identity-providers-redteamers/)
- [Eli Guy (XM Cyber): Attack Techniques in Okta - Part 1 - A (Really) Deep Dive into Okta Key Terms](https://xmcyber.com/blog/attack-techniques-in-okta/)
- [Eli Guy (XM Cyber): Attack Techniques in Okta - Part 2 - Okta RBAC Attacks](https://xmcyber.com/blog/okta-rbac-attacks/)
- [Eli Guy (XM Cyber): Attack Techniques in Okta - Part 3 - From Okta to AWS Environments](https://xmcyber.com/blog/okta-attack-techniques-part-3-from-okta-environments-to-aws/)
- [AppOmni: Okta PassBleed Risks – A Technical Overview](https://appomni.com/ao-labs/okta-passbleed-risks/)
- [Luke Jennings (PushSecurity): Abusing Okta's SWA authentication](https://pushsecurity.com/blog/okta-swa/)
- [David French (Elastic): Testing your Okta visibility and detection with Dorothy and Elastic Security](https://www.elastic.co/security-labs/testing-okta-visibility-and-detection-dorothy)

## Research Tools

Here are some interesting GitHub repositories related to Okta security research:

- [Okta Post-Exploitation Toolkit](https://github.com/xpn/OktaPostExToolkit)
- [Okta Terrify](https://github.com/CCob/okta-terrify)
- [Dorothy](https://github.com/elastic/dorothy)
- [SaaS Attacks](https://github.com/pushsecurity/saas-attacks/)
- [Okta SCIM Attack Tool](https://github.com/authomize/okta_scim_attack_tool)
