# Project status

Last updated: 2026-10-22

CI follow-up (2026-10-22): [GitHub Actions configuration](continuous-integration.md) is implemented locally and validated for review; T-014 distinguishes local Release/backend/frontend checks from the first hosted run, which is still pending. No CI-task commit/push, deployment, repository protection change or new application feature is authorized yet.

## Current phase

**Phase 2B — first React medication lookup interface.** Phase 2A backend is committed/pushed as `a4d2e29` with 53 passing mocked tests and successful live verification. The Product Owner approved a narrow English-only React/TypeScript exact-DIN interface, safe local proxy and necessary frontend tools/tests. Phase 2B was committed/pushed as `9ac2766` after explicit approval. The current working implementation is the bounded Phase 2C synthetic foundation described below; it remains uncommitted for review. Clinical publication, private patient data and the broader MVP remain future scope. Accepted ADRs and mandatory privacy/security principles are unchanged.

## Implemented

- Project foundation and documentation.
- Committed Application lookup contracts, Infrastructure DPD client and one API endpoint; verification results are described in [backend documentation](phase2a-backend.md).
- Unit and in-process API integration test projects with mocked HTTP: 45 unit and 8 API integration tests passed, with none skipped.
- Live local-endpoint verification on 2026-10-17: all three researched DINs returned expected source metadata with HTTP 200; unknown DIN 00000000 returned safe HTTP 404. No code changes were needed; see [live verification evidence](phase2a-backend.md#live-verification-through-the-local-api).
- Phase 2B working-tree React screen: exact-DIN input, metadata display, visible non-guidance notice, source details and empty/loading/error/retry states; see [frontend documentation](phase2b-frontend.md). Final production build/typecheck, zero-warning lint, 28 tests and zero-vulnerability npm audit passed. Browser-verification limits remain documented there.
- Live HTTP checks through Vite on port 5173 returned expected real metadata for the three DINs and safe HTTP 404 for 00000000. No backend CORS/source changes were needed.

## Not implemented

- Reviewed clinical catalogue publication, name search, QR workflows, approved translations and audio.
- Authentication, deployed authorization enforcement, patient verification, or QR generation/resolution; synthetic authorization rules are described below.
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

See the [vision](product-vision.md), [requirements](product-requirements.md), [catalogue](feature-catalogue.md), [UX research plan](ui-ux-blueprint.md), and [roadmap](project-roadmap.md). Phase 2A/2B implement only exact-DIN metadata lookup and its interface, not the full feature catalogue. T-011 has 53 passing mocked backend tests; T-012 covers frontend checks. Broader usability, RTL and accessibility validation remain open; AI-008 tracks unavailable real-browser QA and HUM-013 the supported local Node prerequisite.

The initial market is Manitoba. Simple mobile-first UX, progressive disclosure, multilingual accessibility and RTL support are core product principles. Optional audio and simplified language remain proposals. Medical accuracy, translation review, paper reduction and regulatory applicability require evidence and qualified review.

Outstanding decisions above are tracked in [Human TODO](../tasks/HUMAN-TODO.md): HUM-001 scope; HUM-002 languages/accessibility; HUM-003 content; HUM-004 future verification/QR; HUM-005 identity/roles; HUM-006 tenant ownership/isolation; HUM-007 legal/privacy research; HUM-008 operations; HUM-009 research arrangements; HUM-010 business model. [AI TODO](../tasks/AI-TODO.md) records dependent follow-up.

The former verification-MVP wording has been clarified in authentication/minimization guidance; mandatory future private-access controls are unchanged. Older architecture descriptions describe intended eventual capabilities, not initial release approval. No accepted ADR conflict is currently identified; release scope and technical details remain unresolved.

Phase 1B added [integration strategy](integration-strategy.md) (HUM-011/AI-005) and [pilot/impact evaluation](pilot-and-impact-evaluation.md) (HUM-012/AI-006); it authorized no external API access or pilot. No sustainability/clinical benefit is demonstrated and no real-patient collection is approved. The proposed synthetic public catalogue-to-QR demonstration with LTR/RTL fixtures still awaits HUM-001/HUM-002/HUM-003 and separate implementation approval; Phase 1C research is a separate read-only activity.

Phase 1C explicitly authorizes read-only public DPD research; it does not authorize an implemented government/PMS connection. [Research findings and proposed backend plan](health-canada-dpd-research.md) record three successful product-data lookups, official-page monograph references, response-shape and data gaps, and outstanding rights/operations questions. At the Phase 1C research stage, AI-007 and planned T-011 tracked follow-up and no executable tests existed. Subsequently, Phase 2A implemented T-011 with 53 passing mocked backend tests; T-001 through T-010 remain planned coverage. No API client, imported patient-facing content, schema, dependency installation, commit or push was created during this research.

The paragraph above records the completed Phase 1C research scope. The later Phase 2A instruction explicitly authorizes the minimal live DPD metadata backend and its tests, as detailed in [Phase 2A backend documentation](phase2a-backend.md). It does not approve publication, patient data, persistence or the full public QR prototype. Phase 2A subsequently received explicit commit/push approval and is recorded at `a4d2e29`. Phase 2B subsequently received explicit commit/push approval and was committed/pushed as `9ac2766`; Phase 2C remains uncommitted.

Finalization checks on 2026-10-22 with active Node.js 24.22.0: frontend production build, standalone TypeScript check, zero-warning lint and 28 frontend tests passed; all 53 backend tests passed without package restoration. Historical npm audit/live-DPD evidence above was not rerun. Manual browser and assistive-technology QA remains pending AI-008; the [manual checklist](phase2b-frontend.md#manual-browser-qa-checklist-ai-008--t-012) is ready. HUM-013 remains open pending explicit human confirmation.

## Phase 2C design review

The [core model](decisions/ADR-004-organization-authorization-foundation.md) and bounded synthetic implementation were explicitly approved on 2026-10-22. [Implementation and evidence](phase2c-identity-tenancy-design.md#implemented-synthetic-foundation): three dependency-free Domain/Application projects, test-only in-memory readers and 61 new authorization cases. All 114 backend tests and all 28 frontend tests passed; frontend build/typecheck/lint passed. An existing running API locked normal backend outputs; alternate ignored build outputs were used successfully without stopping it. No authentication, protected API, database, invitation workflow, audit store or dashboard is implemented. Provider/BFF and PostgreSQL application scoping plus RLS remain preferred directions awaiting separate implementation review. Review stop: no staging, commit, push or new scope.
