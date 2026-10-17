# Project status

Last updated: 2026-10-17

## Current phase

**Phase 1 — foundation, product discovery and documentation.** Current work covers brainstorming, requirements analysis, and UX/UI planning. Discovery documentation now records the product vision, candidate features, research approach, content governance, and planned validation. MVP scope is not approved; patient-specific functionality remains future scope. Existing accepted ADRs and security/privacy principles are preserved.

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

See the [vision](product-vision.md), [requirements](product-requirements.md), [catalogue](feature-catalogue.md), [UX research plan](ui-ux-blueprint.md), and [roadmap](project-roadmap.md). All application capabilities remain unimplemented. T-001 through T-008 are planned coverage records, not executable or passed tests.

The initial market is Manitoba. Simple mobile-first UX, progressive disclosure, multilingual accessibility and RTL support are core product principles. Optional audio and simplified language remain proposals. Medical accuracy, translation review, paper reduction and regulatory applicability require evidence and qualified review.

Outstanding decisions above are tracked in [Human TODO](../tasks/HUMAN-TODO.md): HUM-001 scope; HUM-002 languages/accessibility; HUM-003 content; HUM-004 future verification/QR; HUM-005 identity/roles; HUM-006 tenant ownership/isolation; HUM-007 legal/privacy research; HUM-008 operations; HUM-009 research arrangements; HUM-010 business model. [AI TODO](../tasks/AI-TODO.md) records dependent follow-up.

The former verification-MVP wording has been clarified in authentication/minimization guidance; mandatory future private-access controls are unchanged. Older architecture descriptions describe intended eventual capabilities, not initial release approval. No accepted ADR conflict is currently identified; release scope and technical details remain unresolved.
