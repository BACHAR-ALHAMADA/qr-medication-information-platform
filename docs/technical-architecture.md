# Technical architecture overview

Status: Existing architecture direction preserved. No API, frontend, persistence model, deployment, or tests are implemented.

## Accepted decisions

| Decision | Existing authority | Implication |
|---|---|---|
| ADR-001 | [Modular monolith](decisions/ADR-001-modular-monolith.md) | Explicit modules in one initial deployment; no speculative microservices |
| ADR-002 | [PostgreSQL](decisions/ADR-002-postgresql.md) | Planned system of record with EF Core; schema and operations unresolved |
| ADR-003 | [Opaque QR references](decisions/ADR-003-qr-token-strategy.md) | Patient token possession alone never authorizes private information |

Use the existing [system overview](architecture/system-overview.md), [module boundaries](architecture/module-boundaries.md), and [scalability guidance](architecture/scalability.md) for details. Planned technology remains C#/ASP.NET Core Web API, React/TypeScript, REST over HTTPS, PostgreSQL/EF Core, and standards-based staff identity.

API handles transport through Application/module contracts. Application coordinates use cases, Domain owns business rules, and Infrastructure implements technical interfaces. Modules own data and expose explicit contracts. Shared BuildingBlocks remain small and genuinely shared.

The existing module placeholders express architectural possibilities, not approved MVP scope. In particular, PatientMedication and Verification placeholders do not authorize F-006 implementation. Multilingual, RTL and accessible mobile design are initial product concerns; storage and client implementation details remain unchosen.

## Decisions still needed

HUM-004 covers QR/verification lifecycle; HUM-005 identity and permissions; HUM-006 tenant isolation and ownership; HUM-008 deployment, availability, capacity and recovery. Resolve isolation before persistence implementation. Do not add Redis, a broker, Kubernetes, or cloud-specific infrastructure without a concrete requirement.

No accepted ADR conflicts with the approved discovery principles. Existing patient-verification MVP wording is a scope ambiguity corrected in linked guidance; the underlying security decision is unchanged. Future conflicts must be flagged with a proposed resolution and owner decision rather than silently rewriting accepted ADRs. T-007 describes planned architecture verification.
