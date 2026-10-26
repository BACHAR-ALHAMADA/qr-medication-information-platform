# ADR-005: PostgreSQL foundation integrity and revision maintenance

- Status: Accepted for the bounded local PostgreSQL foundation; RLS/runtime access pending
- Date: 2026-10-25
- Authority: explicit Product Owner approval of the Phase 2D-1B FK exception and PostgreSQL revision triggers
- Trace: ADR-001/ADR-002/ADR-004, HUM-006, AI-010, REQ-006/REQ-008, T-015 schema subset

## Decision

Permit exactly one cross-module physical FK: `organizations.memberships.user_id` references `identity.staff_users.user_id`, with ON DELETE RESTRICT and ON UPDATE RESTRICT. This is a narrow exception to ADR-001 storage independence, not permission for module Domain/Application coupling, cross-module table queries or EF navigation. Identity and Organizations keep their own persistence model definitions and DbContexts. The offline setup tool composes those definitions into one initial EF migration/history for this approved seven-table slice. No API reference or runtime adapter is added. Any further cross-module dependency requires separate approval.

Identity must be created before the FK and cannot remove a referenced user independently. Module extraction or future migration separation must explicitly coordinate removal/replacement of this physical dependency and shared migration history. Module lifecycle/deletion contracts remain future work; no coordinated deletion workflow is implemented.

Membership revision enforcement is database-owned. Inserts start at one; each membership UPDATE increments the stored revision, including no-op updates (conservative invalidation). Grant/capability INSERT, UPDATE and DELETE atomically lock/update the exact parent, invoking the membership trigger for its increment. Parent/grant ownership and grant reference keys are immutable; use delete/insert for grant replacement. Manual revision rewrites and overflow fail. Removed membership rows remain tombstones; physical membership DELETE and revision-table TRUNCATE fail, preventing ordinary delete/recreate reset. Transaction/savepoint rollback restores both records and revisions.

The sole SECURITY DEFINER function is owned by `qr_revision_maintainer`, a NOLOGIN/NOSUPERUSER/NOBYPASSRLS role with schema USAGE and only membership key/revision SELECT plus revision-column UPDATE. Its CREATE-on-schema permission is granted only while assigning function ownership and immediately revoked within the migration transaction. All function execution is revoked from PUBLIC. Function SQL uses qualified relation names and `search_path=pg_catalog`. Guard functions use invoker rights. No application role may assume the maintainer. No runtime login or grant-management workflow is introduced. Future privileged schema administration can bypass/remove triggers; this is not protection against a compromised administrator.

## Consequences and boundaries

The offline local migration is separate from application startup. Tables and invoker guard functions remain owned by the local setup administrator; no application credential receives ownership. A destructive Down migration deliberately throws pending a separately reviewed recovery plan. The migration creates a cluster-scoped maintainer role and refuses an existing role with that name rather than adopting unknown privileges.

No RLS is enabled or partially introduced. Before runtime access, review FORCE RLS, maintainer-specific policies, self-bootstrap visibility, trusted actor/tenant context and its arbitrary-SQL threat limit, non-bypass runtime credentials and grants. The nonowner test role has synthetic test-only writes; it is neither a runtime role nor a tenant-isolation model. Revisions do not immediately revoke already-running snapshots/requests or authorize future operations. Public DIN remains database-independent.
