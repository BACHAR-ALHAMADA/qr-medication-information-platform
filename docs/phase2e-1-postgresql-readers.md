# Phase 2E-1: PostgreSQL authorization readers

Date: 2026-11-11 (America/Winnipeg). Status: security review and regression reruns passed; reviewed commit/push/Draft PR conditionally authorized, with hosted publication evidence pending at this checkpoint. Authority: explicit Phase 2E-1 instruction authorizing a safe branch transition, synthetic reader implementation and regressions. Trace: F-002, REQ-006/REQ-007/REQ-008/REQ-009, T-013/T-015, AI-010, ADR-004/ADR-005/ADR-006. No broader release or production authorization follows.

## Verified starting point and preservation

Fetched `origin/main` and verified merge commit `88c83f1e2af593ce6ffccae6937859cb7e6c535d`. Post-merge [main CI run 43109234429](https://github.com/BACHAR-ALHAMADA/qr-medication-information-platform/actions/runs/43109234429) passed Backend, Frontend and PostgreSQL integration. Created `feature/postgresql-authorization-readers` directly from that commit without moving local `main` or changing the former feature branch.

Initial working tree contained only untracked `docs/NEXT-SESSION.md`. The handoff, ignored `.env`/`.env.roles`, safety stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1`, existing containers/volumes and recovery artifacts were preserved. The November 3 dump/globals hashes match the [recovery record](phase2d-backup-restore.md); the historical empty planning-backup directory is not validated by those artifacts. During initial implementation, no reset, clean, stash application, migration, credential rotation, staging, commit, push or PR was performed.

## Reader and module boundaries

- `Identity.Infrastructure.PostgreSqlStaffIdentityReader` implements the unchanged `IStaffIdentityReader`. It reads only `identity.staff_users`, selecting the exact user and enabled flag. It neither queries Organizations tables nor treats identity existence as authorization.
- `Organizations.Infrastructure.PostgreSqlOrganizationAccessReader` implements the unchanged `IOrganizationAccessReader`. One parameterized SELECT materializes the exact self-membership, organization lifecycle, candidate branch, all visible self branch-grant references, capabilities, delegable flags and current database revision. All data-table reads belong to Organizations; the approved physical Identity FK remains migration-only.
- Both Infrastructure projects reference their own Application contracts and the shared PostgreSQL technical project. Domain/Application code and contracts are unchanged. The setup tool retains its existing offline migration responsibilities; no adapter is registered in the API.
- EF schema models remain unchanged. These read-only adapters use Npgsql commands on the existing session's connection/transaction, avoiding a second DbContext connection or an independent snapshot. No new package/version is introduced.

Unknown lifecycle/capability codes, invalid revisions and empty branch references deny rather than acquire authority. Existing database constraints make these invalid states unreachable through ordinary writers; no schema weakening was used to test them. Inactive memberships can remain internal denial metadata with null organization/branch and empty descendant grants. Returned records and Domain membership sets are fully materialized and immutable; there is no tracked entity, lazy query or positive authorization cache.

## Trusted synthetic composition

The harness creates one `ReadOnlyTenantSession` using `qr_reader`, a `TrustedSyntheticActor` and explicit nonzero candidate organization/branch. The session retains those original IDs. `MatchesScopeAsync` rejects mismatched arguments, completed/disposed sessions and absent/malformed/changed database context before a reader's data SELECT. It compares the three current context values with parameterized UUIDs. Identity checks its actor against the same original candidate organization/branch. RLS continues to apply independently.

The test composition is deliberately explicit:

```csharp
await using var session = await ReadOnlyTenantSession.OpenAsync(
    readerDataSource, TrustedSyntheticActor.FromTrustedHarness(actorId), organizationId, branchId);
var authorization = new StaffAuthorization(
    new PostgreSqlStaffIdentityReader(session),
    new PostgreSqlOrganizationAccessReader(session));
var result = await authorization.AuthorizeAsync(request, cancellationToken);
// Inspect/materialize the result within this invocation; do not cache it as future authority.
// Commit a completed read explicitly if desired; disposal otherwise rolls back.
```

Both readers run sequentially on the same read-only Repeatable Read transaction. The caller owns session lifetime; readers neither open connections nor commit/rollback independently. No same-session concurrent operations, automatic retry, singleton tenant state, context pooling or multiplexing is introduced. Independent invocations require independent sessions. Existing bounded rollback and pool cleanup behavior is reused. Database failures and cancellation propagate as exceptions, never successful decisions; scope mismatch/missing rows return null.

The guard detects accidental context substitution in this trusted harness. It does not make descriptive settings an identity mechanism or protect against arbitrary SQL under compromised credentials. The raw-SQL forgery demonstration remains in the existing suite. Application/Domain checks still own lifecycle, exact ownership, capabilities and expected-revision decisions.

An existing Repeatable Read snapshot can retain enabled identity, membership and grants after a separate transaction commits revocation. New transactions observe the committed change. Tests demonstrate both behaviors without claiming immediate revocation. Actor delegation-scope evaluation still does not read or establish recipient eligibility, create a grant or confer administration authority.

## Executed verification

| Check | Result on 2026-11-11 |
|---|---|
| Existing pinned dependency restore for added project-reference graph | Passed; no package/version changes |
| Database-free Release build | Passed; zero warnings/errors |
| Persistence/setup/reader Release build | Passed; zero warnings/errors |
| Existing backend suite | 106 unit + 8 API = **114 passed**, zero failed/skipped |
| Existing PostgreSQL suite plus new readers | 59 existing + 51 new = **110 passed**, zero failed/skipped |
| Backend total | **224 passed**, zero failed/skipped |
| Frontend standalone TypeScript, zero-warning lint, production build | Passed with existing installed dependencies, Node 24.22.0 |
| Frontend tests | **28 passed** |

The initial database run had 108 passes and two failures from a new test's incorrect assumption that frozen sets do not implement `ISet`. The corrected test checks `IsReadOnly` and actual mutation rejection. The full 110-case database suite then passed. No existing application defect or change to Domain immutability was implicated.

Post-suite SQL inspection confirmed the development container remains healthy on its original volume, six staff/tenant tables are empty, the capability catalogue has two rows, both migration IDs remain installed, six tables retain FORCE RLS, eight policies remain and all seven application triggers are enabled. Handoff/credential/dump/globals hashes and the safety stash match the starting checkpoint. Credential scanning found zero actual local credential matches in the changed/new review files; `git diff --check` passed. No critical security defect was identified in this bounded implementation review.

The 51 new cases cover exact immutable mapping and independent tenants; actor/organization/branch substitution; empty/malformed/zero/changed database context; missing/disabled identity and missing/suspended/removed membership; unassigned/disabled branches and organization suspension/closure; branch/capability revocation and stale revisions; actor-only delegation including an unknown recipient; cancellation before each reader and during a deterministically blocked real identity SELECT; rollback/error recovery; and backend-PID-confirmed physical connection reuse after commit, rollback, SQL failure and cancellation. Snapshot tests place committed identity/membership revocation between reads on independent connections. Existing tests retain raw RLS, privilege, migration/model and revision checks. Fixtures use the existing privileged synthetic setup/transactional cleanup; they do not broaden reader privileges.

Commands, from the repository root:

```powershell
dotnet restore tests/Persistence/Foundation.PersistenceTests.csproj --source https://api.nuget.org/v3/index.json
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore
dotnet test MedicationInformationPlatform.sln --configuration Release --no-build --no-restore --verbosity minimal
dotnet build tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-restore
dotnet test tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-build --no-restore --verbosity minimal
```

From `frontend/`: `npm.cmd exec --no -- tsc --noEmit`, `npm.cmd run lint`, `npm.cmd run build`, `npm.cmd test`. No clean install or fresh advisory audit is claimed. Execution used permitted access outside the sandbox for Docker/MSBuild/Node. Local results do not establish hosted execution of reader publication; existing CI already selects the persistence project and includes these cases after publication. Actual hosted results must be assessed on the published head.

## Review boundary

No RLS, roles, migration/schema, public DIN API, frontend, authentication, protected endpoint, patient information or deployment change. No production actor binding or API dependency injection registration. Production secret/process separation, sensitive-write revalidation/audit, recipient eligibility, nonempty historical recovery, off-host recovery and PITR remain outside this slice. Broader T-015 is not complete. Accepted ADRs are preserved; this owner-authorized slice extends their local synthetic boundary only.

Publication review, 2026-11-11: the Product Owner explicitly authorized selective staging, the scoped feature commit, normal push and a dedicated Draft PR only if review and regressions pass. Full review found no critical security defect or blocking coverage gap within the trusted sequential-session contract. Both Release builds, 114 backend tests, 110 PostgreSQL tests, 28 frontend tests and TypeScript/lint/build passed again. No implementation changes were needed. All 17 pending files (including the excluded handoff) were checked: zero actual local credential matches, private-key/token pattern matches or binaries. Do not merge; inspect all three hosted jobs and verify the 110-test PostgreSQL result before claiming hosted success. No whole human tracker entry is marked complete.
