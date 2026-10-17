# Privacy and security overview

Status: Existing security/privacy principles remain mandatory; technical enforcement is unimplemented. This document is a navigation and discovery summary, not a replacement for detailed guidance.

## Authoritative existing guidance

- [Privacy model](privacy/privacy-model.md), [data minimization](privacy/data-minimization.md), [classification](privacy/data-classification.md).
- [Authentication](security/authentication.md), [authorization](security/authorization.md), [QR security](security/qr-security.md), [initial threat model](security/threat-model.md).
- [ADR-003](decisions/ADR-003-qr-token-strategy.md): accepted opaque patient QR security principle; lifecycle details pending.

Public information and patient-specific information must have distinct data and API paths. No private fields may reach the browser before successful verification and server authorization. Protected staff operations require verified tenant membership and least privilege. Do not put sensitive information into QR payloads, logs, errors, analytics, or support materials.

F-006 is future scope, not an approved MVP requirement. Its eventual introduction cannot waive verification, opaque high-entropy tokens, expiry/revocation, tenant isolation, audit, or minimization principles. Verification method and token lifecycle need HUM-004; identity needs HUM-005; persistence isolation needs HUM-006.

## Discovery and research

Use synthetic patient data only. Minimize research participant information and decide consent, access, and retention arrangements before collecting it (HUM-009). Language preference or usability feedback should not become a reason to collect health histories.

Legal applicability, organizational responsibilities, notices, access/correction, retention/deletion, and breach workflows need qualified research (HUM-007; [research register](regulatory-research.md)). Hosting, recovery and security operations need HUM-008. No compliance or legal conclusion is established.

Planned security coverage is T-005/T-006; revisit the threat model once approved workflows and deployment are concrete (AI-002).
