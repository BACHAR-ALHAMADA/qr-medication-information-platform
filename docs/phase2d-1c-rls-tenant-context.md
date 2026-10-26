# Phase 2D-1C: local PostgreSQL RLS and tenant context

Date: 2026-10-25 (America/Winnipeg). Status: implemented locally; stopped at implementation review gate. Authority: explicit Product Owner approval for LOCAL DEVELOPMENT AND TESTING ONLY. Trace: [ADR-006](decisions/ADR-006-local-postgresql-rls-context.md), ADR-005, AI-010, HUM-006/HUM-008, REQ-006/REQ-007/REQ-008, T-015 subset. Earlier schema-only/no-RLS records are historical. Domain/Application behavior, frontend and public DIN API are unchanged.

## Roles and credentials

| Role | Scope |
|---|---|
| `qr_platform_owner` | NOLOGIN, NOSUPERUSER, NOBYPASSRLS; owns seven tables, schemas, EF history, invoker guards and context helpers. FORCE RLS applies to reads; ownership still allows privileged DDL/policy changes. |
| `qr_migrator` | Separate LOGIN, NOINHERIT, NOSUPERUSER, NOBYPASSRLS, no CREATEDB/CREATEROLE; may explicitly SET ROLE to owner for reviewed offline migrations. Not an application role. |
| `qr_setup` | Separate LOGIN, NOSUPERUSER, BYPASSRLS synthetic fixture provisioner. SELECT on application tables; INSERT/UPDATE/DELETE on named fixture tables; membership INSERT and status-only UPDATE. No capability-catalogue writes, DDL, TRUNCATE or elevated memberships. Never runtime. |
| `qr_reader` | Separate LOGIN, SELECT-only, NOINHERIT/NOSUPERUSER/NOBYPASSRLS, no CREATEDB/CREATEROLE/replication/role memberships/ownership. No DML, TRIGGER, TRUNCATE, schema CREATE, TEMP or EF-history SELECT. Default read-only transactions are defense in depth; tests also turn that default off and verify privileges still reject writes. |
| `qr_revision_maintainer` | Existing NOLOGIN/NOSUPERUSER/NOBYPASSRLS role; no members, only membership keys/revision SELECT and revision-column UPDATE. Owns the sole SECURITY DEFINER trigger function. |
| `qr_dev_admin` | Existing local bootstrap administrator only; never used for an application request or runtime reader. |

Generated random reader/setup/migrator passwords are in ignored `infrastructure/development/.env.roles`; the existing admin `.env` is unchanged. Passwords were not displayed, embedded in migration SQL, or passed as CLI arguments. Bootstrap uses fixed allowlisted role names, parameterized server quoting, no Npgsql parameter logging, suppressed password-statement/error diagnostic logging and sanitized tool error output. Existing role credentials are reused on explicit bootstrap rerun; this is not a production secret manager. Test configuration resides in the setup tool and can read local admin configuration; no claim of production process/filesystem secret isolation is made.

## Migration and policy review

Initial migration/source/SQL remain preserved. Added `20261026040035_TenantRowSecurity`; reviewable SQL is [tenant-row-security.sql](../infrastructure/development/tenant-row-security.sql). It creates the named roles, role-targeted grants, two invoker context helpers, RLS flags/policies and ownership/default-privilege changes in one migration transaction. Applied only to `127.0.0.1:55432/qr_medication_dev`; no other database, cloud service or deployment was used. Migration history records both migrations at EF 10.0.13. The SQL artifact is not an idempotent reapply command; use EF history through the offline tool.

Six tables have both ENABLE and FORCE RLS: staff_users, organizations, branches, memberships, membership_branch_grants and membership_capabilities. Capability definitions are shared non-sensitive metadata, SELECT-only for the reader. EF history is inaccessible to reader. There are eight policies:

