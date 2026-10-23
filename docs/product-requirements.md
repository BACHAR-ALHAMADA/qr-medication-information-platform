# Product requirements and documentation standard

Status: Discovery baseline. No release scope or application implementation is approved by this document.

## Status vocabulary

- **Approved decision/principle:** explicitly authorized direction; cite the owner instruction or accepted ADR.
- **Proposal / awaiting Product Owner approval:** candidate behavior, not an implementation commitment.
- **Assumption:** a belief requiring validation, not a requirement.
- **Research question:** evidence needed, including unverified medical and regulatory matters.
- **Decision Required:** an unresolved choice; link its Human TODO entry.

Approval status, release priority, and implementation status are separate fields. Accepted ADRs remain accepted. Proposed acceptance criteria do not become approved requirements through documentation alone.

## Stable requirements

| ID | Requirement or principle | Status / source | Feature / planned test |
|---|---|---|---|
| REQ-001 | QR-based access to trusted public medication information | Product concept; release behavior awaits HUM-001 | F-001 / T-001 |
| REQ-002 | Calm, simple, mobile-first UX with progressive disclosure | Approved Product Owner principle, 2026-10-17 | F-001, F-002 / T-002 |
| REQ-003 | Multilingual accessibility, RTL, large text, and screen reader needs shape design from the beginning | Approved Product Owner principle, 2026-10-17; language list and validation targets pending HUM-002 | F-003 / T-003 |
| REQ-004 | Distinguish provenance, content revisions, and medically reviewed translations | Product direction; workflow awaits HUM-003 | F-004 / T-004 |
| REQ-005 | Private instructions require separate verification and server authorization; QR possession alone is insufficient | Mandatory existing security principle, ADR-003 | F-006 / T-006 |
| REQ-006 | Protected staff operations require verified tenant membership and least privilege | Mandatory existing security guidance; bounded organization/branch/capability model approved 2026-10-22 in [ADR-004](decisions/ADR-004-organization-authorization-foundation.md), synthetic T-013 subset implemented | F-002, F-006 / T-005, T-006 |
| REQ-007 | Collect and expose only purpose-bound data; avoid sensitive telemetry | Mandatory existing privacy guidance | All applicable features / T-005 |
| REQ-008 | Keep modular monolith boundaries and the planned PostgreSQL stack | Accepted ADR-001 and ADR-002 | All future implementation / T-007 |
| REQ-009 | Use synthetic patient data only in design, research materials, and testing | Approved project constraint | All / T-008 |
| REQ-010 | Standalone central reviewed catalogue; pharmacist information selection without full-description authoring; no patient account for public QR reads | Phase 1B Product Owner operating-model direction, 2026-10-17; delivery scope awaits HUM-001/HUM-003 | F-001/F-002/F-004 / T-001/T-004 |
| REQ-011 | Preserve future PMS interoperability through explicit boundaries, minimum authorized data and provider-independent adapters; no assumed API access | Phase 1B strategy direction; actual integration is future proposal awaiting HUM-011 | F-007 / T-009 |
| REQ-012 | Evaluate comprehension, inclusion, workflow, avoidable paper, content integrity and satisfaction without claiming unmeasured benefits | Phase 1B evaluation direction; protocol, targets and activities await HUM-009/HUM-012 | F-008 and existing public workflow / T-010 |

## Feature record standard

Every feature must eventually document business purpose, target users, functional requirements, UX requirements, privacy/security considerations, dependencies, acceptance criteria, testing requirements, priority, and implementation status. Initial records are in the [catalogue](feature-catalogue.md); unresolved details stay explicit rather than invented.

Use stable IDs: F- for features, REQ- for requirements, J- for journeys, REG- for regulatory and medical accuracy research questions, T- for planned tests, HUM- and AI- for tasks, BUG- and FIX- for issue history, and existing ADR numbers for decisions. Never renumber or reuse an ID. Link changes and fixes to their originating record. Test IDs describe planned coverage until executable tests exist and are run.

Before implementation, obtain scope approval, resolve relevant dependencies, and make acceptance criteria measurable. See [testing strategy](testing-strategy.md) and [task trackers](../tasks/AI-TODO.md).
