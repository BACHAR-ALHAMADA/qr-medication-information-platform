# Phase 2D-1B PostgreSQL foundation implementation

Date: 2026-10-25 (America/Winnipeg). Status: implemented locally, review gate; Phase 2D remains incomplete. Authority: Product Owner bounded schema/migration/test authorization plus explicit FK and trigger decisions. Trace: [ADR-005](decisions/ADR-005-postgresql-foundation-integrity.md), AI-010, HUM-006/HUM-008, REQ-006/REQ-008/REQ-009, T-015 schema/revision subset only. The earlier [schema review blocker](phase2d-1b-schema-review.md) is superseded for the two explicitly approved decisions.

## Implementation

Seven application tables: `identity.staff_users`; `organizations.organizations`, `branches`, `memberships`, `membership_branch_grants`, `capabilities`, `membership_capabilities`. EF's migration-history table is separate in schema `foundation`.

Identity and Organizations have separate Infrastructure projects, DbContexts and persistence-only model configuration contracts. Shared-type dictionary entities keep this initial schema-only mapping internal to persistence use; Domain/Application projects are unchanged and receive no EF dependency. A standalone `tools/DatabaseSetup` composition tool owns the single initial migration and snapshot. This intentionally reconciles the requested one-migration slice with the older proposal's separate histories; future module extraction requires coordinated migration ownership work. The approved cross-module FK is explicit migration SQL, absent from EF navigations; future migrations must preserve/review this FK and custom trigger SQL, which EF model snapshots do not represent.

Constraints include composite tenant-aware membership/branch FKs, restrictive deletes, tenant-scoped primary-key uniqueness, nonempty UUIDs, checked lifecycle/capability codes, positive revisions, 160-character labels and nondelegable scope-evaluation capability. Overlapping branch references and labels across organizations are allowed. Reverse indexes cover `(user_id,organization_id)` memberships and `(organization_id,branch_id,user_id)` branch grants; EF also generates a capability-FK index. No performance/load claim follows.

Revision semantics, role ownership and lifecycle restrictions are recorded in ADR-005. Membership insert requires explicit revision one. All membership updates and grant insert/update/delete operations increment; stale values fail optimistic concurrency predicates. Removed memberships cannot be physically deleted; grant reference changes require delete/insert, while delegability changes use UPDATE. No-op UPDATE also increments. Grant-trigger updates serialize on the parent row; a dedicated multi-session concurrency/deadlock stress test has not been performed. No immediate revocation guarantee is claimed.

## Versions and migration review

Restored direct versions: EF Core, Relational and Design **10.0.13**; Npgsql EF provider and Npgsql driver **10.0.3**. Local `dotnet-ef` manifest pins **10.0.13**. Restore/build resolved these exact versions without compatibility warnings; build treats warnings as errors. Provider metadata permits EF >=10.0.4 and <11, Npgsql >=10.0.3 on net10.0. These are the explicitly authorized versions, not a claim to the newest servicing release: tool restore reported 10.0.14 available. Revisit servicing deliberately in a subsequent review.

`dotnet list tools/DatabaseSetup/DatabaseSetup.csproj package --vulnerable --include-transitive` reported no known vulnerable packages from the configured sources. That is a point-in-time feed result, not proof of absence of vulnerabilities or an image security audit. [Provider metadata](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3), [EF package](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.13).

Reviewed migration `20261026033754_InitialFoundation` (generated UTC timestamp; local review date remains October 25). The reviewable complete SQL is [initial-foundation.sql](../infrastructure/development/initial-foundation.sql). Review confirmed seven tables plus EF history, restrictive FK ordering, two capability catalogue rows only, no patient/staff fixtures, exact trigger privilege grants/revokes, fixed search paths, no RLS, no destructive Up operations. Custom SQL creates the approved restrictive Identity FK, four trigger functions and seven triggers. Down deliberately refuses destructive rollback. The SQL artifact is a review artifact, not an idempotent reapply command; use the EF runner.

Applied once to the previously empty `127.0.0.1:55432/qr_medication_dev`, PostgreSQL 18.6; no other database was created or migrated. Local setup administrator performs DDL only and is never registered as an application runtime identity. The application has no dependency on these projects or database availability. The runner reads only ignored `infrastructure/development/.env`, fixes the host to loopback and rejects database/port values other than the approved development target. It does not accept arbitrary connection-string arguments. Existing Compose volume and credentials are unchanged.

## Reproduction commands

Run from repository root with healthy local Compose PostgreSQL; keep credentials out of CLI arguments and shared output. The setup tool does not run from API startup.

```powershell
dotnet tool restore
dotnet restore tools/DatabaseSetup/DatabaseSetup.csproj
dotnet restore tests/Persistence/Foundation.PersistenceTests.csproj
dotnet run --project tools/DatabaseSetup -- script
dotnet run --project tools/DatabaseSetup -- apply
dotnet test tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-restore
dotnet test MedicationInformationPlatform.sln --configuration Release --no-restore
```

