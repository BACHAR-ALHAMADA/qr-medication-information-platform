# Project status

Last updated: 2026-10-26 (America/Winnipeg)

## Current phase

**Phase 2D local PostgreSQL foundation — end-of-day review/publication.** Seven application tables, EF migrations, monotonic revision triggers, separated roles and FORCE RLS on six tables are implemented for LOCAL DEVELOPMENT AND TESTING ONLY. A trusted synthetic harness uses parameterized transaction-local context; no production actor binding, live runtime reader adapter or protected endpoint exists. See [current evidence and limitations](phase2d-1c-rls-tenant-context.md).

Local Release builds passed with zero warnings/errors; 106 unit, 8 mocked/in-process API and 59 PostgreSQL tests passed (173 total; zero failed/skipped). Existing hosted Backend/Frontend CI does not run PostgreSQL integration tests. New hosted results must be reported independently after the Draft PR runs.

The Product Owner authorized reviewed Phase 2D commits, a non-force feature-branch push and a Draft PR targeting main; no merge or new feature scope is authorized. The earlier Phase 2A/2B/2C and CI evidence below is historical. Clinical publication, private patient data and the broader MVP remain future scope.
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
- Production persistence deployment, live authorization reader adapters and runtime identity binding; local schema/migrations/RLS are implemented as described above.
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

## Phase 2D proposal

Phase 2C and CI were squash-merged in PR #1 as `0f3fb2749cab6809cbc9907fcc07ee9c1e8ad475`; the reviewed feature head had passing hosted Backend/Frontend checks. Current planning branch is `feature/postgresql-tenant-isolation`. The [persistence and RLS plan](phase2d-postgresql-tenant-isolation-plan.md) is unapproved documentation, not a database implementation. T-015 is planned only. No installation, container, schema, migrations, package addition, application change, commit or push was performed for this planning task. Earlier Phase 2C review-stop wording above records the historical pre-merge state.

## Phase 2D-0 runtime upgrade - current review state

The bounded .NET 10 prerequisite is authorized and implemented in the working tree; see [versions, verification and scope separation](phase2d-0-dotnet10-upgrade.md). All eight projects target net10.0, root global.json pins SDK 10.0.402, and CI reads that pin. ASP.NET integration-test hosting is the only direct package upgrade. Historical .NET 8/Phase 2B/Phase 2C evidence above is retained. PostgreSQL, EF Core, RLS and authentication remain unimplemented; the original Phase 2D proposal remains unchanged and unapproved for persistence. No staging, commit or push is authorized.

## Phase 2D-1 safety review — 2026-10-25

Current branch: feature/postgresql-foundation; .NET 10 prerequisite is merged. Database implementation stopped because Docker was unavailable. No PostgreSQL/EF/RLS implementation added. Restore and Release build passed (zero warnings/errors); 114 existing backend tests passed, zero failed/skipped. See [session evidence](phase2d-1-safety-review.md) for preserved work, tooling, decisions and verification limits. Earlier review-state statements above are historical.

## Phase 2D-1A local PostgreSQL checkpoint - 2026-10-25

The Product Owner authorized local development PostgreSQL setup only and confirmed Docker installation. Pinned PostgreSQL 18.6 is running and healthy on 127.0.0.1:55432 with a persistent named volume and ignored local administration credentials; authenticated TCP SQL verification passed. See [setup, exact commands and evidence](phase2d-1a-local-postgresql.md). No schema, EF adapters/migrations, RLS or application wiring exists. Earlier Docker-blocker statements are historical; Phase 2D remains incomplete. HUM-006 architecture decisions and later persistence approvals remain open. Stop for review; no staging/commit/push or whole-task closure.
## Phase 2D-1B schema decision gate - 2026-10-25

Bounded EF/schema/migration/constraint-test work is authorized, but the request explicitly reserves unresolved architectural decisions. HUM-006 cross-module identity FK exception and revision triggers versus constrained writer remain unresolved; complete seven-table migration/test work stopped before implementation. Selected metadata-compatible EF Core 10.0.13 and Npgsql provider/driver 10.0.3, without installation or real-DB stack verification. Existing 114 backend tests passed; PostgreSQL is healthy with zero application tables. No new database tests or schema changes. See [decision options and evidence](phase2d-1b-schema-review.md). AI-010 resumes after these explicit outcomes. No whole human item closed; all existing work, stash and backups preserved.
## Phase 2D-1B approved foundation implemented - 2026-10-25

The Product Owner explicitly approved the restrictive Identity FK exception and database-owned revision triggers; see [ADR-005](decisions/ADR-005-postgresql-foundation-integrity.md). Separate module EF infrastructure and one offline migration now create the seven-table schema locally, with tenant-consistent constraints and hardened revision-maintainer privileges. EF 10.0.13/Npgsql 10.0.3 restore/build and local migration passed; 27 real PostgreSQL tests plus all 114 existing backend tests passed (141 total; zero failed/skipped). See [implementation, commands and limits](phase2d-1b-postgresql-foundation.md). Earlier decision-blocker statements are historical for these two approved subdecisions. No runtime adapters or RLS were added; remaining HUM-006 isolation/context/bootstrap and later runtime gates remain open. No whole human task is closed. Stop for review, without staging/commit/push.
## Phase 2D-1C local-only RLS/context checkpoint - 2026-10-25

Explicit Product Owner approval now covers local RLS, role separation, transaction-local context and narrowly self-scoped bootstrap; see [ADR-006](decisions/ADR-006-local-postgresql-rls-context.md). A second local migration applies six ENABLE/FORCE tables and eight reviewed policies with distinct owner/migrator/setup/reader/maintainer roles. A trusted synthetic harness uses parameterized read-only Repeatable Read scopes; production actor authentication is absent. All 59 PostgreSQL and 114 existing backend tests passed (173 total; zero failed/skipped). Read-pool reuse/cancellation and snapshot revocation limits are verified, including the accepted context-forgery threat demonstration. See [commands, policy/privilege review and limitations](phase2d-1c-rls-tenant-context.md). Earlier no-RLS statements are historical. No API/frontend/Domain/Application changes, live reader adapters or startup migrations. Stop for review; no stage/commit/push or whole human task closure.