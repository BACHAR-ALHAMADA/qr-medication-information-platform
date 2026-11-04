# Phase 2D PostgreSQL CI integration

Date: 2026-11-03 (America/Winnipeg). Status: implemented and locally verified; uncommitted review gate. Authority: explicit Product Owner request for disposable PostgreSQL CI. Trace: T-014/T-015, AI-009/AI-010, HUM-008/HUM-014, ADR-005/ADR-006. Scope remains synthetic testing only.

## Safety checkpoint

- Branch `feature/postgresql-foundation`, HEAD `aed89dc3af7d36f72e3ca6a4d21dd4ac1c0d6f1a`; live `git ls-remote` and Draft PR #3 metadata confirm the same remote head. PR remains draft/open. No branch change, staging, commit, push or PR mutation.
- Initial tree contained only untracked `docs/NEXT-SESSION.md`, an older handoff; preserved along with the existing safety stash. Local `.env` and `.env.roles` were neither changed nor used by disposable test runs.
- Reviewed AGENTS.md, Phase 2D reports, ADR-001 through ADR-006, CI, requirements conventions and all four trackers. The current request supersedes historical scope prohibitions only for this CI work.
- Starting installed Docker Desktop made Engine 29.9.2 available. No developer database container was running; TCP port 55432 was unavailable. No existing database/volume was started, migrated or deleted. SDK 10.0.402 selected correctly.
- The old handoff's exact backup-manifest path was absent. No backup was deleted or altered; integrity at that path cannot be verified. See HUM-008 follow-up.

## Implementation and security

The independent `PostgreSQL integration` job preserves Backend and Frontend jobs. It provisions `qr_medication_ci` on a GitHub-hosted Ubuntu service with a dynamically assigned port and no persistent volume mapping. The existing reviewed PostgreSQL `18.6-bookworm` image and SHA-256 digest are reused exactly. A new cluster avoids collisions with migrations' deliberately non-adoptable cluster roles.

Service initialization reads a random initial password from Linux `/proc/sys/kernel/random/uuid`, avoiding a literal password in service startup arguments/logs. TCP uses SCRAM. Setup generates four independent 32-byte random passwords, masks each before writing it to the runner environment, and rotates the admin password through container-local administrative socket access. Hex-only generated SQL goes over stdin with password-statement/error logging suppressed. No repository secret, developer credential or password-bearing CLI argument is required. Container-local administrative socket access is used only for setup; no TCP trust authentication is enabled.

`QR_DATABASE_MODE=ci` selects fixed loopback host/database/admin names, an explicit port and required environment passwords. It never reads/writes developer credential files. Unknown modes and CI without explicit mode fail closed. Existing local mode remains available outside CI.

Explicit `ci-bootstrap` rejects existing application/history schemas, applies both existing EF migrations, then provisions separate migrator, privileged synthetic setup and restricted reader passwords. Independent `apply` verifies actual migrator authentication with SET ROLE owner. CI checks rebootstrap rejection. No API-startup migration or runtime adapter is added.

BUG-008/FIX-008 removes the hard-coded developer database privilege target using `current_database()` and `%I` identifier quoting; review SQL is synchronized. Existing migration IDs, policies, grants and FORCE RLS are preserved. Existing developer databases already received correct privileges, so no corrective migration is required there.

Sources reviewed: [GitHub PostgreSQL service guidance](https://docs.github.com/en/actions/tutorials/use-containerized-services/create-postgresql-service-containers), [official PostgreSQL image documentation](https://hub.docker.com/_/postgres). Hosted orchestration remains unverified.

## Verification

| Check | Result |
|---|---|
| Explicit public-source solution and persistence restores | Passed |
| Solution and persistence/setup Release builds | Passed; zero warnings/errors |
| Database-free backend tests | 106 unit + 8 API = 114 passed; zero failed/skipped |
| Fresh disposable bootstrap | Passed twice in independent new clusters; both migrations applied |
| Separate migrator authentication/history check | Passed |
| PostgreSQL tests | 59 passed; zero failed/skipped, including migration count, FORCE RLS and role restrictions |
| Existing-schema rebootstrap rejection | Passed; nonzero sanitized failure as expected |
| Missing CI configuration / CI local-file fallback | Both rejected with sanitized nonzero failures |
| Frontend TypeScript / lint / build | All passed |
| Frontend tests | 28 passed |
| Workflow | Checksum-verified actionlint 1.7.12 passed; unavailable ShellCheck/Pyflakes disabled |
| Whitespace | `git diff --check` passed |
| Secret review | Actual local credentials: zero matches across 147 shareable files; credential files/helper remain ignored; no 64-hex credential-shaped values found in final container logs |

Backend total remains **173 passing tests**; no new xUnit cases are counted. Initial sandbox MSBuild/Node process failures were resolved by permitted execution outside the sandbox; results above are completed reruns. Frontend used existing installed dependencies; clean `npm ci` and GitHub setup actions were not rerun locally.

Both newly created verification containers were stopped and preserved under `qr-ci-verification-*`. The ignored `TestResults/verify-ci.ps1` helper generates credentials only in process memory. No pre-existing resource was removed.

## Remaining review and limits

- Review the uncommitted diff, then separately authorize commit/push and inspect hosted PostgreSQL, Backend and Frontend jobs on PR #3. Local Windows execution does not establish hosted Linux success. Required-check policy and draft/merge approval remain owner responsibilities.
- Independent backup restore/recovery is unimplemented and untested. Fresh initialization is not recovery evidence. Destructive Down remains guarded. Resolve synthetic recovery ownership/scope before that exercise.
- ADR-006 limits remain: arbitrary SQL under compromised reader credentials can forge context; Repeatable Read can retain a pre-revocation snapshot. Production identity binding, authentication, tenant isolation, secret/process separation and deployment readiness are not established.
- Future EF writer revision-refresh, named-constraint completeness and multi-writer locking coverage remain deferred under AI-010. No login, protected endpoint, patient data/feature, clinical claim or frontend change was added.

## Approved publication safety review — 2026-11-03

The Product Owner conditionally authorized one scoped commit and a normal push after final checks, with PR #3 remaining draft. Live remote/HEAD synchronization was reconfirmed. The complete 12-file tracked diff and this report were reviewed. Generated `rls-script` output matches the reviewed SQL after newline/outer-whitespace normalization. A third fresh disposable cluster passed both migrations, distinct migrator authentication, all 59 real PostgreSQL cases and existing-schema rejection. Workflow validation passed again.

Read-only backup investigation outside sandbox restrictions confirmed that the recorded temporary backup directory exists but is empty: its seven historical file copies and manifest cannot be verified. The separate Git safety stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1` is readable, retains six tracked planning files and the untracked planning document, and repository object checks reported no corruption. This verifies available stash contents, not the missing historical file-backup hashes or recovery behavior. Neither backup directory nor stash was modified. Selective staging and a non-force push do not remove these resources or the excluded local handoff.
