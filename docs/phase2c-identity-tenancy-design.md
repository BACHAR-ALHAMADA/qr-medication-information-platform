# Phase 2C — Identity, authorization and multi-tenancy design

Initial proposal: 2026-10-17. Updated: 2026-10-22. **Core model approved; synthetic authorization foundation implemented locally for review.** The Product Owner approved the bounded organization/membership/branch/capability model in [ADR-004](decisions/ADR-004-organization-authorization-foundation.md). Provider, production role matrix, database isolation implementation and lifecycle policies remain proposals. No login, database, protected API, dashboard or patient functionality is introduced.

## Authority and review

Reviewed [AGENTS.md](../AGENTS.md), accepted [ADR-001](decisions/ADR-001-modular-monolith.md), [ADR-002](decisions/ADR-002-postgresql.md), [ADR-003](decisions/ADR-003-qr-token-strategy.md), [technical architecture](technical-architecture.md), [system overview](architecture/system-overview.md), [module boundaries](architecture/module-boundaries.md), [scalability](architecture/scalability.md), [authentication](security/authentication.md), [authorization](security/authorization.md), [threat model](security/threat-model.md), [QR security](security/qr-security.md), [privacy/security](privacy-security.md), [privacy model](privacy/privacy-model.md), [classification](privacy/data-classification.md), [minimization](privacy/data-minimization.md), [requirements](product-requirements.md), [feature catalogue](feature-catalogue.md), [roadmap](project-roadmap.md), [status](project-status.md), [testing strategy](testing-strategy.md) and all four [task trackers](../tasks/HUMAN-TODO.md).

Existing approved principles require server authorization, verified tenant membership, least privilege, separate public/private paths and minimal telemetry (REQ-005 through REQ-009). ADR-001 retains a modular monolith; ADR-002 selects PostgreSQL/EF Core but leaves isolation unresolved; ADR-003 remains binding on any future private QR workflow. No accepted ADR conflict is identified or ADR changed. ADR-004 records the subsequently approved core model and bounded synthetic implementation. Other proposals below refine F-002/REQ-006 without approving dashboards or private patient workflows. HUM-005/HUM-006 remain partially unresolved; HUM-001 controls broader scope.

## Proposed organization and identity model

Approved: **one independently administered pharmacy organization per tenant**, representing an explicitly onboarded pharmacy operating boundary. Do not assume that a shared brand, corporate parent, email domain or identity-provider directory proves shared data authority. Independently administered organizations are separate tenants under ADR-004. Verification of actual organizational ownership and legal responsibilities still requires HUM-006/HUM-007 review.

| Concept | Proposed responsibility and invariant |
|---|---|
| Pharmacy organization | Owns tenant data and memberships; active/suspended/closed lifecycle; stable opaque reference. Suspension denies tenant operations without deleting historical audit. |
| Branch | Belongs to exactly one organization. Branch is a subordinate authorization scope, not an independent tenant. No automatic cross-branch access; grant explicit branch sets or explicit organization-wide scope. Cross-organization moves require a separately approved transfer process. |
| Application user | One staff identity mapping using validated OIDC issuer plus subject; active/disabled state. Email is contact information, never the stable identity or proof of membership. No local password store proposed. |
| Organization membership | Links user to one organization, with active/suspended/removed lifecycle, roles, branch scope and authorization revision. Roles never apply to the user's other memberships. |
| Invitation | Pending, expiring offer of one membership and bounded role/branch scope; conveys no access until authenticated acceptance and server checks. |
| Platform grant | Separate, narrowly scoped platform administration authority; does not imply pharmacy membership or clinical-record access. |

Organizations owns organization/branch/membership rules; Identity owns provider integration, user mapping and session state. Application use cases consume explicit contracts for current access decisions; they do not read another module's tables. Infrastructure implements eventual storage/provider integration. API handles authentication transport and request validation, while Application enforces permissions and resource ownership. Domain rules remain framework independent. Audit ownership is still HUM-006 Decision Required; recommend an explicit Audit application contract implemented inside the monolith, with one accountable owner.

Do not implement manufacturer tenants, parent-company hierarchy, cross-tenant reporting, patient identity or tenancy transfers in the foundation. These have no approved workflow. Qualification as a pharmacist requires an owner-defined verification process; a role label does not establish licensure.

## One account, multiple organizations

