# Okta_RoleAssignment Node

## Overview

To help visualize role assignments in BloodHound, `OktaHound` creates `Okta_RoleAssignment` nodes for each role assignment in Okta. These nodes represent the relationship between a [user](Okta_User.md), [group](Okta_Group.md), or [application](Okta_Application.md) and a role ([built-in](Okta_Role.md) or [custom](Okta_CustomRole.md)).

## Okta_HasRoleAssignment and Okta_ScopedTo Edges

The `Okta_HasRoleAssignment` edges connect users, groups, and applications to their respective `Okta_RoleAssignment` nodes.
The `Okta_ScopedTo` edges connect the `Okta_RoleAssignment` nodes to the resources they are scoped to, such as the organization or specific groups or applications.

```mermaid
graph TB
    ra1("Okta_RoleAssignment Help Desk Administrator")
    ra2("Okta_RoleAssignment Super Administrator")
    r1("Okta_Role Help Desk Administrator")
    r2("Okta_Role Super Administrator")
    u1("Okta_User john\@contoso.com")
    u2("Okta_User steve\@contoso.com")
    u3("Okta_User alice\@contoso.com")
    g1("Okta_Group Seattle Help Desk")
    g2("Okta_Group Seattle Office")
    org("Okta_Organization contoso.okta.com")

    u1 -- Okta_MemberOf --> g1
    g1 -. Okta_HasRoleAssignment .-> ra1
    g1 -. Okta_HasRole .-> r1
    g1 -- Okta_HelpDeskAdmin --> u3
    u3 -- Okta_MemberOf --> g2
    ra1 -- Okta_ScopedTo --> g2
    u2 -. Okta_HasRoleAssignment .-> ra2
    ra2 -. Okta_ScopedTo .-> org
    u2 -- Okta_SuperAdmin --> org
    u2 -. Okta_HasRole .-> r2
```
