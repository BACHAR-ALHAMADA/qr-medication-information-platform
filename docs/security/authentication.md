# Authentication

## Staff

Staff authentication is planned to use an established OpenID Connect / OAuth 2.0 identity provider. The application should validate issued tokens using supported libraries and provider metadata, map identities to application memberships, and avoid storing or handling passwords unless a later documented decision requires it.

Authentication establishes identity; it does not by itself grant access to a tenant or patient record. Authorization must be checked separately on each protected operation.

## Patient verification

Patient-specific functionality is future scope until the Product Owner explicitly approves MVP scope (F-006; HUM-001). Whenever introduced, scanning a patient-specific QR code must be followed by a separate verification step before private access. A flow without a full patient account remains a proposal, not an approved MVP requirement. The factor, delivery method, retry/expiry policy, and assurance level are **Decision Required** (HUM-004). Avoid treating guessable personal facts alone as strong authentication. A successful challenge should grant only short-lived, narrow access to the intended assignment. See the [feature catalogue](../feature-catalogue.md) and [Human TODO](../../tasks/HUMAN-TODO.md). This scope clarification preserves ADR-003 and its mandatory security principle.

## Operational requirements

Use HTTPS, secure secret management, rate limits, generic responses that limit enumeration, and redacted logging. Never log credentials, verification factors, bearer tokens, or sensitive request bodies.

## TODO

- Select identity provider and define staff account lifecycle, MFA, recovery, and session policy.
- Select patient verification method based on usability, threat model, and privacy review.
