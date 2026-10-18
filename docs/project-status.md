# Project status

Last updated: 2026-10-17

## Current phase

**Phase 2A — first exact-DIN DPD backend slice.** The Product Owner approved a read-only source-metadata endpoint and mocked automated tests. Implementation is in the working tree for review; the build passed with zero warnings/errors and all 53 mocked automated tests passed. Patient-facing clinical publication and patient-specific functionality remain future scope. Existing accepted ADRs and security/privacy principles are preserved. Phase 1C documentation was committed/pushed separately as `c31ac78`; implementation is uncommitted.

## Implemented

- Project foundation and documentation.
- Application lookup contracts, Infrastructure DPD client and one API endpoint in the working tree; verification results are described in [backend documentation](phase2a-backend.md).
- Unit and in-process API integration test projects with mocked HTTP: 45 unit and 8 API integration tests passed, with none skipped.
- Live local-endpoint verification on 2026-10-17: all three researched DINs returned expected source metadata with HTTP 200; unknown DIN 00000000 returned safe HTTP 404. No code changes were needed; see [live verification evidence](phase2a-backend.md#live-verification-through-the-local-api).

## Not implemented

- Frontend behavior, reviewed clinical catalogue publication and QR workflows.
- Authentication, authorization enforcement, patient verification, or QR generation/resolution.
- Database schema, EF Core model, migrations, or data.
- Dashboards, PMS/FHIR integrations, deployment or patient-facing medical content.

## Decisions required / TODO

- Patient verification factor and delivery channel.
- Staff identity provider, MFA, account lifecycle, and role/permission matrix.
- Patient matching data and minimization approach.
- Tenant isolation details, including whether PostgreSQL row-level security will supplement application scoping.
- Authoritative public content sources, provenance and editorial/revision workflow.
- Deployment/hosting, availability, scale, backup and recovery objectives.
- Retention, deletion, privacy workflows, and qualified legal/regulatory research.
- QR token lifecycle and printed-label reassignment/revocation procedures.
- Threat-model workshop and security/privacy review before pilot.

No legal or regulatory conclusion is made by this project status document.

## Discovery navigation and decision ownership

See the [vision](product-vision.md), [requirements](product-requirements.md), [catalogue](feature-catalogue.md), [UX research plan](ui-ux-blueprint.md), and [roadmap](project-roadmap.md). Phase 2A implements only source lookup, not the full feature catalogue. T-011 now has 53 passing mocked tests; other coverage IDs remain planned unless covered explicitly by this narrow metadata slice.

The initial market is Manitoba. Simple mobile-first UX, progressive disclosure, multilingual accessibility and RTL support are core product principles. Optional audio and simplified language remain proposals. Medical accuracy, translation review, paper reduction and regulatory applicability require evidence and qualified review.

Outstanding decisions above are tracked in [Human TODO](../tasks/HUMAN-TODO.md): HUM-001 scope; HUM-002 languages/accessibility; HUM-003 content; HUM-004 future verification/QR; HUM-005 identity/roles; HUM-006 tenant ownership/isolation; HUM-007 legal/privacy research; HUM-008 operations; HUM-009 research arrangements; HUM-010 business model. [AI TODO](../tasks/AI-TODO.md) records dependent follow-up.

The former verification-MVP wording has been clarified in authentication/minimization guidance; mandatory future private-access controls are unchanged. Older architecture descriptions describe intended eventual capabilities, not initial release approval. No accepted ADR conflict is currently identified; release scope and technical details remain unresolved.

Phase 1B added [integration strategy](integration-strategy.md) (HUM-011/AI-005) and [pilot/impact evaluation](pilot-and-impact-evaluation.md) (HUM-012/AI-006); it authorized no external API access or pilot. No sustainability/clinical benefit is demonstrated and no real-patient collection is approved. The proposed synthetic public catalogue-to-QR demonstration with LTR/RTL fixtures still awaits HUM-001/HUM-002/HUM-003 and separate implementation approval; Phase 1C research is a separate read-only activity.

Phase 1C explicitly authorizes read-only public DPD research; it does not authorize an implemented government/PMS connection. [Research findings and proposed backend plan](health-canada-dpd-research.md) record three successful product-data lookups, official-page monograph references, response-shape and data gaps, and outstanding rights/operations questions. AI-007 and planned T-011 track follow-up; T-001 through T-011 remain planned application coverage, with no executable tests. No API client, imported patient-facing content, schema, dependency installation, commit or push was created during this research.

The paragraph above records the completed Phase 1C research scope. The later Phase 2A instruction explicitly authorizes the minimal live DPD metadata backend and its tests, as detailed in [Phase 2A backend documentation](phase2a-backend.md). It does not approve publication, patient data, persistence or the full public QR prototype. No implementation commit/push is authorized before Product Owner review.
