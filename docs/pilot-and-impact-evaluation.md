# Pilot and impact evaluation plan

Status: Proposed evaluation design for Phase 1B. No pilot, recruitment, real-patient data collection, outcome or environmental saving is approved or reported. Baseline values, sample size, thresholds, sites and schedule are Decision Required (HUM-012).

## Three distinct evidence stages

| Stage | Allowed scope after its approval | Evidence limits / gate |
|---|---|---|
| Synthetic demonstration | Fictional catalogue fixtures and simulated selection/scan tasks; no real patient data or external integration | Tests technical behavior, layout and version rules only. Product Owner must approve the proposed development milestone (HUM-001); this plan does not authorize implementation. |
| Usability research | Authorized participants perform synthetic tasks; include older adults, newcomers/people with limited English proficiency, RTL users, pharmacists, and people with disabilities | HUM-009 governs recruitment, consent, accommodations and data handling; HUM-002 selects languages/validation targets. No medication histories or real patient records. Qualitative findings do not establish clinical benefit or population outcomes. |
| Future real-world pharmacy pilot | Only a separately approved protocol with a named operator, site permission, content review and applicable legal/privacy review | HUM-012 plus HUM-003/HUM-007/HUM-008 and relevant scope decisions. Define permitted data explicitly. This document grants no real-patient data collection approval; default to public content and minimized aggregate measures. Patient-specific functionality remains future scope. |

Use the [existing UX research approach](ui-ux-blueprint.md) for interviews, moderated tasks and inclusive recruitment. Do not collect immigration status, diagnoses or health histories to establish inclusion. Participant accommodations and optional language/accessibility feedback need consent and minimization even when medication tasks are synthetic.

## Proposed measures (REQ-012; planned T-010)

| Evaluation area | Proposed method / baseline | Interpretation and safeguard |
|---|---|---|
| Patient comprehension | Qualified reviewers define equivalent synthetic questions about essential information, safety content and source identity; compare task results and misunderstandings against the agreed existing-information format | Score understanding, not treatment adherence or health outcomes. Agree rubric and safety-error severity before sessions; stop or revise unsafe wording. |
| Language accessibility | Task completion, language selection, comprehension and fallback observations by evaluated language, including RTL | Fluent/medical reviewers check equivalence; separate translation defects from navigation issues. Report language coverage and missing evidence. |
| Older adults and disability access | Observe participants' usual tools and needs; keyboard/screen reader, text enlargement, small screens, focus/reading order and mixed-direction checks | Provide accommodations and paper alternatives where needed. Do not assume age predicts ability; small groups and overlap limit comparisons. |
| Pharmacy workflow time | Time the same synthetic find-information-to-public-QR task against an agreed current workflow; record selection errors, retries, assistance and interruptions | Include onboarding, verification of the selected product and rework. Faster completion is not success if product selection or information safety worsens. No patient identities in timing records. |
| Avoidable paper use | Agree the eligible informational handout category first; count baseline and intervention sheets per equivalent eligible encounter/task, including reprints, fallback copies and any QR carrier sheets | Paper reduction = baseline sheets per eligible encounter minus intervention sheets per eligible encounter; report counts, denominators, period and uncertainty. Do not count legally required or patient-needed print as avoidable. No savings inferred merely from scans. |
| Content accuracy and version integrity | Qualified medical/language review plus synthetic source-change, translation-lag, withdrawal and correction scenarios; trace each displayed revision and source | Separate medical review findings from software checks. Version integrity must not imply clinical correctness. Trace T-004 and HUM-003. |
| Satisfaction and adoption barriers | Short optional feedback and interviews on trust, ease, language, phone/connectivity access, paper preference and pharmacist burden | Record refusals/missing feedback where feasible without identifying people; satisfaction alone does not establish comprehension or adoption. |

Use comparable task difficulty, content versions, formats and time windows. Counterbalance task order where practical to reduce learning effects. Document recruitment bias, sample limitations, assistance, incomplete tasks and denominator changes. Set target thresholds and analysis rules before evaluation; do not choose them after seeing outcomes.

## Sustainability interpretation (F-008)

The objective is measurable reduction of avoidable paper where legally permitted while preserving access and safety. Do not withhold print to make the metric improve. REG-002/HUM-007 must establish applicable paper constraints for the workflow before an actual reduction intervention. HUM-012 must define baseline, eligible units, fallback accounting and comparison period with pharmacy input.

Report observed paper use separately from net environmental impact. Do not convert sheets into trees, carbon or cost savings without a justified method and source factors. Digital infrastructure and device use have impacts not quantified by paper counts. No environmental benefit is demonstrated by this plan.

## Governance and reporting

Before any participant activity, agree a protocol, consent/accessibility support, who can access records, retention/deletion, whether recording is necessary, and how findings will be de-identified (HUM-009). Avoid recording by default; do not add analytics or patient tracking merely to populate measures. Use aggregate results and suppress identifiable small-group details.

HUM-012 owns stage-specific go/no-go criteria, baseline/targets, pilot permissions and a safety escalation/stop plan. Proposed stop reasons include wrong-product display, unreviewed or stale safety content, privacy leakage, severe comprehension errors and inaccessible essential information. Qualified stakeholders must agree severity and response before a pilot. Defects use existing BUG/FIX trackers; research findings link to F/REQ/T records without becoming approved features automatically.

Report protocol/version, stage, methods, sample and denominators, languages/configurations, measured results, uncertainty, adverse observations, limitations and decisions. Do not fill missing observations with assumed success. AI-006 will prepare a measurement worksheet/protocol for review; no outcomes or collection tooling exist yet.