| Policy | Reader/maintenance behavior |
|---|---|
| `staff_self` | Exact actor only; disabled identity state is readable internally for denial. Requires all three valid context values. |
| `membership_self` | Exact actor plus one candidate organization, including inactive states. No self-query or organization-policy dependency; no unrestricted discovery. |
| `organization_self` | Candidate organization, enabled self identity and active self membership. Suspended/closed organization state is readable internally for rejection. |
| `branch_grants_self` | Actor's own grants in the candidate organization, with active organization and the organization policy's enabled-user/active-membership checks. This returns self grant references, not other branch rows. |
| `capabilities_self` | Actor's own capability rows under the same active-organization conditions. It does not grant a product permission by itself. |
| `branch_scope` | Exact candidate tenant/branch with a visible explicit self grant. An enabled flag of false is internal denial metadata; Domain must reject it. |
| `revision_read` | SELECT for the nonlogin maintainer only; permits the existing trigger's exact-parent lookup under FORCE RLS. |
| `revision_update` | UPDATE with explicit USING/WITH CHECK for maintainer only. Column ACL permits revision changes only; the membership guard calculates the increment. No caller/runtime can enter this role or call its function. |

Policies are SELECT-only for runtime: no runtime INSERT/UPDATE/DELETE policy or privilege. Maintenance is a separate intentionally privileged operation, not tenant runtime access. No broad test-only RLS DML policy was introduced to make tests pass. Default PUBLIC grants are removed, and owner-created future tables/functions receive no automatic PUBLIC rights; future protected tables still need explicit RLS review.

## Context and function audit

`src/Infrastructure/PostgreSql/ReadOnlyTenantSession.cs` supplies only a technical transaction scope, used by the trusted synthetic harness. It rejects an administrative datasource, requires the dedicated reader session identity and explicit nonzero candidate references, begins Repeatable Read, sets READ ONLY and parameterizes three transaction-local settings. It creates no authorization result and implements no Identity/Organizations reader adapter. The API references neither this project nor DatabaseSetup.

`context_uuid(text)` accepts only the three known setting names, returns NULL for missing/empty/invalid/zero UUIDs, and catches invalid text without echoing it. `context_ready()` requires all three. Both are stable SECURITY INVOKER functions. Their dependency graph is acyclic: user/membership -> organization -> grants/capabilities -> branch. Every protected read denies absent/malformed context; the two-row shared catalogue intentionally remains readable.

All six user functions have `search_path=pg_catalog`; all application table references in privileged SQL are schema-qualified. The sole SECURITY DEFINER routine remains `organizations.bump_grant_revision()`, owned by the constrained maintainer, with no PUBLIC EXECUTE, no arguments, no dynamic SQL and no caller-selected parent. The other guards and helpers use invoker rights; only context helpers are executable by reader. Catalogue inspection and tests verify these properties, role memberships and column ACLs.

No production actor authentication/binding exists. A harness-issued actor reference is not evidence that a client owns that identity. The test `ContextForgeryIsAnExplicitThreatLimitNotAnAuthenticationClaim` deliberately changes actor context through arbitrary reader SQL and shows impersonation of another synthetic actor is possible. This accepted limitation requires parameterization, trusted server composition, least privilege and a separately reviewed production identity flow; RLS/context alone does not solve compromised runtime credentials.

## Snapshot, cleanup and test evidence

The session must be short-lived, fully materialized and disposed before connection return; no HTTP lifetime, transaction TTL or global revocation service is implemented. Commit is explicit, otherwise disposal rolls back. Cleanup has an independent five-second cancellation bound, clears an uncertain pool and disposes transaction/connection; normal Npgsql reset remains enabled. No tenant singleton, context pooling, multiplexing or automatic retry was added.

A deterministic test first establishes snapshot A, commits a membership suspension on the separate setup connection, then proves the existing read transaction still sees its earlier rows/revision. A new read transaction sees suspension, denies descendant reads and fails an explicit stale-revision predicate; tenant B's independent membership remains available. RLS does not compare a caller's expected revision automatically: the unchanged Application/Domain rules remain responsible for current-state/capability/revision evaluation. Already-running requests cannot be immediately revoked by this mechanism.

Executed:

