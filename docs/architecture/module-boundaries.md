# Module boundaries

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

API → Application contracts/use cases → Domain. Infrastructure implements application/domain-facing interfaces. Module-to-module calls use explicit contracts, avoiding direct dependencies on another module's storage. Shared abstractions should not obscure ownership.

## Open questions

- Decision Required: exact ownership of patient identity and organization records.
- Decision Required: whether Audit is a module or a cross-cutting application service after workflows are defined.
