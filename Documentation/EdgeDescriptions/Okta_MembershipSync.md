## General Information

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
