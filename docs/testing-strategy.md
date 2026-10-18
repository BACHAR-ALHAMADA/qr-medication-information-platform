# Testing and validation strategy

Status: Planned coverage only. No executable tests exist and none were run during documentation setup. Use synthetic patient data and no production secrets.

| ID | Planned coverage | Trace / method |
|---|---|---|
| T-001 | Public QR resolves correct public content; invalid/unavailable references recover safely | F-001, REQ-001; future API/integration and journey checks |
| T-002 | Simple mobile tasks, progressive disclosure, comprehension and recovery | F-001/F-002/F-003/F-005, REQ-002; moderated usability sessions |
| T-003 | Language selection, RTL/mixed text, reflow/large text, keyboard, screen reader and focus behavior | F-003, REQ-003; manual/participant checks plus future automated support |
| T-004 | Source/revision trace, translation linkage, review/publication gates, corrections, format equivalence; if a reporting workflow is approved, synthetic suspected-inaccuracy report intake, triage and affected-version linkage | F-004/F-005, REQ-004, HUM-003; synthetic workflow exercises and qualified medical/language review. Reporting is a research question, not approved MVP scope. |
| T-005 | Authorization, validation, safe errors, minimized telemetry and sensitive-data leakage | All relevant features, REQ-006/REQ-007; future integration/security tests |
| T-006 | Future private access boundaries, tenant isolation, failed verification, copied/expired/revoked tokens, no premature browser data | F-006 and protected F-002 operations, REQ-005/REQ-006; adversarial integration/security tests |
| T-007 | Dependency direction, module ownership and explicit contracts | REQ-008, ADR-001/ADR-002; future architecture checks and review |
| T-008 | Synthetic fixtures/materials and absence of real patient data or production secrets | REQ-009; review of research materials and future fixtures |
| T-009 | Future approved connector contracts, identifier mapping/version drift, ambiguous/unknown products, external input validation, authorization revocation, tenant boundaries and outage fallback | F-007, REQ-011; vendor-supported synthetic contract tests only after HUM-011; no API access or conformance claim assumed |
| T-010 | Stage-specific impact protocol, comprehension/translation checks, accessible tasks, comparable workflow timing, paper baseline/denominators/fallback accounting and minimized research records | F-008, REQ-012; [evaluation plan](pilot-and-impact-evaluation.md); no outcome or participant collection implied |
| T-011 | Proposed DPD source adapter: exact DIN/code association, English/French metadata, ingredient units, object/array/null/empty handling, multiple ingredients, no-match, partial failure, timeouts, throttling, malformed response and no automatic publication | F-001/F-004, REQ-001/REQ-004/REQ-010; [Phase 1C research and plan](health-canada-dpd-research.md). Manual read-only research requests succeeded; no application tests or adapter exist. Deterministic CI should use controlled responses, not depend on live API access. |

## Discovery validation

Follow the [UX research plan](ui-ux-blueprint.md) with older adults, people with limited English proficiency, RTL users, pharmacists and participants with accessibility needs. Agree recruitment, accommodations, measures and thresholds before validation; do not invent numeric targets or claim usability success without evidence.

Record each result with the test ID, scenario, artifact/version, configuration/language, observed outcome, evidence limitations, and linked requirements/features. Log discovered defects in tasks/BUGS.md and meaningful corrections in tasks/FIXES.md with original IDs. Do not confuse proposed coverage with passed tests.

## When implementation is approved

Add deterministic xUnit tests for Domain/Application behavior and integration tests for API/persistence boundaries. Include authorization, tenant isolation, QR access, input validation, and leakage tests. Verify isolation against the chosen persistence mechanism, not only mocked services. Keep medical correctness review distinct from software correctness.

Before any pilot, resolve applicable content, privacy/security, accessibility, and operations decisions; revisit the threat model and document actual results and residual issues. Tooling and exact checks will follow approved implementation scope; no dependencies are installed now.

The proposed first public demonstration uses T-001/T-003/T-004/T-005/T-007/T-008 for technical and fixture checks; T-002 participant sessions require HUM-009 authorization. T-010 at demonstration stage checks measurement readiness and simulated counts only, not real-world impact. Acceptance scenarios are in the [roadmap](project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration). No test IDs denote implemented or passed tests.
