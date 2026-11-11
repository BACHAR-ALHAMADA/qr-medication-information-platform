# Authentication

2026-11-11 approved direction: hybrid managed local identities and later enterprise federation, with application-owned identity/authorization; Auth0 preferred for evaluation only. [Accepted ADR-007](../decisions/ADR-007-hybrid-authentication-identity-mapping.md) prohibits automatic email/client-GUID/domain linking. [Phase 2E-2C](../phase2e-2c-identity-mapping-foundation.md) persists exact mappings only on fresh disposable synthetic PostgreSQL with privileged test-only resolution. Database rows do not authenticate anyone; no real token validation, provider, login, runtime bootstrap or API registration exists. Provenance/assurance, linking/recovery, session revocation and least-privileged runtime lookup remain separate gates. Earlier proposals below are historical where superseded.

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

## Phase 2C proposal for review

The [identity design](../phase2c-identity-tenancy-design.md#authentication-recommendation-and-options) recommends OIDC code + PKCE through the ASP.NET backend, with an SPA alternative, provider operating options and proposed MFA/session/recovery policies. All choices await HUM-005/HUM-008 and applicable HUM-007 review. No provider, credentials or login implementation is introduced.
