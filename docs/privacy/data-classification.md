# Data classification

Initial classification for design; validate categories and handling with privacy/security stakeholders before implementation.

| Class | Examples | Intended handling |
|---|---|---|
| Public | Approved general medication descriptions, warnings, official document references | May be served without authentication; provenance and revision should be clear |
| Internal | Non-sensitive operational configuration and module metadata | Restrict to staff/services that need it |
| Personal | Patient contact or identity data needed for assignment/verification | Minimize, restrict, protect in transit/at rest, and define retention |
| Sensitive health | Patient-medication relationship, dose, frequency, duration, personal instructions, verification outcomes | Strong access controls, tenant scoping, audit, minimal exposure, no sensitive logging |
| Secrets | Credentials, signing keys, QR tokens, verification factors | Dedicated secret handling; never commit or log |

Classification does not determine legal status. TODO: review terminology and obligations with qualified privacy and legal stakeholders before production.
