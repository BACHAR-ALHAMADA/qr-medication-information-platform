# Human TODO

All entries are open / Decision Required. Do not mark complete without human confirmation. Documentation approval does not approve MVP features. IDs are stable.

| ID | Owner decision or action | Trace / timing |
|---|---|---|
| HUM-001 | Product Owner: approve explicit MVP inclusions/exclusions, public QR semantics, release priorities and measurable acceptance criteria after discovery. F-006 stays future scope until this decision. | [Catalogue](../docs/feature-catalogue.md), [roadmap](../docs/project-roadmap.md); before implementation planning |
| HUM-002 | Select initial languages, accessibility target/support matrix and validation criteria, including RTL and fallback behavior. | F-003, REQ-003, T-003; [accessibility](../docs/accessibility-and-localization.md) |
| HUM-003 | Identify authoritative sources, content reuse rights for qualified review, qualified medical/translation reviewers, publication responsibility and correction process, including how suspected inaccurate information is reported and triaged. | F-004/F-005, REG-004/005, planned T-004; [content governance](../docs/medication-content-governance.md); before publication. Reporting remains a research question, not approved MVP scope. |
| HUM-004 | If private scope is approved, decide patient matching/minimization, verification method and assurance, QR encoding/lifetime/reuse/reprint/revocation policy. | F-006, REQ-005, ADR-003, T-006; before private design/implementation |
| HUM-005 | Select staff identity provider, MFA/recovery/session/account lifecycle and role/permission matrix; clarify emergency-access policy. | F-002/F-006; [authentication](../docs/security/authentication.md), [authorization](../docs/security/authorization.md) |
| HUM-006 | Record distinct outcomes for tenant isolation (including whether PostgreSQL RLS supplements application scoping), patient/organization data ownership, and Audit responsibility (module or cross-cutting service). | ADR-002, REQ-006/008; [module boundaries](../docs/architecture/module-boundaries.md); before persistence |
| HUM-007 | Arrange qualified Manitoba/Canada legal/privacy research for the chosen model, organizational responsibilities, retention/deletion, notices, access/correction, incident and paper-use questions. | REG-001 through REG-006; [research register](../docs/regulatory-research.md); before affected pilot commitments |
| HUM-008 | Define operating/hosting ownership, load/availability targets, backup/recovery, security monitoring, incident response and support expectations. | [Scalability](../docs/architecture/scalability.md), [threat model](../docs/security/threat-model.md); before production design |
| HUM-009 | Arrange UX participants/facilitators and accessibility accommodations; approve consent, research-data access/retention and recording choices before collection. | J-001/J-002, T-002/T-003, REQ-009; [UX plan](../docs/ui-ux-blueprint.md) |
| HUM-010 | Validate buyer/funder/operator, adoption needs and sustainable content/support costs; decide business direction. | [Business discovery](../docs/business-model.md), HUM-001 |
| HUM-011 | Identify an actual PMS vendor/use case and authorize any outreach/access; confirm interface/version, sandbox, mapping terms, partner/commercial rights, fees and support obligations before integration design commitments. | F-007, REQ-011, REG-007, T-009; [integration strategy](../docs/integration-strategy.md). Future integration only; standalone work is not blocked. |
| HUM-012 | Approve stage-specific evaluation protocol, measures/targets, paper eligibility/baseline/denominators, safety stop criteria and any later pilot site/operating permissions. No real-patient collection is approved. | F-008, REQ-012, REG-002, T-010; [impact plan](../docs/pilot-and-impact-evaluation.md). Coordinate HUM-003/HUM-007/HUM-008 and HUM-009; no recruitment or pilot approval implied. |

## Responsibility boundaries

- HUM-002 selects languages, accessibility targets and validation criteria; HUM-009 arranges participants, facilitators, accommodations and authorized research-data handling against those needs.
- HUM-003 owns content/source selection and medical/language review arrangements; HUM-007 arranges qualified legal/privacy review, including applicable reuse rights and regulatory constraints.
- HUM-008 defines the operating model and operational responsibilities; HUM-007 investigates applicable legal/privacy constraints on that model. Neither substitutes for the other.
- HUM-011 owns actual vendor feasibility/access/commercial decisions; HUM-007 owns qualified legal/privacy review and HUM-006 owns persistence/tenant design. Standards research does not grant vendor access.
- HUM-012 owns evaluation design and future pilot go/no-go; HUM-009 owns participant/research-data arrangements, HUM-002 accessibility targets, and HUM-007 paper-use/privacy constraints. HUM-001 still approves development scope, including the [proposed public demonstration](../docs/project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration); none of these entries is complete.

## Phase 1C decision inputs — all still open

- HUM-001: review the [DPD source-lookup implementation proposal](../docs/health-canada-dpd-research.md) before code; authorization for real public product research does not approve patient-facing publication or expand the synthetic prototype automatically.
- HUM-003/HUM-007: confirm DPD metadata attribution/reuse and distinguish sponsor-monograph link-only use from reproduction, hosting, extraction, translation or audio rights. The dataset's licence listing is evidence, not blanket clearance for every linked document.
- HUM-008: approve upstream request volume, revalidation/freshness, retained-snapshot and outage policy before a live dependency. HUM-011 remains the separate PMS/vendor decision; successful DPD research grants no PMS access.

## Phase 2A scope authorization and remaining actions

The Product Owner explicitly approved the first exact-DIN read-only DPD backend and automated mocked tests, plus committing/pushing the six Phase 1C documentation files. This confirms only that sub-scope of HUM-001; broader MVP/publication decisions remain open. Implementation must remain uncommitted for review. No private data, persistence, frontend, summaries or translations are authorized.

- The Product Owner explicitly approved required test-only package restoration after the earlier automatic-review block. Restore/build succeeded and all 53 mocked tests passed (see [backend status](../docs/phase2a-backend.md)); this resolves only that restore authorization, not broader deployment/MVP decisions.
- HUM-008: choose a current supported patched .NET runtime/SDK before deployment; installed development tools are SDK 8.0.100/runtime 8.0.0. Local verification does not establish production readiness.
- HUM-003/HUM-007 reuse, medical publication and monograph rights decisions remain open. No human task is marked wholly complete by this implementation authorization.
