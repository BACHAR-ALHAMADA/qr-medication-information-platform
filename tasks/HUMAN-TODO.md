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

## Responsibility boundaries

- HUM-002 selects languages, accessibility targets and validation criteria; HUM-009 arranges participants, facilitators, accommodations and authorized research-data handling against those needs.
- HUM-003 owns content/source selection and medical/language review arrangements; HUM-007 arranges qualified legal/privacy review, including applicable reuse rights and regulatory constraints.
- HUM-008 defines the operating model and operational responsibilities; HUM-007 investigates applicable legal/privacy constraints on that model. Neither substitutes for the other.
