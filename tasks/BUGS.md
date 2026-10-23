# Bugs

No open bugs are currently recorded. Documentation scope ambiguity BUG-001 was corrected; see [FIX-001](FIXES.md). Phase 2A source-metadata implementation and test verification status are recorded in [backend documentation](../docs/phase2a-backend.md); an environment/package approval block is not an application bug.

| ID | Finding | Status / trace |
|---|---|---|
| BUG-001 | Authentication guidance stated patient verification was an MVP requirement, and minimization guidance assumed private-data MVP scope, while the approved discovery direction keeps patient-specific functionality future until explicit scope approval. | Corrected by FIX-001 on 2026-10-17; F-006, HUM-001. Mandatory verification/security principles were not defective and remain unchanged. |
| BUG-002 | During Phase 2B review, the loading live region was inside an aria-busy results region, which could defer screen-reader announcements until loading ended. | Corrected by FIX-002; F-003, REQ-003, T-012. Busy state now applies to the form; loading announcements remain outside it. |
| BUG-003 | Initial Phase 2B DIN placeholder color had calculated contrast 4.36:1 against white, below the 4.5:1 normal-text design check. | Corrected by FIX-003; F-003, REQ-003, T-012. It now uses the darker muted-text token, approximately 5.58:1. |
| BUG-004 | Phase 2B documentation retained outdated current-Node, T-011 and Phase 2A approval wording, and its file inventory incorrectly described the combined working tree. | Corrected by FIX-004 on 2026-10-22; T-011/T-012, HUM-013. Historical evidence is retained; human completion is not assumed. |
