# Okta_Agent

## Overview

The `Okta_Agent` node represents an Okta Agent, which is a component used in Okta's integration with on-premises systems.
Okta Agents facilitate communication between the Okta cloud and on-premises applications or directories, enabling features such as single sign-on (SSO) and user provisioning.

One or more agents are grouped into Agent Pools, represented by the [Okta_AgentPool](Okta_AgentPool.md) nodes, to provide redundancy and load balancing.

![Active Directory Agent in BloodHound](../Images/bloodhound-ad-agent.png)

> [!WARNING]
> Edges between the `Okta_Agent` and AD `Computer` nodes are not created in the current version of `OktaHound`.
> This functionality is planned for a future release.
