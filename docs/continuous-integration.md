# Continuous integration

The [CI workflow](../.github/workflows/ci.yml) is implemented locally for review. It runs on pull requests targeting `main` (including draft PR updates) and pushes to `main`. It does not run on standalone feature-branch pushes without an applicable pull request. No workflow has been committed or pushed as part of this CI task, and a hosted pass is not yet claimed. Trace: T-011/T-012/T-013/T-014.

## Jobs and tools

Two independent jobs run on GitHub-hosted `ubuntu-24.04`, each with a 15-minute timeout:

- **Backend:** install .NET SDK `8.0.x`, select the action's exact installed SDK using a runner-only `global.json`, restore the solution from public NuGet, build Release without another restore, then run every solution test project without rebuilding/restoring. This includes the catalogue unit tests, synthetic authorization tests and in-process API integration tests. The temporary SDK selection prevents newer SDKs preinstalled on the hosted runner from taking precedence; it is not a committed repository file.
- **Frontend:** install Node.js `24`, run `npm ci` against the committed lockfile, then run standalone TypeScript checks (`npm exec --no -- tsc --noEmit`), zero-warning ESLint, production build and Vitest. The build also runs TypeScript as defined by the existing package script. All frontend commands run from `frontend/`.

Official actions are pinned by compatible major: [checkout v7](https://github.com/actions/checkout/tree/v7), [setup-dotnet v6](https://github.com/actions/setup-dotnet/tree/v6), and [setup-node v7](https://github.com/actions/setup-node/tree/v7). Their official action manifests were checked when preparing this workflow. These major references receive upstream fixes; they do not provide immutable SHA pinning. SDK/runtime patch releases may also advance within the selected version ranges. No application dependencies or package scripts are changed.

## Security and service boundaries

Workflow permissions are limited to `contents: read`; unspecified token permissions are disabled. Checkout uses `persist-credentials: false`. There are no custom repository secrets, privileged PR triggers, deployment steps, service containers, package publishing, cache writes or artifact uploads. Automatic npm caching is disabled, and no NuGet cache is configured.

Backend integration tests host ASP.NET in-process and replace the DPD HTTP handler with synthetic fixtures. Authorization readers are test-only in-memory fixtures. Frontend tests mock fetch in happy-dom; build does not start the Vite development proxy. The tests need no locally running API, real Health Canada service, identity provider or production database. The jobs still need network access to GitHub, official tool downloads and public package registries during setup/restore/install. Package installation can execute dependency lifecycle scripts; jobs receive no application secrets or write-scoped token.

Passing CI verifies only the existing synthetic/mocked application boundary. It does not prove production tenant isolation, recipient eligibility, grant execution, OIDC/BFF, PostgreSQL RLS, medical correctness, browser accessibility or deployment readiness.

## Local reproduction and verification

From the repository root:

```powershell
dotnet restore MedicationInformationPlatform.sln --source https://api.nuget.org/v3/index.json
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore
dotnet test MedicationInformationPlatform.sln --configuration Release --no-build --no-restore --verbosity normal
```

From `frontend/` (use `npm.cmd` on Windows if PowerShell blocks `npm.ps1`):

```powershell
npm.cmd ci
npm.cmd exec --no -- tsc --noEmit
npm.cmd run lint
npm.cmd run build
npm.cmd test
```

Local validation on 2026-10-22 passed: checksum-verified standalone actionlint 1.7.12 (workflow syntax/semantics; unavailable ShellCheck/Pyflakes disabled), backend restore and Release build with zero warnings/errors, and all 114 backend tests (106 unit, eight API integration; none skipped). Under Node 24.22.0, a clean `npm ci` in an isolated temporary copy of tracked frontend files passed, followed by standalone TypeScript, zero-warning lint, production build and all 28 tests. This lockfile-based isolated run is the frontend CI reproduction.

The repository-local `npm ci` initially failed with EPERM on a locked Rolldown native binding held by existing tooling. That process was left running. Existing local dependencies were restored without changing package manifests/lockfile, and local typecheck/lint/build/all 28 tests also passed; npm reported a cleanup warning for an old locked binding under ignored `node_modules`. No recursive cleanup or process termination was performed. Local Windows verification does not execute GitHub's setup actions or establish a hosted Ubuntu pass. A first hosted run must be reviewed after separately approved commit/push; repository Actions policy or fork approvals may require owner action. Branch protection and required-check policy are not configured by this change.