A synthetic staff user may be Pharmacist in organization A, Pharmacy Admin in organization B and have no membership in C. Login identifies that user once. A workspace selector lists only their active authorized organizations and permitted branches. Selecting B requests a context; the server verifies B membership and uses B's permissions only. No union of roles or data across A and B is permitted.

Use an explicit organization reference on each protected request as a **selector**, never as authority. After authentication, resolve a request-scoped immutable context from current user, active organization, membership and branch scope. Avoid a mutable session-wide active tenant: two tabs using different organizations must not change one another's scope. Missing, conflicting or unauthorized selectors fail closed. No implicit default tenant for protected operations.

React should show the current organization/branch clearly, clear protected state on switch/logout/removal, cancel pending requests and discard late responses from a previous context. Cache keys must include user/session, organization, branch and relevant permission context. Protected responses should be non-cacheable by shared/browser caches. These UI safeguards supplement server checks. Login and tenant selection must remain keyboard accessible and support future localization/RTL under HUM-002.

## Proposed permissions

Roles are bundles of named capabilities plus resource and branch scope, not an ordered hierarchy. Deny unlisted operations. The following is a review matrix, not an approved release feature set. Public DIN reads remain available to everyone.

| Capability | Platform Admin | Pharmacy Admin | Pharmacist | Authorized pharmacy staff |
|---|---|---|---|---|
| Read public catalogue | Yes | Yes | Yes | Yes |
| Onboard/suspend organizations | Explicit platform grant | No | No | No |
| Manage organization settings/branches | No by default | Own organization | No | No |
| Invite/remove members, assign approved roles/branch scopes | No by default | Own organization, bounded delegation | No | No |
| Read own membership/workspace | Only with membership | Own membership | Own membership | Own membership |
| Read tenant operational workspace | Only with membership | Explicit operational grant | Permitted branches | Explicit task grant and branches |
| Read tenant security audit | Platform security events only | Own organization's minimized events | No by default | No by default |
| Publish central clinical content | No implicit grant | No | No | No |
| Read/write patient assignments or private instructions | No grant in this phase | No grant in this phase | No grant in this phase | No grant in this phase |

Candidate capabilities include `platform.organizations.manage`, `organization.settings.manage`, `organization.memberships.manage`, `organization.audit.read` and `workspace.read`. Exact operations and bundles require HUM-005 approval. Pharmacy Admin administration must not imply clinical access. Future clinical permissions would be separately approved under F-006/HUM-001/HUM-004. Central editorial permission remains a separate HUM-003/HUM-005 decision.

An administrator cannot grant platform privileges, alter another organization, self-elevate or grant capabilities outside an approved delegation set. Assigning another Pharmacy Admin should require an explicit stronger delegation permission and recent MFA. Prevent removal/demotion of the last active administrator without an approved recovery path. First platform/organization administrator bootstrap, qualification verification, ownership disputes and emergency access need named human processes. Recommend no impersonation or emergency clinical bypass in the first milestone.

## Authentication recommendation and options

