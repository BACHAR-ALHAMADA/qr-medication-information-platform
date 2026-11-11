# Phase 2D-1B schema review - 2026-10-25

Historical checkpoint record: subsequent local schema/RLS implementation supersedes the earlier blockers below. See [current scope and evidence](phase2d-1c-rls-tenant-context.md). The Product Owner has now authorized the reviewed end-of-day commits, feature-branch push and Draft PR only; no merge or new feature work.


Status: bounded schema/EF/migration/constraint-test implementation authorized by the Product Owner, but stopped at the explicitly requested architectural decision gate. No schema, EF project/package, migration, role, RLS policy or database test was added or executed. Trace: AI-010, HUM-006/HUM-008, REQ-006/REQ-008, planned T-015. General implementation approval does not select the two decisions the request explicitly reserves for review.

## Critical decisions before the complete migration

1. **Cross-module identity FK (HUM-006):** the proposed `organizations.memberships.user_id -> identity.staff_users.user_id` physically couples module storage/lifecycle. ADR-001 remains accepted and the plan explicitly requires approval of this exception. Recommended option: approve this restrictive FK for the modular monolith, with no Organizations EF navigation/query into Identity and coordinated Identity deletion through contracts. Alternative: retain a logical reference validated by an Identity contract, with an approved reconciliation/lifecycle design; that would weaken database-only referential integrity and change the proposal. No option was selected.
2. **Membership revision enforcement (HUM-006):** approve database-owned triggers versus an equally enforced constrained writer. Recommended direction: the proposed triggers atomically bump the parent revision for membership state, branch-grant and capability/delegability changes; reject overflow, ownership movement and manual revision rollback; retain removed memberships as tombstones. The eventual constrained NOLOGIN/NOBYPASSRLS maintainer, privileges and FORCE RLS interaction need review together. Alternative: a constrained transactional writer with direct DML revoked and equivalent locking, revision and rollback tests; an ordinary EF SaveChanges convention does not enforce this against other SQL writers. No option was selected. A positive-revision CHECK alone cannot establish monotonicity or automatic bumps.

These choices affect memberships and both grant tables, EF mappings, migration SQL and expected integration tests. Creating a reduced schema or a migration without the required invariants would not meet the requested complete seven-table milestone. Schema/migration/test implementation is therefore stopped; no partial security mechanism was introduced. The new request explicitly authorizes a local schema-only slice without RLS; historical plan wording coupling tables and RLS does not override that scope. No production/runtime isolation is claimed, and eventual RLS threat-limit/bootstrap and role approval remain open before protected runtime use.

The request also asks for one reviewed migration whereas the proposal describes module-owned migration histories. This is an implementation arrangement to reconcile after the critical decisions, preserving module ownership; it is not treated as an additional approval blocker in this review.

## Exact dependency selection for resumption

Selected metadata-compatible stable versions, not installed/restored or verified as a working database stack:

| Package/tool | Version |
|---|---|
| Microsoft.EntityFrameworkCore | 10.0.13 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.13 |
| Microsoft.EntityFrameworkCore.Design (PrivateAssets=all) | 10.0.13 |
| dotnet-ef (local tool) | 10.0.13 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| Npgsql | 10.0.3 |

[Provider package metadata](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3) targets net10.0 and requires EF Core/Relational >=10.0.4 and <11.0.0, and Npgsql >=10.0.3. [EF Core 10.0.13](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.13) and [Npgsql 10.0.3](https://www.nuget.org/packages/Npgsql/10.0.3) are available stable versions. [Microsoft support guidance](https://learn.microsoft.com/en-us/ef/core/what-is-new/) lists EF Core 10 support through November 10, 2028. [Npgsql 10 release notes](https://www.npgsql.org/doc/release-notes/10.0.html) document the maintained major release; no separate commercial support commitment is inferred. Metadata compatibility is not a PostgreSQL 18 integration result or a vulnerability/advisory audit. Recheck servicing/advisories at actual installation and validate dependency resolution and real database behavior then.

## Verification and preservation

- Branch `feature/postgresql-foundation`; HEAD `8ea0c662e2de635f6cd6265d8814de90aa7b917f`; safety stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1` preserved.
- Initial Git status: six modified tracked planning/tracker files and six untracked documents/configuration files; nothing staged. No staging, commit, push, merge, branch switch or deletion performed.
- All seven backup files in the existing temporary backup directory match their manifest hashes. Existing local work preserved; session changes are this new review and appended checkpoints only.
- Compose reports healthy PostgreSQL 18.6, published only at 127.0.0.1:55432. Readiness passed. Separate password-authenticated TCP SQL confirms PostgreSQL 18.6 and zero non-system application tables. No DDL or fixture writes executed; development administration account was not wired to application runtime.
- SDK 10.0.402; `dotnet test MedicationInformationPlatform.sln --configuration Release --no-restore` passed: 106 unit tests plus 8 mocked/in-process API integration tests; 114 total, zero failed/skipped. New PostgreSQL tests: zero added/executed. This does not establish schema constraints or tenant isolation.
- No frontend/API behavior, application source, dependencies, startup migration behavior or credentials changed. No confirmed bug/fix discovered; BUGS/FIXES unchanged.

Review gate: obtain explicit FK-exception and revision-enforcement outcomes before implementing the complete seven-table schema, reviewed migration and real-PostgreSQL constraint suite. No whole human tracker entry is closed.

## Phase 2D-1B approved foundation implemented - 2026-10-25

The Product Owner explicitly approved the restrictive Identity FK exception and database-owned revision triggers; see [ADR-005](decisions/ADR-005-postgresql-foundation-integrity.md). Separate module EF infrastructure and one offline migration now create the seven-table schema locally, with tenant-consistent constraints and hardened revision-maintainer privileges. EF 10.0.13/Npgsql 10.0.3 restore/build and local migration passed; 27 real PostgreSQL tests plus all 114 existing backend tests passed (141 total; zero failed/skipped). See [implementation, commands and limits](phase2d-1b-postgresql-foundation.md). Earlier decision-blocker statements are historical for these two approved subdecisions. No runtime adapters or RLS were added; remaining HUM-006 isolation/context/bootstrap and later runtime gates remain open. No whole human task is closed. Stop for review, without staging/commit/push.