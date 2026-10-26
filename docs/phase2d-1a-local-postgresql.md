# Phase 2D-1A: local PostgreSQL

Date: 2026-10-25. Approved scope: Product Owner request for development-only PostgreSQL setup and verification, stopping before persistence implementation. Trace: AI-010, HUM-001/HUM-006/HUM-008, REQ-008/REQ-009. This supersedes the earlier Docker availability blocker only; Phase 2D remains incomplete.

## Configuration and evidence

- Docker Engine 29.9.2; Compose v5.6.1; Linux/x86_64 engine.
- Official image `postgres:18.6-bookworm@sha256:bbf83594798e144bff365a9c191c5d779f181fb3135902064ab8ff11918020fc`. Registry manifest verified Linux/amd64 support before pulling; running SQL reports PostgreSQL 18.6 (Debian 18.6-1.pgdg12+2), 64-bit. This proves local container compatibility, not future EF/Npgsql compatibility or an image vulnerability audit.
- Compose project `qr-medication-dev`; service `postgres`; container `qr-medication-dev-postgres-1`.
- Host `127.0.0.1:55432` -> container port 5432. Existing port 5432 listener was preserved. Docker inspection confirms only the IPv4 loopback publication; Windows loopback TCP connection passed.
- Database `qr_medication_dev`; administration user `qr_dev_admin` (image-created superuser). Random 256-bit development password is in ignored `infrastructure/development/.env`, never printed. The safe `.env.example` contains an empty password and startup requires a nonempty value. Existing ignore rules suffice.
- Named volume `qr-medication-dev_postgres_data` -> `/var/lib/postgresql`; SQL confirms PGDATA `/var/lib/postgresql/18/docker`, matching the PostgreSQL 18 image layout.
- Compose `up --wait` succeeded; container health is `healthy`. Health probe uses `pg_isready` every 5 seconds, timeout 5 seconds, 12 retries, startup grace 10 seconds. Readiness alone does not establish authenticated access.
- Separate password-authenticated TCP `psql` connection passed: version, database/user, server address/port and data-directory queries. Non-system application table count is zero. No synthetic records were needed or inserted.

The application is not connected to this database. Administration credentials must never be used by application runtime. No runtime/migration/provisioner role design has been implemented. No seven-table schema, EF package/adapter, migration, RLS, protected API or frontend/DIN change was made. Docker alone does not supply database security. This is local development only, with SCRAM host authentication; no patient data or real pharmacy records. Docker environment inspection and expanded Compose configuration can expose credentials: do not share their output. Local transport is not a production TLS configuration.

## Commands (PowerShell, repository root)

All commands explicitly select the local environment and Compose files. Do not print `.env` or run expanded `docker compose config` when collecting shareable evidence.

Validate without printing resolved secrets:

```powershell
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml config --quiet
```

Start, or start again after shutdown (waits for health):

```powershell
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml up -d --wait --wait-timeout 120
```

Health and readiness:

```powershell
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml ps
docker inspect --format '{{.State.Health.Status}}' qr-medication-dev-postgres-1
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml exec -T postgres pg_isready -h 127.0.0.1 -U qr_dev_admin -d qr_medication_dev
```

Password-authenticated SQL verification without putting a password on the host command line (PowerShell single quotes intentionally preserve container variables):

```powershell
'SELECT version(); SELECT current_database(), current_user;' | docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml exec -T postgres sh -c 'export PGPASSWORD=$POSTGRES_PASSWORD; exec psql -h 127.0.0.1 -U $POSTGRES_USER -d $POSTGRES_DB -v ON_ERROR_STOP=1'
```

Stop without removing the container or volume:

```powershell
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml stop
```

Remove this Compose container/network while retaining the database volume:

```powershell
docker compose --env-file infrastructure/development/.env -f infrastructure/development/compose.yaml down
```

Do not add `--volumes`/`-v`, prune volumes, or delete existing data without separate approval. Shutdown commands are documented, not executed: the verified database remains running. Named-volume mount was verified; a restart/recovery exercise was not performed.

For another developer, copy `.env.example` to `.env` only if `.env` does not exist; set a freshly generated development-only password before startup. `POSTGRES_PORT` can be changed to an unused local port. Initialization variables take effect only for an empty volume: editing the password in `.env` does not rotate an existing database password. Do not delete the volume to resolve a credential mismatch.

## Preserved work and remaining gates

Branch stays `feature/postgresql-foundation`, HEAD `8ea0c662e2de635f6cd6265d8814de90aa7b917f`. Initial six modified tracked planning/tracker files and three untracked documents were preserved; additions to status/tracker/review files are append-only. Existing safety stash `c6cbf3cdeb80ee26866769434d20c06b76a042e1` and all seven manifest-verified backup files remain intact. No staging, commit, push, merge, branch switch or deletion occurred.

HUM-006 decisions remain required before affected persistence: context-setting threat limit/self-bootstrap visibility, cross-module user FK exception and constrained revision maintenance. HUM-001/HUM-008 scope, roles/migration review and later dependency/CI approvals remain bounded by the existing plan. No whole human task is closed. No application bug was established, so BUGS/FIXES were unchanged. Backend/frontend tests were not rerun because no application or dependency files changed; T-015 remains unimplemented/unexecuted. Stop for Product Owner review before further work.

Sources: [official image](https://hub.docker.com/_/postgres), [PostgreSQL version support](https://www.postgresql.org/support/versioning/), [Docker PostgreSQL persistence guidance](https://docs.docker.com/guides/postgresql/immediate-setup-and-data-persistence/). Existing [plan](phase2d-postgresql-tenant-isolation-plan.md) and [safety review](phase2d-1-safety-review.md) retain historical evidence.
