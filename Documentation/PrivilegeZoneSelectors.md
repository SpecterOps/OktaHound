# Privilege Zone Selectors
The following Cypher selectors define the default Privilege Zone for the OktaHound extension.
Each selector is defined in a JSON file located in the [PrivilegeZoneSelectors](../Src/PrivilegeZoneSelectors) directory of the OktaHound repository.

## Okta: Organization

Organization nodes in Okta.

```cypher
MATCH (n:Okta_Organization)
RETURN n
```

This selector is defined in the [organization.json](../Src/PrivilegeZoneSelectors/organization.json) file.

## Okta: Tier Zero Devices

Devices associated with principals who have SUPER_ADMIN or ORG_ADMIN role assignments.

```cypher
MATCH (n:Okta_Device)-[:Okta_DeviceOf]->(:Okta)-[:Okta_HasRoleAssignment|Okta_MemberOf*1..2]->(r:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization)
WHERE r.type = "SUPER_ADMIN"
OR r.type = "ORG_ADMIN"
RETURN n
```

This selector is defined in the [tier0-devices.json](../Src/PrivilegeZoneSelectors/tier0-devices.json) file.

## Okta: Tier Zero Principals

Principals with SUPER_ADMIN or ORG_ADMIN role assignments.

```cypher
MATCH (n:Okta)-[:Okta_HasRoleAssignment|Okta_MemberOf*1..2]->(r:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization)
WHERE r.type = "SUPER_ADMIN"
OR r.type = "ORG_ADMIN"
RETURN n
```

This selector is defined in the [tier0-principals.json](../Src/PrivilegeZoneSelectors/tier0-principals.json) file.

