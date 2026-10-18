# Product vision

Status: Product direction approved for discovery; MVP scope not approved. Updated: 2026-10-17.

The QR Medication Information Platform aims to help people reach trusted medication information by scanning a QR code. The initial target market is Manitoba, Canada. Nothing described here is implemented.

## Phase 1B strategic objectives

The Product Owner's expanded direction, 2026-10-17, establishes four objectives for discovery and evaluation, not demonstrated benefits or an approved MVP feature list:

1. Patient understanding and accessible medication information: help people find and understand relevant reviewed public guidance and safety information.
2. Multilingual inclusion: design for older adults, newcomers, RTL users and people with disabilities, recognizing overlapping needs without assuming language or ability from identity.
3. Environmental sustainability: measure reduction of avoidable paper use where legally permitted, while preserving paper access when required or needed. Paper counts alone do not establish net environmental benefit.
4. Digital healthcare efficiency and future interoperability: reduce avoidable information-finding work and preserve a path to Canadian pharmacy/healthcare connections, without making external integration necessary to initial operation.

The [evaluation plan](pilot-and-impact-evaluation.md) defines proposed measures and approval gates. No improvement in comprehension, time, inclusion, paper use or health outcomes has been measured.

## Intended initial operating model

Operate independently with a centrally managed, reviewed medication catalogue. Pharmacists find appropriate existing information rather than manually writing full drug descriptions. Public QR access requires no patient account and leads to calm, mobile-first, multilingual and accessible information. Content retains provenance, review states, version history and correction handling under [content governance](medication-content-governance.md).

This is authorized product direction, not implementation approval. The precise catalogue, languages, roles, publication process and demonstrator scope still need HUM-001/HUM-002/HUM-003 decisions. [Integration strategy](integration-strategy.md) keeps future PMS access optional and separates pharmacy records from this platform's public content. Protected patient-specific instructions remain future capability.

## Approved product principles

- A calm, minimal, mobile-first patient experience with simple language and clear medication safety information.
- Multilingual accessibility and RTL support considered from the beginning, alongside large text and screen reader access.
- Progressive disclosure: show essential information clearly, with additional detail and powerful functions available through understandable controls.
- An intuitive pharmacist experience that supports work without unnecessary interface clutter.
- Public content provenance, medical review, medically reviewed translations, and content versioning as intended quality foundations; their operating process remains to be approved.
- Modular architecture that permits future capabilities without forcing them into the initial interface.

## Capabilities to evaluate

Public QR access is the central product concept. Optional audio guidance and simplified-language content are proposals, not approved MVP commitments. Protected patient-specific instructions remain a future capability until the Product Owner explicitly approves MVP scope. Whenever introduced, existing verification, authorization, tenant isolation, and QR security requirements remain mandatory.

Reducing paper consumption is a potential benefit only where legally permitted and operationally appropriate. No paper replacement, health-outcome improvement, regulatory compliance, or clinical effectiveness claim is established.

## Discovery outcomes

Validate whether patients can find and understand the information they need, whether pharmacists can maintain trustworthy content efficiently, and whether the approach works across language, accessibility, and device needs. Define measurable success criteria through research before approving a release scope.

See [requirements](product-requirements.md), [features](feature-catalogue.md), and [roadmap](project-roadmap.md). Scope approval is tracked as HUM-001 in [Human TODO](../tasks/HUMAN-TODO.md).
