# Regulatory and medical accuracy research register

Status: Open research questions only. Initial market: Manitoba, Canada. No legal, regulatory, licensing, or medical conclusion has been verified for this register. Phase 1B checked introductory primary standards references linked in [integration strategy](integration-strategy.md); those references establish neither API access nor legal applicability.

| ID | Question to investigate | Needed evidence / review | Trace |
|---|---|---|---|
| REG-001 | Which privacy/health-information rules and organizational responsibilities apply to each proposed operating model? | Current primary authority and qualified privacy/legal interpretation of actual data flows | HUM-007, F-006, REQ-007 |
| REG-002 | When, if ever, may digital material reduce or replace paper medication information? What must remain on labels or paper? | Applicable authority, pharmacy workflow and professional review; distinguish supplement from replacement | HUM-007, product vision |
| REG-003 | Which accessibility and language obligations apply, and which standards should the product adopt as a design target? | Current authority and accessibility expertise; separate legal duties from voluntary targets | HUM-002, HUM-007, F-003 |
| REG-004 | Which medication sources and translations can be used, adapted, displayed or voiced, under what rights and attribution? | Source terms, permissions and qualified medical/editorial review | HUM-003, HUM-007, F-004, F-005 |
| REG-005 | How should qualified reviewers verify clinical accuracy, safety wording, translation equivalence, and correction handling? | Reviewer qualifications, source evidence, documented clinical review procedure | HUM-003, F-004, T-004 |
| REG-006 | What hosting, record handling, retention, access/correction, incident and cross-border processing constraints apply? | Confirmed service/data model, primary authority and qualified review | HUM-007, HUM-008, F-006 |
| REG-007 | Which DIN/CCDD mappings, HL7 FHIR versions/Canadian profiles and vendor contracts are applicable to an authorized integration, with what access, reuse and conformance conditions? | Actual vendor specification and permissions, current primary standards/terms, mapping evidence and qualified review; no presumed government/PMS API availability | HUM-011, HUM-003, HUM-007, F-007, T-009 |

## Evidence recording standard

For each investigation, record the exact source/title, issuing authority, URL or document reference, publication/effective date, access date, jurisdiction, applicability assumptions, reviewer, finding, limitations, and affected F/REQ/ADR/task IDs. Mark findings unverified until reviewed by an appropriately qualified person. Outdated or ambiguous evidence stays a research question.

Research findings do not automatically approve feature scope or alter security principles. Record resulting owner decisions separately and link any accepted ADR changes explicitly. AI-003 may prepare a source-based evidence dossier using labelled operating-model assumptions before final decisions; qualified applicability review remains necessary. AI-005 covers vendor-specific feasibility. REG-002 also governs the paper eligibility assumptions used in the [impact plan](pilot-and-impact-evaluation.md); no reduction intervention is approved.
