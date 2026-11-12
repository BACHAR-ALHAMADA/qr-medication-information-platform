# Phase 2E-2B: identity mapping design

Phase 2E-2C approval update: ADR-007, the two-table schema/integrity rules and privileged synthetic migration/resolver on fresh disposable PostgreSQL are now approved. [Implementation evidence](phase2e-2c-identity-mapping-foundation.md) supersedes approval-pending and proposed/unimplemented C statements below. This document preserves the original design checkpoint; remaining provider/runtime/workflow gates are unchanged. PR #5 is now merged at b517c4c.

Date: 2026-11-11 (America/Winnipeg). Status: design only, architecture review gate. Approved direction and proposed technical details are separated in [draft ADR-007](decisions/ADR-007-hybrid-authentication-identity-mapping.md). Trace: F-002, REQ-006/REQ-007/REQ-008/REQ-009, HUM-005/HUM-006/HUM-007/HUM-008, AI-002/AI-010 and planned T-017. No migration, provider integration, permission change, invitation or linking implementation is authorized.

## Historical Phase 2E-2B baseline and PR #5 readiness

Branch remains `feature/trusted-actor-boundary`, HEAD `04ac9509fe097b830cb033f5237762fe9cef8b7f`; the only initial untracked file is `docs/NEXT-SESSION.md`. Read-only remote checks confirm latest main is still `c5bbad285812c8f981a67e9ae2b36ace1e715adf` and the feature remote matches HEAD. No fetch or branch switch was necessary. Credentials, handoff and recovery-file hashes match the earlier checkpoint; safety stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1` remains preserved. The historical planning-backup directory remains previously known empty/unverified.

[PR #5](https://github.com/BACHAR-ALHAMADA/qr-medication-information-platform/pull/5) targets main, remains Draft/open/unmerged, reports MERGEABLE/CLEAN and has no submitted reviews or review threads. Backend, PostgreSQL integration and Frontend are successful on the expected head. [Hosted run 43115544447](https://github.com/BACHAR-ALHAMADA/qr-medication-information-platform/actions/runs/43115544447) has verified counts of 160 backend, 110 PostgreSQL and 28 frontend cases. These are existing hosted results, not tests rerun during this documentation task.

Review recommendation: GO for merging the synthetic-contract slice after an explicit owner merge decision. No new blocking defect was identified. It has no actual authentication adapter or persistent mapping. Its internal constructors/test-friend access provide composition safeguards, not protection against hostile in-process callers. No API registration exists. This design's future production gates do not turn the deliberately synthetic PR into production authentication.

## Approved direction, assumptions and exclusions

**Approved:** hybrid managed local identities for small pharmacies, later enterprise OIDC/SAML federation, Auth0 preferred for evaluation only, application-owned staff identity and pharmacy authorization.

**Proposed interpretation:** managed local identities live in the eventual managed provider's credential store. The application stores no password hashes, MFA seeds, recovery codes or provider signing/client secrets in mapping tables. Federation preferably terminates at that broker, with OIDC toward the application. Direct SAML support would need separate contracts and validation design.

**Still pending:** provider/plan/region, whether a BFF is adopted, approved assurance/recovery policy, exact mapping schema and migration, runtime bootstrap role/mechanism, invitation/link authority, and operational revocation targets. No real accounts or pharmacy onboarding. Recipient eligibility and delegation remain separate gates.

## Proposed minimum schema

The current `identity.staff_users` has only nonzero `user_id` and `is_enabled`; it remains the authoritative internal user. The following are proposed new Identity-owned tables, not existing schema or executable DDL. Proposed byte limits require owner review and provider examples before approval.

| Table / column | Proposed storage and invariant |
|---|---|
| `identity.authentication_issuers.issuer_id` | UUID PK, nonzero, server-generated opaque reference |
| `issuer` | `text COLLATE "C"`, NOT NULL, UNIQUE across every lifecycle state, 1–1024 UTF-8 bytes; exact configured HTTPS issuer; no whitespace/control/userinfo/query/fragment |
| `is_enabled` | boolean NOT NULL, default false; enabling database row alone cannot expand the trusted protocol/configuration allowlist |
| `revision` | bigint NOT NULL, initially 1, positive; database guard calculates increments on updates, rejects caller rewrites and overflow |
| `created_at`, `updated_at` | timestamptz NOT NULL, database-owned timestamps; no login telemetry or IP fields |
| `identity.external_staff_identities.mapping_id` | UUID PK, nonzero, server-generated |
| `issuer_id` | NOT NULL FK to Identity issuer registry, ON DELETE/UPDATE RESTRICT |
| `subject` | `text COLLATE "C"`, NOT NULL, opaque exact string, 1–1024 UTF-8 bytes, not whitespace-only and no controls; no case folding, trimming or Unicode normalization |
| `user_id` | nonzero NOT NULL FK to existing `identity.staff_users`, ON DELETE/UPDATE RESTRICT |
| `state` | controlled text: `active`, `disabled`, `unlinked`; default `disabled`; unknown/NULL forbidden |
| `revision` | bigint NOT NULL, initially 1, positive; same proposed database-owned guard semantics |
| `created_at`, `updated_at` | timestamptz NOT NULL, database-owned; mutation audit is a separate workflow obligation |

UNIQUE `(issuer_id, subject)` applies to active, disabled and unlinked rows together. Never use an active-only unique index. Keep an ordinary `user_id` index for authorized identity lifecycle operations. Do not make `user_id` unique: two proved identities may point to one staff user. Since registry issuer is globally unique, the composite enforces exact issuer/subject uniqueness. Proposed bounded index values avoid unbounded text keys; validate maximum-byte behavior against the approved PostgreSQL build, including multi-byte strings and the deterministic `C` collation. Application validation is authoritative for URI shape; DB checks enforce bounded/nonempty/control-safe values, not cryptographic URL/issuer validation.

Proposed invoker-rights guards on the new tables freeze issuer text/ID, mapping tuple/ID and mapping `user_id`, and reject ordinary DELETE/TRUNCATE. They preserve tombstones and prevent ordinary disable/delete/reinsert reassignment. Explicit state transitions are active -> disabled/unlinked, disabled -> active/unlinked, unlinked terminal. Reactivation of disabled rows needs new proof/approval; never silently revive during login. Revisions increment conservatively even on no-op updates and roll back atomically; overflow fails. Issuer disablement has its own revision and does not require a fan-out update to every mapping. No SECURITY DEFINER revision function or change to existing membership-maintainer authority is needed for these proposed row guards. Privileged schema administrators can bypass guards; this is not administrator-compromise protection.

Physical erasure/retention, tombstone-key retention and exceptional identity correction remain HUM-007/HUM-008 decisions. The proposal does not invent a legal retention obligation or promise indefinite retention. A separately authorized purge must address principal-reuse risk before deleting uniqueness history. No mapping reassignment/account-merge command belongs in the minimum slice.

## Issuer and subject policy

1. Trusted configuration explicitly allowlists the issuer, client/audience, metadata/key sources and protocol. Only controlled configuration may trigger metadata/key retrieval; never discover an arbitrary issuer from a request or DB row. This prevents URL-valid-but-untrusted issuers and discovery/JWKS SSRF.
2. Compare token issuer and configured issuer exactly. Canonical spelling is chosen at enrollment; do not lowercase, strip paths/slashes/ports, URL-decode or silently alias during lookup. Existing `ExternalPrincipalKey` equality follows this exact-string direction; it is only a shape check and does not enforce an issuer allowlist or token validation.
3. Treat subject as opaque and exact, scoped to its issuer. Preserve case, whitespace within a valid nonblank subject and Unicode bytes; do not normalize them into another identity. A provider-specific adapter may impose its protocol's stricter subject rules. Proposed storage bounds may require a future explicit contract-validation change; do not truncate or hash to hide collisions.
4. For brokered enterprise login, persist the validated broker's issuer/subject. An upstream Entra tenant ID, email, SAML NameID, organization/domain claim or a browser-selected connection is not the key. Separate connection/provenance policy is still required below.
5. Issuer/domain/custom-domain/client/sector or broker-primary-identity changes may change `iss` or pairwise `sub`. Treat them as new identities requiring controlled migration or linking, not aliases. If a provider reuses a deleted subject, the tuple alone cannot distinguish the new person: retain tombstones, prohibit automatic rebind and require provider lifecycle/recovery review.

The stable OIDC identity basis is the issuer/subject pair; other profile claims cannot replace it ([OIDC Core](https://openid.net/specs/openid-connect-core-1_0.html#ClaimStability)).

## Resolver and the bootstrap boundary

An Identity resolver accepts only `ValidatedAuthentication` obtained through trusted source composition. In one read-only snapshot, perform a parameterized exact join of issuer -> external mapping -> staff user. Read only Identity-owned columns. Require trusted-config issuer enabled, registry enabled, mapping active, unique result and staff enabled/nonzero. Unknown or ineligible principals return a generic denial through the existing contract; duplicate/contradictory results must never select the first row. Cancellation/errors propagate, not fallback identities. Return no unrelated mappings, profiles or credentials. Existing actor creation and StaffAuthorization checks remain separate steps.

**Bootstrap conflict:** `qr_reader` staff RLS requires an internal actor plus organization/branch settings, which are unavailable before external mapping resolution. It cannot implement this resolver as-is. Never guess an actor, put an external subject into `app.actor_id`, borrow owner/setup credentials for runtime, broaden current reader SELECT, or add an unrestricted list/search API.

For approved C, use only the privileged disposable test administrator in a caller-owned read-only Repeatable Read synthetic session (implemented by SyntheticIdentityMappingSession). New tables ENABLE/FORCE RLS with no runtime policies/grants, owned through existing offline migration composition. Tests assert current reader credentials cannot access them. Privileged results establish mapping correctness only, not runtime least privilege or production identity isolation.

Before a runtime resolver, separately choose/review a pre-actor access mechanism. Candidate: a dedicated bootstrap connection allowed only to execute a fixed exact-key lookup returning minimum staff identity, with reviewed function ownership/RLS/search_path, restricted execution, auditing/rate limits and no table-list privilege. This would require new reviewed grants/policies/possibly a narrowly privileged function and is NOT approved here. A dedicated global lookup component remains trusted: exact-key SQL cannot itself prove that a caller authenticated that identity. No existing role is changed by this design task or assumed to receive new access.

## Linking a second identity safely — future workflow only

Application linking adds a second immutable mapping to the same internal user; it need not merge provider profiles. Disable provider-side automatic email linking. If broker linking/primary-account changes are later necessary, coordinate them as a distinct migration because the emitted subject may change. Auth0 itself warns that both accounts need authentication before linking ([linking precautions](https://auth0.com/docs/manage-users/user-accounts/user-account-linking)).

Proposed ceremony:

1. Resolve and freshly reauthenticate the existing identity to the enabled internal employee, with approved step-up assurance. The target internal user comes from that trusted context, never a client GUID or search by email.
2. Create a short-lived, single-use, high-entropy server-bound link intent tied to that user, initiating session, intended new authentication route and operation. Store only a verifier where practical. Bind callback state/nonce/PKCE as appropriate; exact TTL is Decision Required. The link intent is not independent authentication evidence.
3. Independently authenticate the new identity through an approved route with fresh proof. A silent SSO result or email verification alone is not a replacement for the chosen freshness/assurance policy. Confirm employee intent using non-enumerating UI; do not expose other users' mappings.
4. Under serialized mutation/concurrency control, recheck both identities, issuer status, internal enabled state, assurance and unused intent. Lock the internal user and relevant mapping/intent records in deterministic order, and rely on exact unique constraints to close concurrent-link races. An already-mapped tuple belonging to a different internal user is a conflict, not reassignment; same-user retry is idempotent only after the same authorization checks.
5. Record the new mapping plus minimum audit event and intent consumption atomically. Revoke/rotate affected application sessions and notify through an approved independent channel. Do not transfer memberships, grants, provider metadata or tokens. Audit ownership, failure policy and notification retention are Decision Required before mutation implementation.

No pharmacy administrator may link an identity to an existing global internal user based only on employment records, matching email/domain or authority over one tenant. No linking path may absorb another internal account's memberships. Recovery without control of both identities requires separately approved manual identity proofing, independent review, audit and session invalidation; it is not an ordinary link.

Unlinking is a sensitive state transition to `unlinked`, not DELETE or tuple release. Require fresh approved proof using a surviving eligible identity; prevent last-usable-identity lockout unless an approved disable/recovery process is intended. Serialize link/unlink/disable to prevent concurrent last-identity removal. Disable a compromised identity immediately through an authorized lifecycle process even if that suspends access; do not preserve access merely to avoid lockout. Global staff disablement blocks every mapping; tenant membership suspension affects that tenant through Organizations. These are distinct lifecycle controls.

## Provisioning and invitations — proposed boundary, not implementation

Unknown external login does not create staff or memberships. Separate provider credential enrollment, Identity staff/mapping provisioning and Organizations membership/grant provisioning through explicit Application contracts. Identity never writes Organizations tables directly. Creation of a mapping to an existing employee follows the linking/recovery rules above; provider account creation or mail delivery alone is insufficient.

An authorized inviter's current tenant membership and narrow invitation capability must be checked server-side; no such production capability or workflow exists today. A future invitation records server-selected target organization, allowed enrollment route, intended account category, minimal proposed access and an opaque single-use verifier with expiry/revocation. Email is delivery/contact data only. Invite possession never proves an existing global account belongs to the redeemer and does not bypass fresh identity validation, inviter/tenant checks or mapping conflict checks. Existing-account redemption requires the trusted existing user plus appropriate proof; new-employee enrollment must be explicitly approved and must not resolve by matching email.

Redemption rechecks inviter authority, organization state, route policy, intended binding, expiry/revocation/consumption and recipient eligibility in a coordinated transaction. Begin with no authority until the Identity and Organizations provisioning contract succeeds; provider profile creation is an external side effect, not part of the DB transaction. On partial failure, leave access disabled/pending and reconcile explicitly; do not retry into automatic linking. Rate-limit delivery/redemption and avoid enumeration/secret-bearing URL logs. No invitation table, provider call, capability or audit store is part of C.

## Assurance, enterprise scope and revocation

Actual token/session validation precedes `ValidatedAuthentication`: approved issuer/client, signature/algorithm/key trust, audience, lifetime and flow-specific state/nonce/PKCE/replay protections through maintained libraries. An ID token must not be treated as a generic API access token. SAML should initially be brokered; direct SAML requires signed assertion/response, audience/recipient/destination, correlation, replay and stable subject rules in a separately reviewed adapter. None exists today.

MFA/step-up is operation-specific. A provider's `amr`/`acr`/`auth_time` is meaningful only after validation and an agreed semantic mapping, including whether upstream enterprise MFA is trusted. Email OTP is not automatically an approved second factor. Define fresh proof, idle/absolute session limits, refresh/replay handling, recovery and emergency access before login/linking. App actor creation requires validation + eligible exact mapping + enabled staff; it is not a grant.

**Cross-pharmacy risk:** one internal employee can have independent memberships in A and B. After linking an enterprise identity administered by A, compromise or administrative reset in A must not silently make that route eligible for B. Recommend an application-controlled intersection of current memberships with permitted authentication routes/assurance for the candidate organization. Shared broker issuer/subject may hide the upstream connection; preserve and validate route provenance before granting session reach. Organization/domain claims can be routing hints or restrictive checked evidence, never authentication or automatic membership grants. Local fallback must not bypass a tenant's enterprise/assurance policy. This route policy and its future storage/contracts need owner approval; current key-only validation evidence cannot enforce it. No existing authorization semantics are changed now.

Local mapping/issuer disablement or unlink must deny new resolutions. Existing provider/application sessions and already-running snapshots may survive unless separately invalidated/rechecked. Mapping and issuer revisions are proposed evidence for a future session/revalidation design, not a complete logout mechanism; do not reuse membership revision as an authentication epoch. A future global staff security epoch/session registry may be needed for logout-all and recovery; it is outside the two-table minimum and must be approved before such claims. Provider logout/revocation events need trusted delivery and measurable reconciliation bounds; never claim immediate deprovisioning based on a webhook alone. Sensitive writes need current transactional checks; ADR-006's Repeatable Read and writable-context threat limits remain.

## Threat model and safeguards

| Threat | Proposed control and residual limitation |
|---|---|
| Forged user GUID/email/domain or trusted-looking ClaimsPrincipal | No accepted caller evidence path; only trusted adapter composition. Hostile in-process code/dependencies remain out of this contract's protection. |
| Rogue issuer, metadata/JWKS SSRF, issuer/subject confusion | Controlled config allowlist, exact tuple, no arbitrary discovery or fallback. Compromised approved provider remains a trust risk. |
| Email recycling or verified-email automatic merge | No email linkage or profile-driven rebind; both-proof linking and retained tuples. Mailbox ownership alone is insufficient. |
| Concurrent duplicate links, tuple transfer, disable/recreate | Whole-history unique key, immutable ownership, tombstones, revisions and transactional intent consumption. Privileged DBA compromise remains outside guards. |
| Link CSRF/login CSRF, token replay or stale proof | Session-bound single-use ceremony plus protocol protections and reauthentication; exact policy not yet implemented. |
| Enterprise admin in A reaches B through linked global user | Explicit route/assurance restrictions intersect application memberships; no provider organization/domain claim grants access. Runtime hybrid use blocked until this decision is implemented/tested. |
| Provider primary/issuer/domain change or subject reuse | Explicit migration/new-proof workflow; no alias or mapping overwrite. Provider guarantees must be evaluated. |
| Removed staff remains signed in | Mapping/staff checks, planned session invalidation and measured reconciliation; old snapshots are not immediately revoked. |
| Bootstrap enumeration or credential misuse | No public lookup, separate exact-key runtime mechanism review, generic denials, parameterization and minimized output; privileged test credentials never become runtime. |
| Sensitive telemetry/retention | No raw subjects, tokens, invites, credentials or profile payloads in logs; opaque references and approved audit retention/access. This is a design principle, not legal certification. |

## Planned T-017 real PostgreSQL matrix

Planned only: no new executable tests or migrations exist from this design task. Extend unchanged 160 backend, 110 persistence and 28 frontend baselines; determine actual added case count after implementation.

| Group | Required evidence for proposed C |
|---|---|
| Fresh migration/model | New two tables/indexes/FKs/guards, no pending model changes, old migration IDs unchanged, explicit fresh disposable bootstrap. No startup migration. |
| Exact identity | Same subject/different issuers and case/slash/Unicode distinctions stay distinct; one active exact mapping resolves the expected enabled user; two distinct mappings may resolve the same user. |
| Integrity | Duplicate tuple rejected in every state; concurrent duplicate inserts cannot produce two owners; missing staff/issuer and zero IDs rejected; ownership/key rewrites, DELETE/TRUNCATE and overflow fail. |
| Bounds/collation | Byte limits and multi-byte boundaries tested on actual PostgreSQL; no implicit truncation/normalization; invalid/blank/control values rejected. |
| Lifecycle | Disabled issuer, disabled/unlinked mapping, disabled staff and unknown/missing state deny; reactivation rules and terminal unlink enforced; revision/timestamp updates and rollback correct. |
| Resolver | Parameterized exact Identity-only query, null/ambiguous/mismatched/failure/cancellation handling; no user/profile listings or cross-module data; single read-only snapshot. |
| Security boundary | Existing qr_reader cannot access new mapping tables; no new rights/policies on existing tables; existing reader/RLS tests still pass. Privileged fixture successes labelled setup evidence, not runtime isolation. |
| Transactions/concurrency | Deterministic suspension/revision interleaving: fresh scope denies while old snapshot may retain state; rollback restores rows/revisions; cancellation/error cleanup and physical pool reuse if pooling is used. |
| Regressions/CI | Existing suites and hosted PostgreSQL fresh bootstrap remain green; no silent DB skips. Recovery of nonempty mappings is a separate follow-up before operational use. |

Future adapter/link/invitation tests additionally need invalid signature/issuer/audience/expiry, MFA/freshness failures, missing second proof, replay/CSRF, link conflicts/last-identity races, invitation misuse, enterprise-to-other-pharmacy denial, provider event delay and broker subject changes. Do not count these as C coverage or implemented capabilities.

## Narrow Phase 2E-2C proposal and approvals

1. After explicit PR #5 merge approval, verify merged main/CI and separately authorize a dedicated implementation branch. This design task does not switch branches or merge.
2. Approve this two-table schema, bounded/exact keys, restrictive FKs, lifecycle/tombstone/revision guards and deny-by-default access for new tables. Approve adding one forward EF migration through the existing offline runner/history; no edits to accepted old migrations or existing RLS/roles. Initially apply only in a new disposable synthetic test target, not the preserved development database. Update exact schema-inventory assertions to include the approved new table names, third migration and additional FORCE RLS tables; retain every existing policy/privilege/authorization assertion rather than deleting checks to accommodate the change.
3. Implement Identity persistence and exact resolver behind the existing interface under a trusted synthetic harness using disposable privileged setup connections only. Keep it unregistered in the API, with no runtime credential claim or conversion to the existing synthetic tenant session. Use read-only Repeatable Read and fully materialized output. No real provider dependency is required for this persistence test slice.
4. Implement and run the T-017 C matrix plus regressions. Review generated SQL, privileges and fixture cleanup. Do not create invitations, account links, provider profiles, grants or session middleware.
5. Stop for review. Separately approve runtime bootstrap least privilege, validation-source issuance/provenance/assurance extensions, mapping write/audit process and any provider evaluation environment. If the owner wants least-privileged runtime bootstrap tested in C, expand/approve that security scope explicitly first; do not quietly use qr_reader or broaden its policies.

Owner decisions remain: accept/revise the technical mapping proposal; new-table migration/test scope; bootstrap access mechanism; who may provision/link/recover/unlink and how last identities are handled; enterprise route policy; assurance/session/revocation metrics; audit owner/failure/notification policy; data minimization/retention/erasure; provider budget, plan, region, DPA/subprocessors and evaluation acceptance criteria. Auth0 evaluation preference is approved; accounts, purchases, integration and production remain unapproved. No whole human task is complete.
