# ADR-006: Local PostgreSQL RLS and synthetic tenant context

- Status: Accepted for LOCAL DEVELOPMENT AND TESTING ONLY
- Date: 2026-10-25 (America/Winnipeg)
- Authority: explicit Product Owner Phase 2D-1C approval of RLS, role separation, transaction-local context and narrowly self-scoped bootstrap
- Trace: ADR-001/ADR-002/ADR-004/ADR-005, HUM-006/HUM-008, AI-010, REQ-006/REQ-007/REQ-008, T-015 subset

## Decision

Use explicit application scoping plus PostgreSQL ENABLE and FORCE RLS for staff identity and the five tenant-owned tables. Keep the two-row capability catalogue shared and SELECT-only. Do not register database authorization readers, an authenticated principal or a protected endpoint in this slice.

Separate nonlogin schema ownership, a migration login allowed to SET ROLE to that owner, an explicitly privileged synthetic setup login, and a SELECT-only reader login. Reader has no ownership, privileged memberships, BYPASSRLS, administrative flags, writes, schema CREATE, temporary-table privilege or history-table access. Setup's BYPASSRLS is explicit, restricted to named fixture-table object privileges, and never runtime authority. Preserve ADR-005's nonlogin/nonbypass revision-maintainer and constrain its access to membership key/revision SELECT and revision-column UPDATE. Its two maintenance policies deliberately apply to any membership row under that role: only the existing non-public fixed trigger can enter it, derives the exact parent from the trigger row, and exposes no caller-argument operation. No runtime role may assume it.

The acyclic reader policy graph is self user/self membership -> organization -> self grants/capabilities -> exact branch. Membership bootstrap is independent of organization visibility and returns only the actor's own membership for one candidate organization, including suspended/removed states needed for denial. All protected reads require valid nonzero actor, organization and branch context. Disabled identity/branch and suspended/closed organization state may remain internal denial metadata, not authority. Application lifecycle/capability/revision rules remain mandatory; RLS does not evaluate the full Domain authorization decision.

Use a dedicated reader datasource and an explicit repeatable-read, read-only transaction. Set actor/organization/branch parameters with transaction-local `set_config(...,true)` on that same connection. Validate scope shape, fully materialize before disposing, and end the transaction before pool return. Cleanup uses an independent bounded token; uncertain rollback clears the affected pool before disposal. Keep Npgsql default reset behavior, no session-level tenant settings, multiplexing or automatic retries. Missing/empty/malformed/zero context returns no protected rows via stable invoker-rights helpers with fixed search_path.

## Trust and revocation limits

`TrustedSyntheticActor.FromTrustedHarness` is an explicit local test-harness reference, not a login, authenticated principal, token or proof that a browser user owns an ID. The transaction object conveys no authorization result. Production actor binding is absent; no client-supplied identity is wired to this mechanism. Arbitrary SQL under reader credentials can reset these descriptive variables and impersonate another existing actor: an executable negative-control test demonstrates this accepted threat limit. A compromised setup/migration/DBA identity is privileged by design.

Repeatable Read provides a coherent snapshot, not immediate revocation. A transaction with an earlier snapshot can retain earlier membership/grant visibility after revocation commits. A subsequent new transaction observes the committed suspension and changed revision; an independent membership in another tenant is unaffected. Future sensitive writes require separate transactional revalidation/locking and audit design. No such workflow is implemented.

## Consequences

Second migration `20261026040035_TenantRowSecurity` transfers schema/table/guard ownership, creates explicit policies and default privilege restrictions, and preserves the initial migration. The admin is used only for the local role-creation bootstrap and narrowly scoped privileged fixture teardown; normal migration checks use the distinct migrator. Roles are cluster-scoped, and existing-role collisions are intentionally not adopted. Destructive Down operations remain guarded. No production deployment/security certification or full T-015 completion follows.

Production identity binding, secret separation, runtime composition/adapters, CI/disposable-database bootstrap, independent restore and remaining authorization tests require subsequent scope review. See [implementation and evidence](../phase2d-1c-rls-tenant-context.md).
