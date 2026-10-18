# Medication content governance

Status: Proposed governance model; sources, reviewers, rights, publication authority and timing require HUM-003. No medication content has been supplied, medically reviewed, or published.

Phase 1B establishes a central reviewed catalogue as the intended operating model (REQ-010). Pharmacists select appropriate existing public information rather than authoring full drug descriptions for each use. Central editorial responsibility is distinct from pharmacy selection and from the pharmacy's clinical record; exact publication permissions remain HUM-003/HUM-005 decisions.

Proposed review states include draft, in review, approved for publication, published, superseded and withdrawn. These are discussion states, not an implemented schema or permission model. Each translation must be tied to its reviewed source revision and its own review outcome. Synthetic demonstrator fixtures must be clearly labelled as nonclinical and must never be presented as medically reviewed real medication guidance. Real publication requires qualified review and confirmed reuse rights; a DIN, terminology entry or PMS match does not supply that approval.

## Intended quality foundations (F-004 / REQ-004)

Distinguish general public medication information from private patient-specific instructions. Preserve source provenance and revision identity; do not represent generated, translated, simplified, or audio material as medically approved without qualified review.

For each proposed public content item, capture the source reference and version/date, applicable product identity, language, local revision, review status, reviewer responsibility, and relationship to translations or alternate formats. Exact metadata and retention policy remain design questions, not a database schema.

## Proposed editorial lifecycle

1. Confirm authoritative source suitability and reuse/translation rights.
2. Prepare content with source linkage and explicit draft status.
3. Obtain qualified medical review; obtain medical and language review of translations and simplified/audio variants.
4. Authorize publication with traceable review and revision context.
5. Monitor source changes and reports; assess corrections, withdrawal, and dependent language/format updates.

This is a discussion workflow, not an approved staffing or service commitment. Agree how stale translations, conflicting sources, urgent safety changes, unavailable reviewers, and publication rollback are handled before publication. Do not silently treat an older translation as equivalent to a changed source.

## Research questions

- Which sources are appropriate for Manitoba users, and what licensing conditions apply? (REG-004)
- Who is qualified and authorized to review each content type and language? (HUM-003)
- How will public safety information be prioritized without omitting essential context?
- What review cadence, correction route, and ownership are feasible? No cadence is assumed.
- How can patients and pharmacists report suspected inaccurate medication information, and who triages reports and links them to affected source versions and translations? This research question belongs to [HUM-003](../tasks/HUMAN-TODO.md) and planned [T-004](testing-strategy.md) coverage. It does not approve a reporting feature for the MVP.
- How will simplified and audio versions preserve meaning and pronunciation?

T-004 will validate synthetic provenance, review, publication, translation linkage, and correction scenarios after workflow approval. Clinical accuracy requires qualified reviewers; software tests do not establish medical correctness.