- Build/restore of the pinned .NET 10/EF 10.0.13/Npgsql 10.0.3 graph succeeded; no warnings/errors.
- Reviewed migration applied via explicit local bootstrap; actual separate migrator login successfully ran the migration-history check with SET ROLE owner. No API-startup migration exists.
- **59 PostgreSQL tests passed, 0 failed/skipped**: retained 27 schema/revision tests (updated for two migrations, FORCE RLS and the setup-only privilege probe), plus 32 RLS/context cases. Coverage includes genuine reader authentication/flags/ACLs; all protected tables rejecting missing/malformed/zero context; cross-tenant raw/nested/join reads; branch substitution; self-only inactive bootstrap; disabled identity/branch and suspended/closed organization handling; stale revisions and RR revocation timing; maintainer updates/rollback; owner FORCE RLS; forbidden writes/DDL/role assumption; concurrent independent read scopes; context-forgery negative control; and accidental admin datasource rejection.
- Max-pool-size-one tests capture and compare actual backend PIDs across A, B and unset scopes after commit, rollback, failed SQL and in-flight command cancellation. The same physical connector is reused in all four cases. Cancellation waits until the server reports the target query running, then cancels it; cleanup is not given the cancelled request token.
- **114 existing backend tests passed** (106 unit + 8 mocked/in-process API integration). Combined total **173 passed**, zero failed/skipped. Frontend unchanged; no frontend checks rerun.
- Catalog verification after tests: two migration records, six ENABLE/FORCE tables, eight policies, five nonadmin foundation roles with only migrator->owner membership, six fixed-search-path functions with one definer, no disabled custom triggers, zero synthetic users/organizations/branches/memberships/grants/capability assignments; only two catalogue rows remain. PostgreSQL remains healthy and loopback-only.

The suite uses this approved local development database with isolated random fixture UUIDs and serialized test classes. Most schema tests roll back. The RLS fixture commits synthetic states so independent reader/setup transactions can test revocation and snapshots; final cleanup uses the admin only to remove those allocated fixture references, temporarily disabling/re-enabling the tombstone guard within one transaction. It performs no TRUNCATE/DROP/unrelated row deletion. This is explicit test maintenance, never a runtime deletion path. A separate disposable-database CI/bootstrap/restore exercise remains unimplemented; role creation is cluster-scoped and deliberately refuses pre-existing role collisions.

Prior final-review limitations remain explicit: named-constraint rejection assertions are not exhaustive, a fresh full schema bootstrap/independent recovery is not exercised by the current existing-database suite, and trigger-generated revision refresh after successful/repeated EF writes is not implemented/tested. No EF writer or live authorization reader is introduced. Multi-session read isolation is tested; multi-writer locking/deadlock/retry behavior remains outside this SELECT-only slice.

## Commands and remaining gate

Run from repository root with local Compose PostgreSQL healthy:

```powershell
dotnet tool restore
dotnet restore tests/Persistence/Foundation.PersistenceTests.csproj
dotnet run --project tools/DatabaseSetup -- rls-script
# Explicit local administrative role/bootstrap operation; never API startup:
dotnet run --project tools/DatabaseSetup -- bootstrap
# Separate migrator login, explicitly assumes nonlogin owner:
dotnet run --project tools/DatabaseSetup -- apply
dotnet test tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-restore
dotnet test MedicationInformationPlatform.sln --configuration Release --no-restore
```

Bootstrap initializes/reuses ignored role credentials without displaying them. Do not print `.env`, `.env.roles`, expanded Compose configuration or password-bearing connection strings. The persistence suite remains outside the database-free solution/CI invocation and must be selected explicitly; missing credentials/DB fail rather than skip. SQL Down refuses RLS removal pending separately reviewed recovery.

