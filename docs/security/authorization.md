# Authorization

Authorization is server-side and applies to every operation that reads or changes protected data. Hiding UI elements is not a security boundary.

## Planned model

Use broad roles for staff categories where useful, combined with permissions and verified organization membership for concrete decisions. Roles may include pharmacist, doctor, pharmacy staff, and platform administrator, but exact capabilities remain **Decision Required**. Apply least privilege and separate administrative duties where warranted.

For tenant-owned records, derive authorized tenant context from the authenticated user's membership. Do not accept a client-supplied tenant identifier as proof of access. Patient verification grants narrowly scoped, short-lived access to one intended assignment; it does not create general patient or staff privileges.

## Enforcement and audit

Check authorization in application use cases and ensure persistence queries are tenant-scoped. Record sensitive reads and changes with actor/access context, tenant, target, action, and time, without recording sensitive payloads or bearer tokens.

## TODO

- Define role/permission matrix and emergency-access policy.
- Decide whether database row-level security supplements application-level tenant scoping.
