# ADR-007: hybrid authentication and application-owned identity mapping

- Status: Accepted for the hybrid direction and bounded synthetic mapping foundation. Production authentication, runtime bootstrap and identity mutation workflows remain separately gated.
- Date: 2026-11-11 (America/Winnipeg)
- Authority: Product Owner Phase 2E-2B direction and Phase 2E-2C explicit schema/synthetic implementation approval
- Trace: ADR-001/ADR-002/ADR-004/ADR-005/ADR-006, F-002, REQ-006/REQ-007/REQ-008/REQ-009, HUM-005/HUM-006/HUM-007/HUM-008, AI-002/AI-010, planned T-017

## Approved direction

Small pharmacies use managed local identities. Larger pharmacy organizations may later federate Microsoft Entra or another enterprise OIDC/SAML provider. Auth0 is preferred for evaluation only, not selected for integration or production. Internal staff identity, organization membership, branch grants and capabilities remain application-owned. Provider organizations, email domains and provider administration do not replace application authorization.

Managed local identities means credentials managed by the eventual identity service, not a new application password database. This interpretation is the proposed implementation direction; the credential methods and provider plan still require approval. Hybrid support is a direction, not permission to add a provider SDK, credentials, login or endpoint.

## Accepted mapping decision

Identity owns an explicit mapping from the exact validated issuer/subject pair to exactly one existing internal staff user. Multiple independently proved external identities may refer to that same internal user. An external principal may never refer to two users, including after disablement/unlinking. No matching or automatic linking by email, client GUID, organization ID, domain, branding, names or unvalidated claims. Unknown identity means denial, not just-in-time staff creation.

Use exact configured issuer strings without normalization at lookup; preserve subject strings without trimming, case-folding or Unicode normalization. Trusted protocol validation must first match an enabled, explicitly approved issuer/client configuration. A syntactically valid HTTPS URL or a database registry row is not protocol validation. For brokered login, the key is the issuer/subject asserted by the validated broker token, not a client-supplied upstream identity or an unqualified SAML NameID. Direct SAML-to-application mapping is outside the proposed first slice.

Two Identity tables are approved: an enabled issuer registry and immutable-owner external staff mappings. Exact uniqueness, restrictive same-module FKs, bounded strings, lifecycle tombstones and revision guards prevent ambiguous mappings and ordinary key reassignment. Phase 2E-2C approves one forward-only EF migration, initially applied only to a fresh disposable PostgreSQL database, and an exact resolver under an explicitly privileged synthetic harness. No runtime grants or policies are approved. See [schema and invariants](../phase2e-2b-identity-mapping-design.md#proposed-minimum-schema) and [implementation evidence](../phase2e-2c-identity-mapping-foundation.md).

Linking is a separate sensitive operation: fresh authentication of both old and new identities, approved assurance, a server-bound single-use ceremony, conflict checks, atomic constraints, revalidation, audit and session invalidation. A pharmacy administrator may not merge a global user across organizations. Provider-side account merging must not silently change application mappings. Unlinking disables a mapping without freeing the tuple for another user and must address recovery/last-identity lockout. Recovery without both proofs is a separately approved manual process, not an email fallback.

## Consequences and gates

Existing self-scoped PostgreSQL `qr_reader` cannot bootstrap an unknown internal actor. Do not broaden it or assign a guessed actor to satisfy RLS. Approved Phase 2E-2C is a persistence-only synthetic slice: new mapping tables and an exact resolver tested with explicitly privileged disposable setup connections; existing runtime credentials retain no mapping access. Least-privileged runtime bootstrap requires its own reviewed mechanism before live integration.

Enterprise authentication provenance can restrict where an identity may authenticate; it never grants pharmacy membership. Shared broker issuer/subject alone does not identify which upstream connection authenticated a session. Before runtime hybrid use, decide and test connection provenance, tenant-specific assurance, local fallback restrictions and cross-pharmacy reach. Phase 2E-2A's key-only evidence cannot enforce these policies today. No current StaffAuthorization rule is changed by this proposal.

Accepted ADR-001 through ADR-006 are preserved. No additional cross-module FK or direct Organizations table access is proposed. Mapping revisions are distinct from organization membership authorization revisions. No immediate revocation of existing Repeatable Read snapshots is claimed.

## Alternatives considered

- Provider profile/organization as the application user/tenant: rejected as the default design because it couples provider lifecycle to internal identity and would bypass or duplicate approved pharmacy authorization.
- Automatic email/domain linking: rejected because mailbox/domain control and account reuse do not prove ownership of an existing internal employee.
- Application password database: not proposed; it adds credential lifecycle responsibility outside the approved managed-identity direction.
- One internal user per external identity: simpler isolation but would split a person's memberships and make later linking a migration; not recommended for the stated multi-identity employee goal.
- Full runtime authentication plus mapping now: deferred pending provider, assurance, mapping access, lifecycle/audit and storage approval.

## Decisions required

The schema/limits/tombstone/revision rules and disposable synthetic migration/resolver scope are approved and implemented locally for review. Runtime bootstrap access, linking/recovery authority, assurance and revocation targets, enterprise provenance/scope policy, data retention and provider evaluation criteria remain Decision Required. The [design record](../phase2e-2b-identity-mapping-design.md) preserves the threat model and future provisioning boundary; [provider comparison](../phase2e-2b-provider-comparison.md) preserves evaluation evidence and cost caveats. No provider integration, production authentication or live mutation workflow is authorized.
