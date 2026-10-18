# Project status

Last updated: 2026-10-17

## Current phase

**Phase 1C — Health Canada medication data research**, within Phase 1 documentation/discovery. Read-only DPD feasibility probes used real public medication product data and no patient data. Standalone central reviewed content, no-account public QR access, future optional interoperability and proposed impact evaluation remain product direction. Backend implementation and patient-facing use of imported content are not approved; patient-specific functionality remains future scope. Existing accepted ADRs and security/privacy principles are preserved.

## Implemented

- Project foundation and documentation only.

## Not implemented

- API or frontend behavior.
- Authentication, authorization enforcement, patient verification, or QR generation/resolution.
- Database schema, EF Core model, migrations, or data.
- Dashboards, external integrations, deployment, or medical content.

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

See the [vision](product-vision.md), [requirements](product-requirements.md), [catalogue](feature-catalogue.md), [UX research plan](ui-ux-blueprint.md), and [roadmap](project-roadmap.md). All application capabilities remain unimplemented. T-001 through T-010 are planned coverage records, not executable or passed tests.

The initial market is Manitoba. Simple mobile-first UX, progressive disclosure, multilingual accessibility and RTL support are core product principles. Optional audio and simplified language remain proposals. Medical accuracy, translation review, paper reduction and regulatory applicability require evidence and qualified review.

Outstanding decisions above are tracked in [Human TODO](../tasks/HUMAN-TODO.md): HUM-001 scope; HUM-002 languages/accessibility; HUM-003 content; HUM-004 future verification/QR; HUM-005 identity/roles; HUM-006 tenant ownership/isolation; HUM-007 legal/privacy research; HUM-008 operations; HUM-009 research arrangements; HUM-010 business model. [AI TODO](../tasks/AI-TODO.md) records dependent follow-up.

The former verification-MVP wording has been clarified in authentication/minimization guidance; mandatory future private-access controls are unchanged. Older architecture descriptions describe intended eventual capabilities, not initial release approval. No accepted ADR conflict is currently identified; release scope and technical details remain unresolved.

Phase 1B added [integration strategy](integration-strategy.md) (HUM-011/AI-005) and [pilot/impact evaluation](pilot-and-impact-evaluation.md) (HUM-012/AI-006); it authorized no external API access or pilot. No sustainability/clinical benefit is demonstrated and no real-patient collection is approved. The proposed synthetic public catalogue-to-QR demonstration with LTR/RTL fixtures still awaits HUM-001/HUM-002/HUM-003 and separate implementation approval; Phase 1C research is a separate read-only activity.

Phase 1C explicitly authorizes read-only public DPD research; it does not authorize an implemented government/PMS connection. [Research findings and proposed backend plan](health-canada-dpd-research.md) record three successful product-data lookups, official-page monograph references, response-shape and data gaps, and outstanding rights/operations questions. AI-007 and planned T-011 track follow-up; T-001 through T-011 remain planned application coverage, with no executable tests. No API client, imported patient-facing content, schema, dependency installation, commit or push was created during this research.
