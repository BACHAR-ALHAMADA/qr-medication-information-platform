# Integration strategy

Status: Phase 1B strategy direction requested by the Product Owner, 2026-10-17. Future integration designs are proposals, not approved MVP scope. No integrations, vendor agreements, credentials or API access exist in this repository.

## Standalone first (REQ-010)

The intended initial service manages a central reviewed medication catalogue. Pharmacists find appropriate public information and its public QR link without authoring full drug descriptions or connecting a Pharmacy Management System (PMS). Patients read public information without patient accounts. Content production and publication remain governed by [content governance](medication-content-governance.md); a pharmacist's selection is not publication approval or a patient prescription.

Standalone operation must remain usable if a future connector is unavailable. Do not make PMS connectivity, government API access, patient records, or dispensing events prerequisites for the [proposed public demonstration](project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration).

## System-of-record boundary

The pharmacy PMS remains the pharmacy's system of record for its prescribing/dispensing/patient workflows. This platform owns its public catalogue, provenance, review states, revisions and QR resolution. It does not replace the PMS, reconcile medication histories, or update a prescription as part of this strategy. ADR-002's PostgreSQL system of record means this application's own records, not authority over pharmacy clinical records; the accepted ADR is unchanged.

Future patient-specific exchanges require separate scope approval, verified authority, and the existing private-access safeguards (F-006). Access to public product information must not implicitly authorize patient data, tenant records or a write-back to the PMS.

## Candidate identifiers and standards (REQ-011, F-007)

These are research candidates, not selected protocols or evidence of compatibility. Checked primary references on 2026-10-17:

| Candidate | Potential use to investigate | Limits / source |
|---|---|---|
| Canadian DIN | Product matching between vendor identifiers and catalogue entries | Health Canada describes DIN as an eight-digit product identifier. A DIN is not a patient identifier, content licence or patient-information document. Keep it as a validated string, preserving leading zeros if adopted. [Health Canada DIN fact sheet](https://www.canada.ca/en/health-canada/services/drugs-health-products/drug-products/fact-sheets/drug-identification-number.html) |
| Canadian Clinical Drug Data Set (CCDD) | Investigate medication terminology, versioned mappings and appropriate concept granularity | Infoway lists releases, editorial and technical resources; access terms, mapping coverage, release choice and applicability need verification. Do not assume one-to-one DIN/concept/content mapping or that terminology supplies reviewed patient education. [Infoway CCDD resources](https://accelero.infoway-inforoute.ca/en/standards/terminology-standards/canadian-clinical-drug-data-set) |
| Applicable HL7 FHIR standards | Investigate medication representation and agreed exchange contracts when a vendor use case exists | HL7's R4 Medication resource describes medication identification/definition. This is a reference example, not selection of R4 or a Canadian profile. Agree the vendor's supported version, relevant Canadian implementation guide/profile, terminology and conformance tests first. A standard does not grant access. [HL7 Medication R4](https://hl7.org/fhir/R4/medication.html) |

DIN/CCDD values, medication names and vendor keys belong in validated catalogue/mapping workflows, not QR payloads. Public QR URLs retain opaque public references under existing project guidance. Ambiguous, missing, withdrawn or changed mappings must fail safely for human review, never silently select a clinically different product.

## Proposed provider-independent adapters

Keep a small, use-case-specific Application contract separate from vendor formats; implement an adapter in Infrastructure only after an actual integration is approved. Adapters validate and translate vendor messages into module-owned requests and results. Domain and public UI should not depend on vendor SDKs, and connectors must not directly read another module's tables. This remains inside the accepted modular monolith; no connector service, broker or generic integration framework is proposed for the first milestone.

Start any later investigation with product-reference lookup only if it meets the agreed need. Define direction of exchange, minimum fields, allowed actions, mapping provenance/version, error handling and outage fallback before extending to clinical information. Idempotency, retry limits and change reconciliation need design if writes or event processing are ever approved; none is authorized here.

## Authorization, privacy and tenant boundaries

- Obtain explicit pharmacy and vendor authorization for the exact integration purpose, organization, fields and operations. Do not infer authority from possession of a vendor identifier or credentials.
- Verify tenant membership for staff actions and bind each connection to its authorized tenant server-side. Reject cross-tenant references; test connector errors and telemetry for leakage.
- Prefer product-only data when sufficient. Do not collect patient identity, prescription details or dispensing records to find public information. Patient-specific information remains out of current scope.
- Apply least-privilege scopes, boundary validation, HTTPS, secure secret storage, credential revocation and audit with no raw payloads, credentials, patient data or secret-bearing URLs in logs.
- Do not fetch arbitrary vendor-supplied URLs or trust external content as reviewed. Define allowed destinations and review all imported content/mappings. Assess hosting, retention and third-party processing through HUM-007/HUM-008.

## Dependencies, risks and research

HUM-011 must identify an actual vendor/use case and confirm supported interface/version, sandbox, documentation, commercial rights/fees, partner approval, certification needs if any, support, version-change policy and contractual data responsibilities. No government or PMS API availability is assumed; do not contact vendors or connect accounts without authorization.

REG-007 tracks standards applicability, source terms and integration obligations. Technical risks include wrong-product matching, outdated terminology, vendor version drift, external outages, overbroad permissions and cross-tenant leakage. Commercial risks include unavailable access, fees, contracts, vendor lock-in and maintenance costs. Lack of access must leave the standalone workflow viable.

AI-005 will prepare a capability/mapping evidence matrix after the relevant use case is clarified. Future T-009 coverage must include ambiguous mappings, revoked authorization, malformed messages, tenant boundaries, outage fallback and supported contract versions. No adapter implementation or conformance claim is approved.
