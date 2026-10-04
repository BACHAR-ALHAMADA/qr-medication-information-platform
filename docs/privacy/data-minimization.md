# Data minimization

Collect and retain only data needed for a documented workflow. Avoid requiring a patient account for the MVP if a safe, narrow verification flow can satisfy the use case. Do not include personal or medication data in QR codes, URLs beyond an opaque bearer token, analytics, or routine logs.

For each proposed field, document its purpose, source, access roles, retention/deletion need, and whether a less identifying alternative works. Keep public product content separate from patient assignments. Limit verification attempts and store only the challenge state and outcome needed for security and operations.

## Decision Required

- Patient matching and verification data required for MVP.
- Retention and deletion periods for patient, assignment, audit, and verification records.
- Analytics and support diagnostics that can be collected without sensitive data.

TODO: validate minimization choices with privacy stakeholders and research applicable obligations; no specific legal requirement is asserted here.
