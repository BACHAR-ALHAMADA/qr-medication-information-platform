# Feature catalogue

All features are unimplemented. Release priorities and MVP inclusion await HUM-001; product principles do not automatically approve individual features. Criteria below are proposed review criteria, not completed tests. REQ IDs are defined in [requirements](product-requirements.md); T IDs in [testing](testing-strategy.md).

## F-001 — Public QR medication information

- **Purpose / users:** Help patients and supporting caregivers reach trusted general information.
- **Functional requirements:** Proposed QR entry to the relevant public content; distinguish source and revision; handle unavailable or invalid references without exposing private records (REQ-001, REQ-004).
- **UX:** Essential information first, clear safety information, mobile layout, optional detail expansion (REQ-002, REQ-003).
- **Privacy/security:** Public references grant no patient access; no sensitive QR payloads; minimized telemetry.
- **Dependencies:** Content sources and review process (HUM-003), public QR semantics and scope (HUM-001); HUM-004 applies only to future private access, and ADR-003 remains binding for private tokens.
- **Proposed acceptance:** A synthetic scan reaches the intended public content with identifiable source/revision; invalid references offer a safe recovery path; no private fields appear.
- **Testing:** T-001, T-002, T-003, T-004, T-005.
- **Priority / status:** Central concept; release priority awaiting approval; discovery only.

## F-002 — Pharmacist dashboard

- **Purpose / users:** Support pharmacists in relevant content and QR workflows with low cognitive burden.
- **Functional requirements:** Candidate authorized staff workspace; actual actions and role permissions require workflow research and approval (REQ-006).
- **UX:** Clear primary tasks, understandable status, progressive disclosure of secondary controls; validate with pharmacists rather than assume a workflow.
- **Privacy/security:** Server-side authorization and verified tenant context for protected actions; no sensitive payloads in logs.
- **Dependencies:** HUM-001 scope, HUM-005 identity/permissions, HUM-006 tenant design, AI-001 research synthesis.
- **Proposed acceptance:** Representative pharmacists can complete approved synthetic tasks; unauthorized and cross-tenant attempts are denied. Task targets remain Decision Required.
- **Testing:** T-002, T-005, T-006.
- **Priority / status:** Proposed; awaiting Product Owner approval; unimplemented.

## F-003 — Accessible multilingual experience

- **Purpose / users:** Make information usable across language, literacy, vision, motor, and assistive technology needs.
- **Functional requirements:** Design for language selection, RTL presentation, large text, and screen readers (REQ-003). Initial languages and supported configurations await HUM-002.
- **UX:** Discoverable language controls using understandable language names; correct reading/focus order and mixed-direction content; preserve clarity when text expands.
- **Privacy/security:** No language-based inference of medical identity; translated safety content follows review governance.
- **Dependencies:** HUM-002 language/accessibility targets, HUM-003 qualified translation review.
- **Proposed acceptance:** Reviewed sample content remains readable and operable under agreed text enlargement, keyboard, screen reader, RTL, and mobile conditions; participants can locate language controls.
- **Testing:** T-002, T-003, T-004.
- **Priority / status:** Approved foundational principle; delivery scope and criteria awaiting approval; unimplemented.

## F-004 — Reviewed and versioned medication content

- **Purpose / users:** Help patients and pharmacists distinguish trustworthy current material from drafts or outdated information.
- **Functional requirements:** Proposed provenance, revision identity, medical review, translation review, and correction workflow (REQ-004).
- **UX:** Show understandable source/revision context without clutter; make safety-critical corrections clear.
- **Privacy/security:** Controlled publication permissions and auditable changes; public content must not contain patient-specific instructions.
- **Dependencies:** HUM-003 sources, licensing, reviewers, ownership and publication process; HUM-007 research.
- **Proposed acceptance:** A synthetic publication exercise traces a displayed version and translation to its source and review; unreviewed material cannot be represented as approved.
- **Testing:** T-004, T-005.
- **Priority / status:** Product quality direction; operational requirements awaiting approval; unimplemented.

## F-005 — Optional audio and simplified language

- **Purpose / users:** Explore support for people who benefit from listening or simpler explanations.
- **Functional requirements:** Candidate optional audio guidance and reviewed simplified content; production method and scope undecided.
- **UX:** User-controlled playback with visible text equivalent; simplified content must preserve safety meaning.
- **Privacy/security:** Evaluate external audio processing before use; do not transmit patient information to content services.
- **Dependencies:** HUM-001, HUM-002, HUM-003; medical review of equivalence and usability evidence.
- **Proposed acceptance:** Users can choose and stop audio; text equivalent is available; qualified reviewers approve meaning across formats.
- **Testing:** T-002, T-003, T-004, T-005.
- **Priority / status:** Proposal awaiting Product Owner approval; no MVP commitment; unimplemented.

## F-006 — Protected patient-specific instructions

- **Purpose / users:** Future narrow access to instructions assigned to a patient, supported by authorized staff.
- **Functional requirements:** Future assignment access only after separate verification and server authorization; details remain undecided (REQ-005, REQ-006, REQ-007).
- **UX:** Explain public versus private content and recovery paths clearly; verification method must be researched.
- **Privacy/security:** ADR-003; opaque high-entropy bearer references, verifier storage where practical, expiry/revocation, no token logging, tenant scoping, minimized data and narrow access.
- **Dependencies:** Explicit scope approval HUM-001, verification/QR decisions HUM-004, identity HUM-005, isolation HUM-006, privacy/legal research HUM-007, operations HUM-008.
- **Proposed acceptance:** No private data reaches the browser before successful separate identity verification and server authorization. Possession of a copied token alone must never authorize private patient-data access; a token cannot necessarily be detected as copied. Expired and revoked tokens must be rejected. Failed verification, unauthorized staff, and cross-tenant requests cannot disclose private data.
- **Testing:** T-005, T-006, T-008; security review before any pilot.
- **Priority / status:** Future capability; not approved MVP scope; unimplemented. Mandatory security rules apply whenever introduced.
