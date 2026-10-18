# Project roadmap

Status: Proposed sequence within documentation-led planning; no delivery dates, approved MVP feature set, or implementation authorization.

| Stage | Outcome / exit evidence | Dependencies |
|---|---|---|
| Current: Phase 1 foundation and discovery | Existing architecture/security principles preserved; vision, candidate features, research questions and traceability documented | Product Owner review of this documentation diff |
| Current subphase: Phase 1B strategy | Standalone central-catalogue direction, future integration boundaries, inclusive evaluation and conditional sustainability measures documented | Review [integration](integration-strategy.md) and [impact plan](pilot-and-impact-evaluation.md); HUM-011/HUM-012 remain open |
| Product and UX discovery | Validated needs, pharmacist task evidence, language/accessibility priorities, tested concepts and recorded limitations | HUM-002, HUM-009, HUM-010; AI-001; T-002/T-003 |
| Content and operating-model research | Source/reviewer/translation approach and qualified regulatory/privacy findings for a defined model | HUM-003, HUM-007, HUM-008; AI-003 |
| Scope and design approval | Explicit MVP inclusion/exclusion, measurable acceptance criteria, required decisions and residual risks reviewed | HUM-001 and relevant dependencies; AI-004 |
| Future implementation planning | Approved scope mapped to modules, security controls and executable test plan | Separate authorization; review all task/bug files before a new phase |
| Future pilot readiness | Actual verification results, content review and operational/security readiness evidence | Approved implementation and resolved pilot prerequisites; AI-002 |

Research activities may inform one another; do not interpret this sequence as requiring every future private-access decision before public-only discovery. Resolve each decision before the activity it actually governs.

F-006 remains future capability until MVP scope is explicitly approved. Existing private-access safeguards remain mandatory if it is introduced. No roadmap entry authorizes code, schema changes, dependency installation, deployments, commits or pushes.

## Proposed next milestone — public catalogue-to-QR demonstration

Status: Smallest proposed demonstrable public workflow, awaiting explicit HUM-001 approval before implementation. It covers a thin part of F-001/F-002/F-003/F-004, not the full MVP or a deployable clinical service. No F-005/F-006/F-007 implementation, editorial dashboard, dispensing record, patient account, patient assignment, live integration or real-patient collection is included.

**Workflow:** A pharmacist-role demonstrator finds a fictional catalogue item, confirms its displayed identity and available languages, and opens/displays its public QR. A phone scan opens the same public information without a patient account. The reader can change language, read essential safety information, expand secondary details and inspect source/revision status. The finder uses public data only; it is not an authenticated staff dashboard and cannot mutate catalogue or tenant records.

### Required screens and states (proposed)

| View | Minimum purpose / UX | Trace |
|---|---|---|
| Public catalogue finder with selected-item panel | Search a small curated fixture set by display name; confirm product identity; show available languages and public link/QR | F-002, REQ-010, J-002 discovery lens |
| Public medication information | Clear nonclinical demonstration banner; essential information and safety section first; language selector, accessible secondary disclosure, source/revision context | F-001/F-003/F-004, REQ-001 through REQ-004, J-001 |
| Shared empty/unavailable/error states | No search result, unknown or withdrawn reference, missing translation and network failure; understandable retry/back navigation without invented advice | T-001/T-003/T-004/T-005 |

### API capabilities and data dependencies (proposed, no schema)

Read-only REST capabilities: list/search eligible public catalogue fixtures with bounded validated input; resolve an opaque public reference to the currently eligible published fixture revision; list available languages; return the selected language with source-version and review-state metadata. Return explicit unavailable states for unknown/withdrawn content or unavailable languages; never substitute an unreviewed/stale translation silently. Exact paths, response contracts and status codes require implementation planning after approval.

Build QR URLs from the canonical public route and opaque public reference. The QR carries no medication name, DIN, patient data, tenant data or private access token. QR rendering may be client-side; no separate generation service is required. Reference-to-revision behavior and correction/reprint implications need HUM-001/HUM-003 approval before implementation.

Use a small deterministic fixture set: fictional display identity, opaque reference, structured public sections, source placeholder explicitly marked synthetic, source/local revision, language/direction and review-state metadata. Include a published simulation, draft/withdrawn examples and an unavailable or stale translation case. Supply LTR and RTL fixtures; language choice needs HUM-002. Review-state values simulate a workflow, not medical approval. Do not fabricate a real DIN/CCDD mapping or label fixtures medically reviewed. HUM-003 defines any qualified review needed for participant-facing wording.

Fixture storage behind an Application-facing read interface is a candidate demonstration shortcut. It introduces no database schema and does not supersede PostgreSQL ADR-002. Confirm it under HUM-001; any subsequent persistence requires HUM-006 isolation decisions. No production content ingestion, live source download or publication editor is needed for this slice.

### Proposed acceptance and testing

1. Selecting a fixture and scanning its public QR resolve to the same intended item/revision without login, a patient account or PMS connectivity (REQ-010, T-001).
2. QR decoding reveals only the service URL and opaque public reference. API/browser results contain no patient or tenant fields; validated search and safe error/telemetry behavior are checked (T-005/T-008).
3. Essential safety fixture content is visible without expanding secondary details. Keyboard, screen reader, mobile reflow and text enlargement checks succeed against an agreed HUM-002 configuration matrix (REQ-002/REQ-003, T-003). Do not claim usability success before authorized participant sessions (T-002).
4. Switching between the agreed LTR/RTL fixtures preserves item identity, correct language/direction, reading/focus order and source/revision context. Missing/stale translations show a clear unavailable state; no silent clinical-language substitution (T-003/T-004).
5. Draft/withdrawn fixtures cannot be returned as published public content; a synthetic revision change and correction scenario resolve predictably under the approved reference policy. All displayed content remains labelled nonclinical demonstration material (T-001/T-004/T-008).
6. Read-only use cases follow module boundaries with focused Domain/Application unit coverage and API integration checks when implementation begins; no tenant writes or external APIs exist in the slice (T-007). A manual phone scan and documented configurations supplement automated checks.

HUM-001 must approve scope, fixture/reference policy and acceptance; HUM-002 must choose demonstration languages and accessibility targets; HUM-003 must approve fixture/review-state conventions. AI-004 will reconcile the approved scope before code. Separate HUM-009 authorization is needed for recruitment/data collection; HUM-012 is needed for impact evaluation beyond simulated readiness checks. The demonstration cannot establish comprehension gains, medical accuracy, paper savings or integration compatibility.
