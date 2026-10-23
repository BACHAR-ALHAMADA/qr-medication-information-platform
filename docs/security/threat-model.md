# Initial threat model

This is a starting point for design discussion, not a completed security assessment. Revisit it as workflows, data flows, and deployment choices become concrete.

## Assets

- Private patient medication assignments and instructions.
- Staff identities, credentials, permissions, and tenant membership.
- QR access tokens and verification challenges.
- Public medication content and its provenance.
- Audit records, application secrets, and service configuration.

## Trust boundaries

- Patient/staff browsers to the service over HTTPS.
- Public catalogue reads versus private assignment reads.
- Tenant-to-tenant data boundaries.
- API/application layer to database and external identity/verification providers.
- QR bearer token possession versus verified patient access.

## Initial threats and controls

| Threat | Initial design control |
|---|---|
| Guessing or sharing a patient QR link | High-entropy opaque token, rate limiting, revocation/expiry, separate verification, no raw token logging |
| Private data exposed in public response or browser | Separate endpoints/read models; do not fetch private fields until server-side authorization succeeds |
| Cross-tenant access | Tenant context derived from authenticated membership; scope every operation; test isolation |
| Staff account or privilege misuse | OIDC/OAuth 2.0, least privilege, server-side role/permission checks, audit sensitive actions |
| Injection or malformed input | Boundary validation, parameterized ORM operations, safe output handling |
| Sensitive data leaked through logs/errors | Structured redaction, no payload/token logging, generic external errors |
| Verification abuse or enumeration | Rate limits, short-lived challenges, non-enumerating responses, monitoring |
| Dependency or secret compromise | Minimize dependencies, update them, keep secrets out of source control, rotate through managed processes |

## TODO

- Conduct a structured threat-modeling session after workflows and deployment are specified.
- Define abuse cases, security ownership, incident response, backup access, and security monitoring expectations.
- Research applicable legal and regulatory obligations with qualified stakeholders; none are asserted here.

## Phase 2C identity/tenant design supplement

The [threats and planned acceptance tests](../phase2c-identity-tenancy-design.md#threats-and-planned-acceptance-tests) extend this initial model for forged tenant selectors, object-reference substitution, branch bypass, multi-membership role union, stale contexts, RLS/pool contamination, invitation replay, privilege escalation, revocation races and audit leakage. T-013 now has 61 passing synthetic authorization cases; provider, invitation, database/pool, audit and transactional-race scenarios remain planned. See the [implemented subset](../phase2c-identity-tenancy-design.md#implemented-synthetic-foundation). Identity, isolation and applicable operations/privacy choices remain unresolved.
