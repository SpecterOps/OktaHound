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

