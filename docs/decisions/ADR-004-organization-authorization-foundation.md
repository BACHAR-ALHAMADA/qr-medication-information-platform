# ADR-004: Organization-scoped synthetic authorization foundation

- Status: Accepted for the bounded synthetic foundation; infrastructure and release scope pending
- Date: 2026-10-22
- Authority: explicit Product Owner approval of Phase 2C core model and synthetic authorization implementation
- Trace: F-002, REQ-006/REQ-008/REQ-009, T-013, HUM-001/HUM-005/HUM-006

## Decision

One independently administered pharmacy organization is one tenant. Branches belong to that organization; shared branding does not establish shared tenancy. One user may have independent memberships in multiple organizations. Each protected operation requires explicit organization context checked against current membership and an explicit branch grant. Capabilities are deny-by-default and membership-scoped. Never union privileges across organizations or infer clinical authority from pharmacy/platform administration. Unauthorized delegation and self-elevation are denied. Public anonymous exact-DIN lookup remains unchanged.

Implement only framework-independent rules, explicit Application contracts, synthetic in-memory fixtures and deterministic xUnit tests. Identity owns the current staff identity contract; Organizations owns membership/branch rules and authorization orchestration. No API registration or protected endpoint is added. ADR-001 through ADR-003 are preserved.

## Bounded implementation choices

The two non-clinical capabilities are `WorkspaceRead` and `DelegationScopeEvaluate`. All actions in this slice require one explicit branch; there is no implicit organization-wide scope. `EvaluateActorDelegationScopeAsync` returns `ActorDelegationScopeEvaluation.IsActorScopeAllowed`: it checks only the current actor's authority to propose `WorkspaceRead` within the actor's evaluated organization and branch. The actor must hold both capabilities and an explicit delegable `WorkspaceRead` grant. The candidate recipient reference must be nonempty and different from the actor. `DelegationScopeEvaluate` itself cannot be delegated.

A positive actor-scope result does **not** verify recipient existence, recipient organization membership, recipient status or actual grant execution. No recipient identity/membership is read and no grant or membership mutation is performed. The target organization/branch constrains the actor's proposal; it does not establish that the candidate belongs there. Recipient eligibility, onboarding and grant execution require separate decisions and later authorization. These conservative implementation limits do not approve a production role matrix or bootstrap policy.

Identity and organization readers are trusted internal contracts, implemented only by test fixtures here. User IDs are not browser-authenticated by this library. Each call reads current state, validates identity/ownership/status/revision and returns immutable action-specific evidence. That evidence is not a bearer credential or permission cache. A caller cannot authorize a later operation using old evidence. Readers must later provide coherent snapshots; real sensitive writes must recheck authority transactionally.

## Deferred decisions and consequences

Named role mappings, organization-wide grants, delegated administration, recipient onboarding, first/last-admin recovery, pharmacist qualification and emergency access remain HUM-005/HUM-006 decisions. No ambiguous permission grants access by default.

OIDC with a same-origin BFF and PostgreSQL application scoping plus RLS remain preferred directions pending separate implementation reviews. No provider, credentials, login, database model, migration, RLS, dashboard, clinical permission or patient data is introduced. Provider revocation, storage/audit ownership and lifecycle/privacy/operations decisions remain open under HUM-005 through HUM-008.

Synthetic rules and interleaving tests do not establish deployed tenant isolation, database transaction guarantees, provider/session revocation, invitation security or browser context handling. Those require later approved implementation and integration evidence. See the [design and evidence](../phase2c-identity-tenancy-design.md#implemented-synthetic-foundation).