Recommend an established **OIDC provider with Authorization Code flow and PKCE**, integrating through maintained ASP.NET Core protocol handlers. Prefer a same-origin backend-for-frontend arrangement in the existing ASP.NET application: the server acts as confidential OIDC client, keeps tokens server-side and gives React a Secure, HttpOnly session cookie. No separate service is required. Cookie-authenticated mutations need anti-CSRF validation, appropriate SameSite policy and origin checks; OIDC callbacks need state/correlation and nonce validation. Restrict redirect/logout destinations; use HTTPS. This is a design recommendation, not a package or deployment selection. [Microsoft guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0) describes server-side OIDC and recommends BFF for browser applications; [RFC 9700](https://www.rfc-editor.org/rfc/rfc9700.html) provides OAuth security guidance.

Alternative: React as an OIDC public client using code + PKCE and API audience-bound access tokens. It supports independently hosted frontends/API consumers but exposes tokens to browser script and adds token renewal, CORS and storage risks. Never ship a client secret to React; avoid persistent browser token storage. Neither option permits implicit/password grants or custom credential handling. Prefer BFF for this first-party workspace unless deployment requirements justify the SPA alternative.

Validate issuer, signature/key rollover, audience, lifetime and protocol correlation using maintained libraries; distinguish ID tokens (login) from access tokens (API authorization). Map identity by `(issuer, subject)` as defined by [OIDC Core](https://openid.net/specs/openid-connect-core-1_0.html). Provider directory/organization claims do not establish platform pharmacy membership. Do not auto-link accounts by email or automatically trust arbitrary issuers. Account linking/provider migration requires controlled re-verification. Application memberships remain authoritative even when provider tokens contain stale role claims.

| Provider operating option | Advantages | Costs / review needs |
|---|---|---|
| Managed OIDC identity service | Reduces credential, MFA and protocol operations | Recurring cost, residency/subprocessors, export, outage and vendor dependency; verify MFA, recovery, federation and revocation behavior |
| Self-hosted maintained OIDC provider | More operational/configuration control | Team owns patching, availability, backups, abuse response, keys and recovery; greater operational burden |
| Federation to pharmacy enterprise directories through a chosen broker/provider | Reuses pharmacy workforce identities | Uneven partner capabilities and MFA evidence; onboarding/offboarding mappings still require local memberships |

No vendor is selected or account contacted. HUM-005/HUM-007/HUM-008 must evaluate ownership, supported deployment/runtime, MFA enforcement and evidence, subject stability, recovery, logout/revocation hooks, accessibility, residency, support and cost before selection.

Propose MFA for all staff before protected access, with phishing-resistant passkeys/security keys preferred and required for privileged administration where feasible. Provider-managed TOTP may be an approved fallback after recovery/accessibility review; do not treat email/SMS alone as the preferred privileged assurance. Require recent MFA for privilege changes, recovery and sensitive administration, checking provider-documented assurance/authentication-time evidence on the server. Specify enrollment, lost-device recovery, backup authenticators, helpdesk verification and notifications; recovery must not silently bypass MFA. [NIST authentication guidance](https://pages.nist.gov/800-63-4/sp800-63b.html) is technical evidence, not a claim of legal applicability or assurance certification.

Proposed session defaults for review: 15-minute idle timeout, 8-hour absolute lifetime and MFA within 5 minutes for privileged changes. Shared pharmacy workstations need explicit logout and no persistent “remember me” initially. Exact values, accessibility impacts, recovery ownership and provider capabilities remain HUM-005 decisions. Horizontal hosting must support shared protected session state/key rotation without adding infrastructure speculatively; deployment/session storage choice belongs to HUM-008.

## Server-enforced tenant authorization

For every protected operation, including list/count/search/export/bulk paths:

1. Authenticate and check local user/session validity. Public DIN endpoints stay explicitly anonymous and independent of identity-provider availability.
2. Resolve the requested organization against current active membership and organization status; validate branch scope within that organization. Reject missing/conflicting context.
3. Authorize the named action and target scope in Application. Recheck current membership/authorization revision rather than trusting login-time roles.
4. Load resources by authorized organization plus resource reference. A resource ID from B presented in A must never load B's record, including nested references, joins and related attachments.
5. Derive ownership on create from validated context; reject attempts to change tenant ownership or submit relationships outside it. Preserve ownership through updates/deletes and module contracts. Multi-record operations must authorize every target atomically or reject the request.
6. Record the minimized audit outcome. No permission means no operation even when the UI exposes a button or a valid object reference exists.

Use 401 for unauthenticated API requests, generic 403 for denied context/action, and a consistent not-found response for out-of-scope object references without confirming another tenant's record exists. Never echo attacker-supplied tenant names or resource details in errors. Validate selectors consistently across path/header/body. No client header or provider tenant claim bypasses membership checks.

Background tasks, reports and future integrations need explicit service identity, tenant binding and narrowly authorized operations; no ambient “system tenant.” Tenant context must not leak through pooled connections, singleton services, caches, files or queued work. Sensitive writes should validate authorization revision inside their transaction so removal/demotion races cannot commit using stale grants.

## PostgreSQL isolation options — no schema selected

Application authorization and tenant scoping are mandatory under every option. EF Core query filters may reduce omissions but are not the complete boundary: raw SQL, filter bypass, writes and relationship consistency require independent enforcement.

| Option | Benefits | Trade-offs |
|---|---|---|
| Shared database/shared tables, application scoping | Lowest operational overhead; straightforward cross-module deployment | An omitted predicate can expose data; all access/write paths require enforcement and adversarial tests; greatest reliance on application correctness |
| Shared tables plus PostgreSQL RLS | Database backstop for accidental unscoped reads/writes; one deployment/migration stream | Policy and connection-context complexity; owner/superuser/bypass behavior, relational side channels and privileged jobs need care; shared backup/restore blast radius |
| Schema per organization | Namespace separation and some tenant operational flexibility | Repeated migrations, routing/search-path mistakes and permission overhead; shared database roles may still cross schemas; namespace alone is not isolation |
| Database per organization | Stronger credential/data separation; easier tenant-specific backup/restore | Connection pools, migrations, monitoring, routing and recovery multiply; shared app compromise still matters; greater cost for small independent pharmacies |

**Recommendation for approval:** shared tables with explicit application scoping plus RLS before tenant-owned persistence. Reconsider database-per-organization if approved contractual/operational isolation needs require it. Do not introduce either until HUM-006 records the choice and HUM-008 confirms operational capacity.

For an RLS choice, require fail-closed policies for reads and write checks, tenant-consistent relationships/uniqueness, and a runtime role without superuser/BYPASSRLS or table ownership; use FORCE ROW LEVEL SECURITY where appropriate. Keep migration/maintenance credentials separate from runtime. Establish validated tenant context transaction-locally on each checked-out connection and prevent reuse after commit/rollback; test pool reuse, cancellation and retries. Missing context denies access. Policies apply to every tenant-owned relation and related storage path. [PostgreSQL documentation](https://www.postgresql.org/docs/current/ddl-rowsecurity.html) explains default denial, owner bypass, FORCE RLS and integrity-check exceptions.

RLS is a backstop, not proof that a malicious runtime with arbitrary SQL execution cannot set a different tenant context. SQL injection prevention, least-privilege grants and application membership checks remain necessary. Referential/uniqueness errors must not disclose other tenants. Backups, privileged maintenance, restore and audit extraction need separately controlled procedures. Concrete policies, connection handling, EF design and constraints belong to later approved implementation design; no DDL is specified here.

## Public catalogue versus tenant ownership

MedicationCatalogue owns shared public product metadata, public revisions/provenance and eventual reviewed publication. Existing DPD lookup remains public source metadata, not reviewed clinical guidance. Public read responses contain no memberships, tenant preferences, usage history or patient associations. Public catalogue identifiers confer no tenant authority.

Organizations owns memberships/branches/settings; any future pharmacy-specific selections, workflow state or patient assignments are tenant-owned and use separate protected contracts and read models. Referencing a public product does not make a pharmacy's association with it public. Future tenants cannot edit central reviewed content through tenant administration. Shared public caches may contain only eligible public fields; protected queries, cache entries and audit visibility remain tenant-scoped.

Proposed staff-data classification: user contact and membership relationships are personal/security-relevant data, branch settings are restricted operational data, invitations/session material are secrets, and audit metadata is restricted and may be personal. Keep only necessary identity references, contact information, scope/status and lifecycle evidence; no health details or patient data are needed for this foundation. HUM-007 must validate purpose, access, retention/deletion and organizational responsibilities. No statutory retention period or compliance conclusion is asserted.

## Invitations, removal, revocation and audit

Invitations require an authenticated authorized inviter, active target organization and bounded delegable role/branch scope. Use expiring, single-use, high-entropy invitations with verifier storage, recipient binding and rate limits; proposed expiry is 48 hours, awaiting HUM-005. Do not log invitation URLs or include them in analytics. Use authenticated POST acceptance rather than consuming a link on GET. Acceptance rechecks recipient through provider-verified contact or approved identity binding, inviter authority, current invitation scope and organization status; process replay/concurrent acceptance atomically. Changing email alone must never transfer an existing account. An expired/revoked invitation grants nothing; no automatic join based on email domain. Invitation delivery channel and bootstrap ownership remain decisions.

Membership removal/suspension or role/branch reduction must take effect independently of token expiry: propose that every subsequent protected request reads current authorization state without a stale positive membership cache. Increment an authorization revision and reject old contexts. A user removed from A may keep B if still authorized. Global user disable/session revocation must deny all protected access. Organization suspension denies all its memberships; pending invitations must not activate access. Preserve minimized historical audit without keeping unnecessary profile data indefinitely.

Define the concurrency guarantee explicitly: after a removal commits, new checks deny access; sensitive mutations serialize/recheck membership revision before commit. Already delivered responses cannot be withdrawn. Cancellation of in-flight reads and an acceptable maximum revocation delay for provider-wide account disable require HUM-005/HUM-008 decisions. Provider logout alone may leave local sessions or access tokens valid: combine local session invalidation, current membership checks and tested provider revocation/event or revalidation mechanisms. Initial proposal: local removals have no positive-cache grace period; provider-disable propagation must be demonstrated within an owner-approved bound before rollout.

Audit authentication outcomes, MFA/step-up and recovery outcomes, invitations and acceptance/rejection, organization lifecycle, membership/role/branch changes, denied operations, session revocation and authorized administrative/security-audit reads. Future sensitive reads/writes require their own approved audit coverage. Capture server time, stable actor reference, authorized tenant/platform scope, action, opaque target, outcome, bounded reason code and correlation reference; record minimal role/scope changes where needed. Distinguish validated context from an untrusted attempted selector. Never record tokens, cookies, factors, request bodies, patient details or full secret-bearing URLs. Rate-limit/aggregate abuse telemetry without losing required security evidence.

Audit writers append through a controlled interface; ordinary users/admins cannot rewrite/delete events. Restrict tenant audit readers to their organization, reserve cross-platform security review for a separately granted duty, and audit audit access. Protect integrity, transport/storage and backup access; detect delivery failures. Propose atomic audit with administrative changes, failing those changes if required audit cannot be recorded. Public DIN reads must not depend on private audit availability. HUM-006/HUM-007/HUM-008 must choose ownership, retention/deletion, integrity mechanism, incident access and failure handling before rollout; an append interface alone is not tamper-proof storage.

## Threats and planned acceptance tests

**T-013** extends T-005/T-006/T-007/T-008 for identity and tenant foundations. The matrix below describes broader target coverage; only the synthetic subset listed under implemented evidence is executable and verified. Use two synthetic organizations with overlapping local references, multiple branches, one multi-organization user, a removed user and a platform-only administrator. No real patient records or live provider credentials.

| Threat/scenario | Proposed acceptance evidence |
|---|---|
| Forged/missing/conflicting tenant selector or provider directory claim | API rejects unauthorized context; no data, existence disclosure or side effect; membership is checked despite valid login |
| Object-reference substitution, nested IDs, count/search/export/bulk bypass | Every read/write remains tenant-scoped; mixed-tenant batches fail without partial writes; related ownership cannot change |
| Role escalation or staff calling admin endpoints directly | Application denies ungranted capabilities, self-promotion, platform-role grants and other-organization changes; no clinical privilege inferred from admin role |
| Branch bypass | A member authorized for branch A1 cannot read/change A2; foreign organization branch references fail; organization-wide scope must be explicit |
| Role union, two tabs, stale responses or cache reuse | Admin in B gains no admin authority in A; tabs retain independent contexts; switched/revoked context never renders late protected data |
| RLS omission, raw SQL, write bypass, connection-pool contamination | Real PostgreSQL tests under runtime-equivalent privileges deny unscoped/cross-tenant reads/inserts/updates/deletes, missing context and reused context after rollback/retry; verify owner/bypass configuration |
| Stale grants or removal/write race | Removed membership, disabled user, suspended organization and role reduction deny subsequent requests with otherwise valid sessions/tokens; concurrent sensitive writes cannot commit stale grants |
| Invitation theft/replay/wrong recipient/stale inviter | Wrong identity, revoked/expired offer, changed inviter authority and parallel reuse fail; acceptance grants only the approved tenant/scope |
| Forged/expired/wrong-audience/wrong-issuer tokens; CSRF/session abuse | Supported middleware rejects invalid credentials/callbacks; cookie writes need anti-CSRF; session expiry, logout, MFA/recovery and revocation meet approved bounds |
| Audit/log/error leakage or audit-service failure | Capture synthetic canary secrets to verify exclusion; no other-tenant metadata in errors/audit views; protected change fails safely when required audit fails |
| Public/private boundary or identity-provider outage | Public exact-DIN contract and anonymous lookup continue; public response/cache has no tenant/private fields; no global login challenge on public routes |

The synthetic Domain/Application xUnit subset is now implemented. In-process protected-API negative tests and real PostgreSQL integration tests remain deferred until their separately approved components exist. Add browser context-switch/session checks and a provider sandbox conformance exercise only after authorization. Mocked tests cannot establish RLS or provider behavior. Review dependency directions against T-007. Record actual versions, evidence and limitations before claiming a milestone passes.

## Minimum Product Owner decisions and gates

Core organization tenancy, multi-membership, explicit branch/context grants, deny-by-default capability separation and the synthetic foundation scope are approved in ADR-004. The remaining gates below still apply to provider, persistence, full role/delegation administration and lifecycle implementation. Existing IDs remain; no whole human item is closed.

| Gate | Minimum explicit decision | Existing owner record |
|---|---|---|
| Before any authentication/database implementation | Synthetic rules scope approved; separately approve the actual provider/persistence scope and acceptance evidence. Private dashboards, clinical actions and F-006 remain excluded | HUM-001 |
| Before tenant persistence design/implementation | Organization tenancy, separate multi-memberships and explicit branch grants approved. Decide onboarding/ownership verification, organization-wide scope and first-admin authority | HUM-006, with HUM-007 responsibilities review |
| Before authentication implementation | BFF versus SPA; named provider and operating owner; issuer/account mapping; MFA/recovery and bootstrap; session values and measurable revocation bounds | HUM-005/HUM-008, HUM-007 provider/privacy review |
| Before expanded authorization/administration | Synthetic capability/branch rules and separation approved. Decide production role mappings, administrative delegation, pharmacist qualification, last-admin recovery and emergency policy | HUM-005 |
| Before tenant persistence implementation | Application scoping plus chosen database isolation; runtime/maintenance privileges, pooling behavior and tenant restore requirements | HUM-006/HUM-008; record a new decision without editing accepted ADRs |
| Before lifecycle/audit storage or invitations | Audit owner, access and failure policy; minimal staff/invitation data, delivery, expiry, retention/deletion and incident responsibility | HUM-005/HUM-006/HUM-007/HUM-008 |

Not every patient/QR decision is needed for a synthetic staff foundation. HUM-004 and F-006 decisions remain deferred until that scope is explicitly proposed. Hosting commitments, live pharmacy onboarding and production rollout require the wider applicable privacy/security/operations review; provider research is not permission to create accounts or procure services.

## Small proposed implementation sequence

1. **Core approval received on 2026-10-22; remaining infrastructure decisions pending:** Product Owner records the gate decisions above, selected options, approved scope and remaining risks. Prepare a new decision record for identity/tenant isolation if approved; preserve ADR-001 through ADR-003. Reconcile F-002/REQ-006/T-013 and task records (AI-004).
2. **Approved on 2026-10-22 and implemented locally for review:** implement framework-independent membership/branch/permission rules and Application authorization contracts with synthetic in-memory fixtures and xUnit negative tests. No live provider, credentials, persistence, dashboard, patient data or dependency additions unless separately authorized. Exit evidence: two organizations, role/branch separation, denial on missing/removed membership, no cross-tenant role union and unchanged public-DIN behavior.
3. **Separately approved isolated infrastructure slice:** integrate the selected OIDC approach in a nonproduction provider environment and tenant persistence under the recorded isolation decision. Add only approved dependencies/migrations at that later time; exercise real database isolation and revocation/audit failure tests before any tenant workspace is exposed.
4. **Separately approved minimal protected workspace:** current-user memberships and explicit workspace selection, then bounded organization/invitation administration. Require MFA, audit and negative authorization evidence. Defer clinical permissions and private QR flows. Run existing backend/frontend regression checks plus identity/tenant outage and browser switching checks.

Public DIN lookup routes/contracts and source-metadata disclaimers remain intact throughout. Protected routes use explicit policies without turning public endpoints into login redirects. Each later provider, persistence or workspace milestone still needs scope approval and actual test evidence.

**Stop point:** synthetic foundation is ready for Product Owner review. No further implementation, staging, commit or push is authorized.

## Implemented synthetic foundation

Authority: Product Owner instruction on 2026-10-22; [ADR-004](decisions/ADR-004-organization-authorization-foundation.md). Approved decisions: independently administered organization tenancy, separate multi-organization memberships, explicit organization and branch context, current membership verification, deny-by-default capabilities, separate platform/pharmacy/clinical authority, no privilege union/self-elevation/unauthorized delegation, and unchanged anonymous DIN lookup.

New local projects: `Identity.Application` (immutable enabled-user contract and current identity reader), `Organizations.Domain` (organization/branch/membership values, defensive immutable grants and pure policy), and `Organizations.Application` (current snapshot reader and authorization/delegation contracts). No ASP.NET, EF, storage or third-party package dependency is added to these projects. The existing xUnit project references the authorization project; its historical catalogue name is retained. Synthetic reader implementations exist only in `tests/Unit/StaffAuthorizationTests.cs`.

The minimal capabilities are `WorkspaceRead` and `DelegationScopeEvaluate`. No role name grants either automatically. Every action requires an explicit branch; organization-wide access remains Decision Required. `EvaluateActorDelegationScopeAsync` returns an `ActorDelegationScopeEvaluation` whose `IsActorScopeAllowed` value evaluates only current actor authority to propose `WorkspaceRead` in the actor's evaluated organization/branch, bounded by held and delegable capabilities. The candidate must be nonempty and different from the actor.

**Recipient existence, recipient organization membership, recipient status and actual grant execution are NOT verified.** No candidate eligibility lookup or membership mutation occurs. A positive result cannot authorize or execute a grant. Target scope describes the actor's proposal, not a verified recipient relationship. Eligibility, onboarding, invitations, administration and production delegation mappings remain deferred under HUM-005.

Every call checks current enabled identity and coherent organization/membership/branch state, including ownership, lifecycle and exact positive revision. Missing/invalid/foreign context denies without returned resource details. Frozen copied grant sets and independent immutable action contexts prevent shared mutable request scope. Trusted readers are contracts only: caller identity is not authenticated here, revisions are not credentials, and no result authorizes a later mutation without another check. Real transaction races, provider revocation and RLS cannot be proven by these fixtures.

Historical initial verification on 2026-10-22: 43 new xUnit cases passed, alongside the existing 45 catalogue unit and eight API integration cases (96 backend total, none skipped). Coverage includes positive grants, tenant/branch/subject substitution, missing/unknown context, disabled users/branches, suspended/closed organizations, removed/suspended memberships, stale revisions, no cross-organization role union, delegation limits, defensive grant copying, and deterministic interleaving of two requests on one service. Frontend build, standalone typecheck, zero-warning lint and all 28 tests passed under Node 24.22.0. No frontend or public API source changed.

A pre-existing running API locked normal backend output files; verification succeeded without stopping it using `dotnet test MedicationInformationPlatform.sln --no-restore --verbosity minimal -p:BaseOutputPath=bin/Phase2CReview/`. Restore used only the existing local package cache to resolve new project references; no external dependency was added or downloaded. The first default-output build failure was environmental, not a passed check. Manual browser/assistive-technology QA remains pending AI-008; no live-provider, RLS, invitation, audit-store or deployed authorization success is claimed.

## Pre-commit review corrections

Product Owner authorized all three corrections on 2026-10-22. BUG-005/FIX-005 clarifies the actor-scope contract and named result without adding recipient authorization. BUG-006/FIX-006 records focused regression gaps and unbounded interleaving-test waits. The interleaving test now has a ten-second cancellation deadline, bounded waits, guaranteed release/cancellation in `finally`, and a two-second cleanup bound. No test sleeps or timing-based ordering is used.

Current backend verification: **61 authorization cases**, 45 existing catalogue unit tests and eight existing API integration tests passed (**114 total**, none skipped). The 18 additional cases cover two explicit branches in one organization; revocation in A preserving B; actor disable, membership removal and held/delegable-capability reductions after success using the current revision; unknown/disabled/foreign/ungranted candidate recipients remaining outside scope evaluation; pre-cancelled requests for both operations, including wrong-action scope evaluation; and failures from either reader in both operations. The constructor regression also checks unknown delegable capabilities.

Commands: `dotnet test MedicationInformationPlatform.sln --no-restore --verbosity minimal -p:BaseOutputPath=bin/Phase2CReview/`; from `frontend/`, `npm.cmd run build`, `npm.cmd run lint`, `npm.cmd test`, and `.\node_modules\.bin\tsc.cmd --noEmit`. No new dependencies or restore was needed for these corrections. Current frontend verification under Node 24.22.0 passed production build, standalone TypeScript, zero-warning lint and all 28 tests. No browser/assistive-technology, live-provider or database isolation verification is claimed.

Final static review after corrections: local Markdown file links/anchors and fenced blocks passed; source whitespace and `git diff --check` passed; sensitive-value pattern scan found no matches. The index remains empty on `feature/identity-tenancy`. Public API, MedicationCatalogue, frontend source and ADR-001 through ADR-003 have no working-tree diff. Only existing ignored build/cache outputs were generated; no external dependencies, endpoints or new infrastructure were added. These bounded inspections do not certify absence of all secrets or prove deployed isolation. Ready for Product Owner review; no commit or push.
