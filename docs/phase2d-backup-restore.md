# Phase 2D local backup and restore verification

Date: 2026-11-03 (America/Winnipeg). Status: successful LOCAL-ONLY synthetic exercise; end-of-day documentation commit/push approved after review. Trace: T-015, AI-010, HUM-008, ADR-005/ADR-006. Explicit Product Owner authorization covers this backup/restore exercise only. The exercise performed no staging, commit, push, branch switch, PR readiness change, merge or deployment. The later end-of-day authorization permits only publishing these documentation changes; no additional development is authorized.

## Source and safety

Branch `feature/postgresql-foundation`, HEAD `3b27aed0fecdf58e2468c5582d82a82e2f456fe0`; initial Git status contained only the excluded untracked `docs/NEXT-SESSION.md`. Existing planning stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1`, credentials, volumes and historical backups were preserved.

Source container `qr-medication-dev-postgres-1` uses the existing pinned PostgreSQL 18.6 image and `qr-medication-dev_postgres_data` volume. It was stopped at entry, then started without recreation; readiness initially rejected connections during startup recovery, then became healthy. Source application tables were inspected by counts only: six staff/tenant tables were empty and the shared capability catalogue contained its two expected rows. Both EF migration records were present. The helper refuses nonempty staff/tenant tables before dumping; this evidence covers the known empty synthetic baseline only.

Source operations were readiness/catalog/count queries, `pg_dump`, `pg_dumpall` and creation of uniquely named dump files under container `/tmp`. No source DDL, application DML, migrations, password changes, trigger disabling or database replacement occurred. Source metadata/data snapshots and local `.env`/`.env.roles` SHA-256 values matched before/after the exercise. Normal server startup and operational telemetry/WAL activity are not a claim of physical byte-for-byte volume immutability.

## Preserved artifacts

Successful run: ignored `TestResults/recovery-5f78a555738f48ac96a5d4c07dc2b542/`.

| Artifact | Size / SHA-256 |
|---|---|
| `development.dump` | 38,115 bytes; `612582384EF74BFA60106F1670DFB1B1FF100C504D7632C37FE04DA29DF1AA56` |
| `globals.sql` | 1,429 bytes; `29DC7966B15B2EF1FCBE5116F8A674DB0BC8508C43778584255889BFA9070B51` |

The folder also retains `SHA256SUMS.txt`, a derived `globals-replay.sql`, sorted source/restored metadata snapshots, the complete `verification-commands.ps1` helper used, and `verification-manifest.json` with file sizes/hashes. The original globals export is unchanged and contains no password verifiers. Backup binaries, scripts and local artifacts remain ignored; none are proposed for commit.

Successful target: `qr-restore-verification-5f78a555738f48ac96a5d4c07dc2b542`, a distinct new PostgreSQL cluster with a fresh Docker-managed volume and random loopback TCP port. It is stopped and preserved. Four earlier diagnostic targets/backups are also preserved; no cleanup/pruning was performed. The development container remains running and healthy.

## Exact procedure

The full executed PowerShell helper is preserved in the artifact folder. The following commands describe its ordered operations; `$source`, `$target`, `$remote`, `$backup` are the source above and unique per-run target/container path/local folder. Each native command was checked for nonzero exit. Password values were generated in process memory and never printed or passed as command arguments.

```powershell
$source = 'qr-medication-dev-postgres-1'
$suffix = [Guid]::NewGuid().ToString('N')
$target = 'qr-restore-verification-' + $suffix
$remote = '/tmp/qr-recovery-' + $suffix
$backup = Join-Path (Get-Location) ('TestResults/recovery-' + $suffix)
New-Item -ItemType Directory -Path $backup
$image = 'postgres:18.6-bookworm@sha256:bbf83594798e144bff365a9c191c5d779f181fb3135902064ab8ff11918020fc'
docker start $source
docker exec $source pg_isready -U qr_dev_admin -d qr_medication_dev
# Proceed only when ready, synthetic baseline checked, and source identity verified.
docker exec $source pg_dump -U qr_dev_admin -d qr_medication_dev --format=custom --file=$remote.dump
docker exec $source pg_dumpall -U qr_dev_admin --globals-only --no-role-passwords --file=$remote.globals.sql
docker cp "${source}:$remote.dump" (Join-Path $backup 'development.dump')
docker cp "${source}:$remote.globals.sql" (Join-Path $backup 'globals.sql')
Get-FileHash (Join-Path $backup 'development.dump'),(Join-Path $backup 'globals.sql') -Algorithm SHA256

docker run -d --name $target -e POSTGRES_USER=qr_dev_admin -e POSTGRES_PASSWORD_FILE=/proc/sys/kernel/random/uuid -e POSTGRES_HOST_AUTH_METHOD=scram-sha-256 -e 'POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256' -p 127.0.0.1::5432 $image
# Wait for target pg_isready; never use the source as the restore destination.
$globals = Get-Content -Raw (Join-Path $backup 'globals.sql')
# Assert exactly one matching CREATE first. The original export is preserved.
$globals -replace '(?m)^CREATE ROLE qr_dev_admin;\r?$', '-- Original grantor precreated in the isolated target.' |
    Set-Content (Join-Path $backup 'globals-replay.sql')
