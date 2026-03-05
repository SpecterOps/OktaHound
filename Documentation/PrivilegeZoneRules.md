# Privilege Zone Rules
The following Cypher rules define the default Privilege Zone for the OktaHound extension.
Each rule is defined in a JSON file located in the [PrivilegeZoneRules](../Src/PrivilegeZoneRules) directory of the OktaHound repository.

## Organization

Organization nodes in Okta.

```cypher
MATCH (n:Okta_Organization)
RETURN n
```

This rule is defined in the [organization.json](../Src/PrivilegeZoneRules/organization.json) file.

## Tier Zero Devices

Devices associated with principals who have SUPER_ADMIN or ORG_ADMIN role assignments.

```cypher
MATCH (n:Okta_Device)-[:Okta_DeviceOf]->(:Okta)-[:Okta_HasRoleAssignment|Okta_MemberOf*1..2]->(r:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization)
WHERE r.type = "SUPER_ADMIN"
OR r.type = "ORG_ADMIN"
RETURN n
```

This rule is defined in the [tier0-devices.json](../Src/PrivilegeZoneRules/tier0-devices.json) file.

## Tier Zero Principals

Principals with SUPER_ADMIN or ORG_ADMIN role assignments.

```cypher
MATCH (n:Okta)-[:Okta_HasRoleAssignment|Okta_MemberOf*1..2]->(r:Okta_RoleAssignment)-[:Okta_ScopedTo]->(:Okta_Organization)
WHERE r.type = "SUPER_ADMIN"
OR r.type = "ORG_ADMIN"
RETURN n
```

This rule is defined in the [tier0-principals.json](../Src/PrivilegeZoneRules/tier0-principals.json) file.

