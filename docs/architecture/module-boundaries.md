# Module boundaries

Phase 3A: [fictional public fixtures and lookup](../phase3a-public-qr-demonstration.md) belong to MedicationCatalogue Application. API handlers expose only explicit public demonstration DTO fields; React's `/demo` surface consumes only `/api/demo` and generates QR matrices locally. No cross-module storage access or dependency on Identity, Organizations, tenant context or PostgreSQL is introduced. Existing DIN transport/lookup behavior remains separate.

The modules below are planned bounded areas in one deployable application. A module owns its business rules and persistence behavior; other modules use explicit application contracts rather than reaching into its tables or internals.

| Module | Responsibility | Key boundary |
|---|---|---|
| MedicationCatalogue | Public product information, provenance, revisions, official document references | Public read models must not include patient-specific fields |
| PatientMedication | Patient medication assignments and private instructions | Access is tenant-scoped and permission-checked |
| QrAccess | Opaque token lifecycle and target resolution | Token resolution alone does not disclose private data |
| Identity | Staff identity provider integration and application identity mapping | Delegate credentials and protocol handling to established standards/providers |
| Organizations | Pharmacies, manufacturers, memberships, tenant context | Membership and tenant scope are validated server-side |
| Verification | Short-lived patient verification challenges and outcomes | Successful verification grants narrow, time-limited access |
| Audit | Security-relevant read, verification, and change events | Record sufficient context without sensitive payloads or bearer secrets |

API is the transport boundary. BuildingBlocks should contain only small, stable primitives that are genuinely shared. Domain rules belong to their owning module. Infrastructure implements technical concerns and persistence behind module-owned abstractions.

## Dependency direction

Phase 2E-2C implements two Identity-owned issuer/mapping tables and an exact resolver querying only Identity data. Provider organization/domain claims do not replace Organizations membership or grants. [ADR-007](../decisions/ADR-007-hybrid-authentication-identity-mapping.md) preserves accepted boundaries and the single ADR-005 FK exception. External-to-internal bootstrap cannot reuse the existing actor-self reader: the [implementation](../phase2e-2c-identity-mapping-foundation.md) accepts only privileged disposable synthetic sessions, grants existing runtime roles no new access and has no API registration. Least-privileged runtime lookup remains separately gated.

Phase 2E-2A keeps external identity resolution and actor construction in Identity Application using framework-independent BCL contracts, with no provider, database or HTTP dependency. The trusted validation source and mapping resolver are interfaces only; synthetic implementations live in tests. No change to Organizations rules, PostgreSQL readers or API composition. See [boundary contract and future adapter gates](../phase2e-2a-trusted-actor-boundary.md).

API → Application contracts/use cases → Domain. Infrastructure implements application/domain-facing interfaces. Module-to-module calls use explicit contracts, avoiding direct dependencies on another module's storage. Shared abstractions should not obscure ownership.

## Open questions

Local implementation checkpoint, 2026-11-11: [Phase 2E-1](../phase2e-1-postgresql-readers.md) adds an Identity-owned staff reader and Organizations-owned access reader behind the existing Application interfaces. Only Infrastructure depends on Npgsql and the shared PostgreSQL session; Application/Domain remain unchanged. Both readers receive the same caller-owned read-only transaction, and the trusted synthetic test harness composes them with `StaffAuthorization`. Identity reads only its user table; Organizations reads only its organization/membership/grant tables. The ADR-005 physical FK remains a narrow migration-only exception. There is no API composition, production actor binding or protected operation.

- Decision Required: exact ownership of patient identity and organization records.
- Decision Required: whether Audit is a module or a cross-cutting application service after workflows are defined.
