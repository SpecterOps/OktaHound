# Okta_Group Node

## Overview

Groups in Okta are collections of users that can be used to manage access to applications and resources. Groups can be created manually or synchronized from external directories such as Active Directory.
The built-in **Everyone** group always contains all users in the Okta organization. Only users can be members of groups and groups cannot be nested.

In `OktaHound`, groups are represented as `Okta_Group` nodes.

## Synchronization with External Directories

Similarly to users, groups can also be synchronized from external directories. The Okta API exposes the original Active Directory attributes, which are then collected by `OktaHound`:

![Group synchronized from AD](../Screenshots/bloodhound-ad-synced-group.png)

Nested (transitive) group memberships in Active Directory are always flattened (resolved) when synchronized to Okta, as illustrated below:

```mermaid
graph TB
    subgraph ad["Active Directory"]
        ag1("Group A")
        ag2("Group B")
        u1("User 1")
        u2("User 2")
        u1 -- MemberOf --> ag1
        u2 -- MemberOf --> ag2
        ag2 -- MemberOf --> ag1
    end
    subgraph Okta
        og1("Okta_Group A")
        og2("Okta_Group B")
        u1o("Okta_User 1")
        u2o("Okta_User 2")
        u1o -- Okta_MemberOf --> og1
        u2o -- Okta_MemberOf --> og1
        u2o -- Okta_MemberOf --> og2
    end
    ad == Sync ==> Okta
```

## Okta_MemberOf Edges

The traversable `Okta_MemberOf` edges represent the membership relationships between users and groups in Okta:

```mermaid
graph LR
    u1("Okta_User john\@contoso.com")
    u2("Okta_User steve\@contoso.com")
    u3("Okta_User mary\@contoso.com")
    g1("Okta_Group Marketing")
    g2("Okta_Group Sales")
    u1 -- Okta_MemberOf --> g1
    u2 -- Okta_MemberOf --> g1
    u2 -- Okta_MemberOf --> g2
    u3 -- Okta_MemberOf --> g2
```

## Okta_GroupPush Edges

The non-traversable `Okta_GroupPush` edges represent the group push assignments to applications.
This indicates group provisioning and membership synchronization from Okta to external applications.

```mermaid
graph LR
    g1("Okta_Group Engineering")
    app1("Okta_Application contoso.com")
    g1 -- Okta_GroupPush --> app1
```

## Okta_GroupPull Edges

The traversable `Okta_GroupPull` edges represent the group synchronization relationships from applications to Okta:

```mermaid
graph LR
    g1("Okta_Group HR")
    app1("Okta_Application contoso.com")
    app1 -- Okta_GroupPull --> g1
```

## Okta_MembershipSync Edges

The traversable hybrid `Okta_MembershipSync` edges represent the synchronization relationships between groups in external directories and their corresponding groups in Okta:

```mermaid
graph TB
  subgraph ad["Active Directory"]
    adg1("Group IT")
    adg2("Group HR")
  end
  subgraph okta["Okta"]
    g1("Okta_Group IT")
    g2("Okta_Group HR")
    adg1 -- Okta_MembershipSync --> g1
    g2 -- Okta_MembershipSync --> adg2
  end
  subgraph snow["Snowflake"]
    snowg1("SNOWGroup IT")
    g1 -- Okta_MembershipSync --> snowg1
  end
```

