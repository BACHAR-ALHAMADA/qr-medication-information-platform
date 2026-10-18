# Technical architecture overview

Status: Existing architecture direction preserved. Phase 2A adds a read-only source-metadata API and mocked test projects in the working tree; validation status is in [backend documentation](phase2a-backend.md). No frontend, persistence model or deployment is implemented.

## Accepted decisions

| Decision | Existing authority | Implication |
|---|---|---|
| ADR-001 | [Modular monolith](decisions/ADR-001-modular-monolith.md) | Explicit modules in one initial deployment; no speculative microservices |
| ADR-002 | [PostgreSQL](decisions/ADR-002-postgresql.md) | Planned system of record with EF Core; schema and operations unresolved |
| ADR-003 | [Opaque QR references](decisions/ADR-003-qr-token-strategy.md) | Patient token possession alone never authorizes private information |

Use the existing [system overview](architecture/system-overview.md), [module boundaries](architecture/module-boundaries.md), and [scalability guidance](architecture/scalability.md) for details. Planned technology remains C#/ASP.NET Core Web API, React/TypeScript, REST over HTTPS, PostgreSQL/EF Core, and standards-based staff identity.

API handles transport through Application/module contracts. Application coordinates use cases, Domain owns business rules, and Infrastructure implements technical interfaces. Modules own data and expose explicit contracts. Shared BuildingBlocks remain small and genuinely shared.

The existing module placeholders express architectural possibilities, not approved MVP scope. In particular, PatientMedication and Verification placeholders do not authorize F-006 implementation. Multilingual, RTL and accessible mobile design are initial product concerns; storage and client implementation details remain unchosen.

## Phase 1B operating boundaries

MedicationCatalogue remains responsible for central public content, review states, provenance and revisions. Initial public reads and pharmacist catalogue finding do not require PMS connectivity or patient accounts (REQ-010). Protected editorial/tenant actions still require server-side authorization; central content is not a tenant-data bypass. Publication responsibilities and central-versus-tenant permissions need HUM-003/HUM-005/HUM-006.

The pharmacy's PMS remains its clinical system of record. PostgreSQL in ADR-002 is this platform's record store, not a replacement for pharmacy clinical records. A future approved adapter would implement a narrow Application contract in Infrastructure, validate external inputs and preserve module/tenant boundaries; do not create an integration framework in advance. [Integration strategy](integration-strategy.md) records identifier, vendor-access and standards research (F-007, REQ-011).

The [proposed demonstration](project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration) can use deterministic synthetic fixtures behind an Application-facing read interface if approved. That temporary demonstration approach is not a replacement for ADR-002 or approval of a production persistence design. No schema, new module or infrastructure is introduced now.

## Decisions still needed

HUM-004 covers QR/verification lifecycle; HUM-005 identity and permissions; HUM-006 tenant isolation and ownership; HUM-008 deployment, availability, capacity and recovery. Resolve isolation before persistence implementation. Do not add Redis, a broker, Kubernetes, or cloud-specific infrastructure without a concrete requirement.

No accepted ADR conflicts with the approved discovery principles. Existing patient-verification MVP wording is a scope ambiguity corrected in linked guidance; the underlying security decision is unchanged. Future conflicts must be flagged with a proposed resolution and owner decision rather than silently rewriting accepted ADRs. T-007 describes planned architecture verification.

## Phase 2A implementation boundary

Application owns the exact-DIN lookup contract and result records. Infrastructure owns the DPD adapter, configuration validation and HTTP normalization. The API endpoint invokes Application contracts; only the host composition root references Infrastructure for service registration. No Domain dependency is needed for this metadata-only slice and no persistence or business-rule layer is fabricated. [Backend details](phase2a-backend.md) describe the single endpoint, source/provenance contract and safe failure behavior. Tests use mocked HTTP and in-process hosting, not live API calls.