Review gate: local-only approved mechanisms are implemented. Production actor binding/secret isolation, runtime adapters/wiring, protected endpoints, provider authentication, invitation/permission workflows, real data, production hardening/hosting and expanded test/CI/recovery scope still need separate approval. No additional local security decision blocked this slice. No production-secure or immediate-revocation claim is made. See [PostgreSQL RLS](https://www.postgresql.org/docs/18/ddl-rowsecurity.html), [transaction-local settings](https://www.postgresql.org/docs/18/functions-admin.html#FUNCTIONS-ADMIN-SET), [snapshot semantics](https://www.postgresql.org/docs/18/transaction-iso.html) and [Npgsql pooling/transactions](https://www.npgsql.org/doc/basic-usage.html).

## Historical pre-publication preservation and Git inventory

The inventory below records the completed local review before publication. On 2026-10-26 the Product Owner authorized the reviewed three-commit checkpoint, non-force branch push and Draft PR. Local Release builds and all 173 backend/PostgreSQL tests passed again. Hosted Backend/Frontend checks do not exercise PostgreSQL; actual PR run results must be verified separately. docs/NEXT-SESSION.md and all local credentials, volumes, caches and backups remain excluded from commits.

Branch remains feature/postgresql-foundation; HEAD 8ea0c662e2de635f6cd6265d8814de90aa7b917f and safety stash c6cbf3cdeb80ee26866769434d20c06b76a042e1 are unchanged. All seven original backups match their manifest. Of the 32 initially changed/untracked files, 21 retain identical hashes; 11 received the authorized setup/test changes or appended documentation. The original schema migration, snapshot, initial SQL artifact, module models, accepted ADRs and planning proposal retain identical hashes. No unrelated work was removed. No new confirmed bug was established; existing BUGS/FIXES changes are preserved from the earlier session.

Generated RLS SQL matches the runner output after newline/outer-whitespace normalization. All four actual local passwords were checked against shareable working files with zero matches; both credential files are ignored. No stage/commit/push/merge/branch switch occurred.

```text
## feature/postgresql-foundation
 M docs/project-roadmap.md
 M docs/project-status.md
 M docs/security/authorization.md
 M docs/testing-strategy.md
 M tasks/AI-TODO.md
 M tasks/BUGS.md
 M tasks/FIXES.md
 M tasks/HUMAN-TODO.md
?? .config/dotnet-tools.json
?? docs/NEXT-SESSION.md
?? docs/decisions/ADR-005-postgresql-foundation-integrity.md
?? docs/decisions/ADR-006-local-postgresql-rls-context.md
?? docs/phase2d-1-safety-review.md
?? docs/phase2d-1a-local-postgresql.md
?? docs/phase2d-1b-postgresql-foundation.md
?? docs/phase2d-1b-schema-review.md
?? docs/phase2d-1c-rls-tenant-context.md
?? docs/phase2d-postgresql-tenant-isolation-plan.md
?? infrastructure/development/.env.example
?? infrastructure/development/compose.yaml
?? infrastructure/development/initial-foundation.sql
?? infrastructure/development/tenant-row-security.sql
?? src/Infrastructure/PostgreSql/PostgreSql.Infrastructure.csproj
?? src/Infrastructure/PostgreSql/ReadOnlyTenantSession.cs
?? src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj
?? src/Modules/Identity/Infrastructure/IdentityDbContext.cs
?? src/Modules/Organizations/Infrastructure/Organizations.Infrastructure.csproj
?? src/Modules/Organizations/Infrastructure/OrganizationsDbContext.cs
?? tests/Persistence/AssemblyInfo.cs
?? tests/Persistence/Foundation.PersistenceTests.csproj
?? tests/Persistence/RlsFixture.cs
?? tests/Persistence/RlsTests.cs
?? tests/Persistence/SchemaTests.cs
?? tools/DatabaseSetup/DatabaseSetup.csproj
?? tools/DatabaseSetup/FoundationDbContext.cs
?? tools/DatabaseSetup/Migrations/20261026033754_InitialFoundation.Designer.cs
?? tools/DatabaseSetup/Migrations/20261026033754_InitialFoundation.cs
?? tools/DatabaseSetup/Migrations/20261026040035_TenantRowSecurity.Designer.cs
?? tools/DatabaseSetup/Migrations/20261026040035_TenantRowSecurity.cs
?? tools/DatabaseSetup/Migrations/FoundationDbContextModelSnapshot.cs
?? tools/DatabaseSetup/Migrations/InitialFoundationSql.cs
?? tools/DatabaseSetup/Migrations/TenantRowSecuritySql.cs
?? tools/DatabaseSetup/Program.cs
```

8 modified tracked files; 35 untracked files; nothing staged. This includes preserved changes from earlier sessions. Ignored build outputs and local credential files are not included.