docker cp (Join-Path $backup 'globals-replay.sql') "${target}:/tmp/globals-replay.sql"
docker cp (Join-Path $backup 'development.dump') "${target}:/tmp/development.dump"
docker exec $target psql -X -q -v ON_ERROR_STOP=1 -U qr_dev_admin -d postgres -f /tmp/globals-replay.sql
docker exec $target pg_restore -U qr_dev_admin --dbname=postgres --create --exit-on-error /tmp/development.dump
```

`pg_restore --create` creates the archive's original database name **inside the isolated target cluster**, preserving database ownership/ACLs/configuration. No `--clean`, `--no-owner`, `--no-acl` or `--disable-triggers` was used. Globals must be restored first. No application migration was run to repair or construct the restored schema.

Before test fixtures, the helper compared sorted source/target catalog records: table/index metadata (names, kinds, owners, ACLs and table RLS flags), constraints via `pg_get_constraintdef`, user triggers via `pg_get_triggerdef`, functions via `pg_get_functiondef` plus owner/ACL/definer status, `pg_policies`, schema/default/database ACLs, role flags/settings/memberships including grantors, migration rows, six empty-table counts and catalogue values. Sorting avoids dependence on object OIDs/catalog order. Complete query text and outputs are in the preserved helper/snapshots. All records matched; the comparison did not suppress ownership, ACL or policy differences.

Only after that comparison, target-only SQL renamed the restored database to `qr_medication_ci` and created `qr_ci_admin SUPERUSER LOGIN` for the existing test harness. These are test-only modifications, not changes to the source or backup. Fresh independent 32-byte random hex passwords were assigned to target `qr_ci_admin`, `qr_reader`, `qr_setup`, `qr_migrator`; no source passwords/verifiers were reused. Password SQL was piped over stdin after disabling statement/error-parameter diagnostic logging, using the same local setup pattern as the existing tool. Reader/setup/migrator privileges were not expanded.

```powershell
'ALTER DATABASE qr_medication_dev RENAME TO qr_medication_ci; CREATE ROLE qr_ci_admin SUPERUSER LOGIN;' |
    docker exec -i $target psql -X -q -v ON_ERROR_STOP=1 -U qr_dev_admin -d postgres
$env:QR_DATABASE_MODE = 'ci'
$env:QR_DATABASE_PORT = (docker port $target 5432/tcp).Split(':')[-1]
$passwordSql = "SET log_statement='none'; SET log_min_error_statement='panic'; SET log_parameter_max_length_on_error=0;"
foreach ($pair in @(@('ADMIN','qr_ci_admin'),@('QR_READER','qr_reader'),@('QR_SETUP','qr_setup'),@('QR_MIGRATOR','qr_migrator'))) {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $secret = ([BitConverter]::ToString($bytes)).Replace('-','')
    [Environment]::SetEnvironmentVariable("QR_DATABASE_$($pair[0])_PASSWORD", $secret)
    $passwordSql += "ALTER ROLE $($pair[1]) PASSWORD '$secret';"
}
$passwordSql | docker exec -i $target psql -X -q -v ON_ERROR_STOP=1 -U qr_ci_admin -d postgres
dotnet test tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-build --no-restore --verbosity minimal
docker stop $target
```

## Results and recovery prerequisites

- **59 restored-database tests passed, zero failed/skipped.** This includes genuine restricted-reader authentication, no-context/malformed-context denial, tenant/branch isolation, forbidden DML/DDL/role assumption, FORCE RLS, revision triggers/rollback, function privileges and pool/transaction behavior. Privileged fixture writes/teardown occurred only in the disposable restored database. This is real test execution, not merely dump parsing or a build.
- Matched: seven application tables plus EF history, two migration entries, 51 constraints including seven FKs, seven non-internal triggers, six application functions, eight RLS policies, six source roles and one membership grant. The catalog snapshot also includes table indexes, schema/database/default ACLs and role settings. Six protected tables retain ENABLE/FORCE RLS.
- A single-database dump does not supply cluster roles. The separate globals export restores role definitions, attributes, memberships and reader read-only default. This source has no custom tablespaces or parameter ACL rows and only the `plpgsql` extension. Other environments need independently inventoried globals, extension binaries and tablespace paths before recovery.
- Initial globals attempts failed safely on disposable targets: a membership statement with `GRANTED BY qr_dev_admin` could not be replayed when a differently named bootstrap superuser created that role afterward. Initializing the target with the original bootstrap administrator identity and omitting only its duplicate CREATE in a **derived** globals replay succeeded. No membership grant/options or original backup was weakened/rewritten. The subsequent apparent metadata mismatch was ordering only; all catalog lines matched when sorted.
- `--no-role-passwords` intentionally excludes credential recovery. Re-establish login credentials separately through an approved secret-management process. The local socket administration and test-only superuser pattern used here do not establish production secret separation. Server configuration, `pg_hba.conf`, TLS, external secrets, OS/container permissions, WAL/PITR and external dependencies are not recovered by these artifacts.
- This verifies schema/security and the current empty synthetic baseline, plus test-generated data after restore. It does not verify restoration of nonempty historical staff/tenant rows, clinical/patient data, point-in-time recovery, corruption handling, cross-version upgrades, off-host recovery, encryption, scheduled retention or measured RPO/RTO. The new artifacts are local and unencrypted; never use this exercise's storage pattern for sensitive data.
- The previous planning backup directory remains empty/unverified. The planning safety stash remains preserved/readable. This new database backup does not replace or validate those historical planning backups. Production authentication and tenant isolation remain unclaimed; ADR-006 context-forgery and snapshot-revocation limits still apply.

Primary references: [PostgreSQL pg_dumpall](https://www.postgresql.org/docs/18/app-pg-dumpall.html), [PostgreSQL pg_restore](https://www.postgresql.org/docs/18/app-pgrestore.html).