`apply` uses EF migration history; missing database/tooling causes failure rather than silent skip. The local-only persistence test project is deliberately outside the database-free solution/CI invocation; explicitly run its command. Existing Backend CI therefore acquires no hidden PostgreSQL prerequisite. No new persistence CI job is implemented.

## Executed evidence

- Pinned restore, EF migration generation and setup build succeeded with zero warnings/errors. The complete SQL was generated and reviewed before application. Initial migration applied successfully to the empty development schema.
- Real PostgreSQL tests: **27 passed, 0 failed, 0 skipped**. Coverage: applied migration/model agreement and seven tables, seeded catalogue, index presence, lifecycle/null/length/UUID checks, restrictive deletes and uniqueness, missing user/organization, cross-tenant branch/capability association rejection, delegability checks, membership/grant ownership immutability, all grant mutation types, lifecycle transitions, multiple changes, stale SQL revision and actual EF concurrency-token failure, savepoint/full transaction rollback, tombstones/TRUNCATE prevention, atomic overflow failure, nonowner DML and forbidden revision rewrites/trigger disabling/role assumption/function calls/schema creation, maintainer flags/ownership/search_path.
- Existing backend tests: **106 unit + 8 mocked/in-process API integration = 114 passed**, zero failed/skipped. Combined current total: **141 passed**.
- Initial test run had three test-harness/expectation failures, all corrected: RESTRICT SQLSTATE is 23001; the ownership-move test must first supply an otherwise valid destination FK; a superuser session's SET ROLE cannot prove role-assumption denial. The final probe uses transaction-local SESSION AUTHORIZATION for a NOLOGIN nonprivileged test role, exercising both session and effective identity. See BUG-007/FIX-007 for that meaningful security-test correction.
- Fixtures use synthetic UUIDs/labels only and roll back. Verification after tests: zero staff users, organizations, memberships and surviving test-probe roles; only the two catalogue rows remain. Overflow preparation disables/re-enables one trigger as privileged test setup within a rolled-back transaction; normal probe writes cannot disable it. No real records or patient data used.
- PostgreSQL remains healthy and bound only to `127.0.0.1:55432`. No frontend checks rerun because frontend/API behavior and dependencies are unchanged.

## Remaining security gates

No RLS, runtime authorization reader, login, protected endpoint, patient record, invitation or grant-management feature exists. These are schema-integrity/revision tests, not runtime authorization or tenant row-visibility evidence. Most T-015 authorization/RLS/pooling/recovery coverage remains planned. Review trusted context and arbitrary-SQL threat limits, self-membership bootstrap visibility, runtime/owner/provisioner separation, FORCE RLS maintenance policy, backup/restore and future runtime wiring before introducing them. Tables are locally admin-owned; Docker, constraints and triggers do not establish production security.

Initial branch/HEAD and safety stash are unchanged: `feature/postgresql-foundation`, `8ea0c662e2de635f6cd6265d8814de90aa7b917f`, stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1`. All seven existing backup hashes matched their manifest; no existing planning documents were overwritten. Changes to existing docs/trackers are appended. No staging, commit, push, merge, branch switch, deletion, frontend or existing application-code change. Stop for implementation review; no additional scope is authorized by this record.

## Exact final Git status

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
?? docs/phase2d-1-safety-review.md
?? docs/phase2d-1a-local-postgresql.md
?? docs/phase2d-1b-postgresql-foundation.md
?? docs/phase2d-1b-schema-review.md
?? docs/phase2d-postgresql-tenant-isolation-plan.md
?? infrastructure/development/.env.example
?? infrastructure/development/compose.yaml
?? infrastructure/development/initial-foundation.sql
?? src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj
?? src/Modules/Identity/Infrastructure/IdentityDbContext.cs
?? src/Modules/Organizations/Infrastructure/Organizations.Infrastructure.csproj
?? src/Modules/Organizations/Infrastructure/OrganizationsDbContext.cs
?? tests/Persistence/Foundation.PersistenceTests.csproj
?? tests/Persistence/SchemaTests.cs
?? tools/DatabaseSetup/DatabaseSetup.csproj
?? tools/DatabaseSetup/FoundationDbContext.cs
?? tools/DatabaseSetup/Migrations/20261026033754_InitialFoundation.Designer.cs
?? tools/DatabaseSetup/Migrations/20261026033754_InitialFoundation.cs
?? tools/DatabaseSetup/Migrations/FoundationDbContextModelSnapshot.cs
?? tools/DatabaseSetup/Migrations/InitialFoundationSql.cs
?? tools/DatabaseSetup/Program.cs
```

8 modified tracked files; 24 untracked files; nothing staged. Existing ignored local credentials remain ignored. This inventory includes preserved files from earlier sessions.

## Subsequent checkpoint

The schema-only state and original commands above are historical. Phase 2D-1C adds an explicit administrative `bootstrap` command and a distinct migrator-backed `apply` command, FORCE RLS and a trusted synthetic transaction harness. See [current local evidence and operating commands](phase2d-1c-rls-tenant-context.md); no production actor binding or live reader adapter is introduced.