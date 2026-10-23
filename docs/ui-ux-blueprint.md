# UI/UX blueprint and research approach

Status: Approved design principles with proposed broader layouts and research activities. Phase 2B implements only the exact-DIN product-metadata lookup screen; see [actual scope and verification](phase2b-frontend.md). Participant usability research remains unperformed.

## Experience principles (REQ-002, REQ-003)

Design mobile first. Use calm hierarchy, plain labels, readable text, obvious primary actions, and consistent recovery feedback. Progressive disclosure keeps secondary details and advanced staff actions available without crowding the primary task. Do not bury essential safety information behind an expansion merely to make a page look minimal.

Design multilingual and RTL behavior with the initial layouts, including text expansion, mixed-direction medication names, reading order, and control placement. Provide semantic structure for assistive technology and avoid relying on color alone.

## Proposed information architecture

- Public entry: content identity, language selection, essential information, clear safety content, expandable detail, source/revision context.
- Staff workspace: a task-focused starting view; only research-supported actions become candidates for approval. Access controls remain server-side.
- Future private flow: clearly separate verification from public reading; no patient-specific browser data before authorization.

Exact content hierarchy, labels, navigation, and dashboard actions require research (HUM-001, AI-001). See [journeys](user-journeys.md).

Phase 1B prioritizes finding existing central reviewed information rather than full-description authoring. A public-only finder and public medication page are the [proposed first demonstration](project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration); this does not approve a protected dashboard or PMS workflow. Public reading requires no patient account. Keep integration mechanics out of patient navigation and maintain essential safety visibility through progressive disclosure.

## UX research and usability testing

Recruit older adults, people with limited English proficiency including RTL language users, pharmacists, and people with accessibility needs or assistive technology experience. Include overlapping needs rather than treating groups as mutually exclusive. Recruitment channels, languages, accessibility accommodations, consent materials, and participant support need HUM-009 and HUM-002.

Begin with interviews and pharmacist workflow observation that excludes real patient data. Explore scanning habits, trust in information, language selection, reading needs, current paper use, and difficult recovery situations. Record findings as evidence with limitations; small qualitative samples do not establish population-wide outcomes.

Next use low-fidelity concepts and moderated task sessions with synthetic medication examples clearly marked as nonclinical research materials. Ask participants to find information, change language, identify source/revision context, expand detail, recover from unavailable content, and complete research-approved pharmacist tasks. Use a qualified language facilitator where needed and allow participants' usual assistive tools.

Observe task completion, misunderstandings, assistance needed, navigation errors, safety-information comprehension, and participant confidence. Agree success thresholds before summative validation; do not invent targets now. Combine screen reader, keyboard, text enlargement, RTL and mobile checks with participant feedback (T-002, T-003).

Minimize research records, avoid health histories and secret-bearing links, agree consent/retention before collection, and do not record sessions by default. Synthesize findings into linked F/REQ/T records, identify severity and uncertainty, revise concepts, and retest significant failures. AI-001 tracks synthesis; HUM-009 tracks human arrangements.

Include newcomers among the limited-English-proficiency research lenses without collecting immigration status. The [impact plan](pilot-and-impact-evaluation.md) adds comparable comprehension, pharmacist timing, paper accounting and satisfaction measures, with HUM-012 approval gates; these measures do not approve participant activities or establish benefits.

Phase 2B realizes a restrained single-page layout with a DIN form, persistent metadata-only notice, empty/loading/error/success states and source details behind a native disclosure. It uses the existing exact-DIN API; neither proposed name search nor QR generation is implemented. Editing a DIN clears stale metadata; cancellation/sequence guards avoid displaying an older response under new input. These interaction checks are automated under T-012; real-browser/participant accessibility findings are still required rather than inferred from DOM tests.
