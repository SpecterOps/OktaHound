## General Information

The non-traversable hybrid `Okta_UserSync` edges represent bidirectional user synchronization relationships between Okta and external directories or applications. These edges indicate that user accounts are linked and synchronized between systems.

```mermaid
graph LR
    subgraph ad["Active Directory"]
        adu1("User john\@contoso.com")
    end
    subgraph okta["Okta"]
        u1("Okta_User john\@contoso.com")
        adu1 -. Okta_UserSync .-> u1
    end
    subgraph snowflake["Snowflake"]
        snu1("SNOWUser john\@contoso.com")
        u1 -. Okta_UserSync .-> snu1
    end
```
