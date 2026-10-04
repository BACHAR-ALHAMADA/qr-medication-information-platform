# Privacy model

Privacy by design is a project requirement. The planned experience separates public medication information from patient-specific instructions, uses an opaque QR reference, and discloses private data only after verification and server-side authorization.

## Design commitments

- Minimize collection and retention; define a purpose for every personal or health-related field.
- Keep public catalogue content separate from private patient assignments in models, endpoints, and authorization paths.
- Limit patient access to the specific assignment being verified; limit staff access by role and tenant membership.
- Avoid sensitive content in QR codes, logs, analytics, URLs, error messages, and browser responses before authorization.
- Maintain auditable access to sensitive records while minimizing audit payloads.

## Open questions

- Decision Required: data controller/processor and organizational responsibilities for each deployment model.
- Decision Required: consent/notice, correction, access, retention, deletion, and breach workflows.
- TODO: qualified review of applicable Manitoba, Canadian, and other relevant legal/regulatory requirements before pilot or production. This document does not state a legal conclusion.
