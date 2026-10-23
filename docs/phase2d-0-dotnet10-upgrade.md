# Phase 2D-0 - .NET 10 runtime upgrade

Status: bounded prerequisite approved by the Product Owner; implemented locally and verified on 2026-10-22, awaiting review. Nothing staged, committed or pushed. Branch: `feature/postgresql-tenant-isolation`. Trace: T-011/T-012/T-013/T-014/T-016, AI-009/AI-010, HUM-001/HUM-008/HUM-014. Accepted ADR-001 through ADR-004 are unchanged.

## Exact versions and compatibility

| Component | Upgrade / retained version | Evidence |
|---|---|---|
| SDK | 10.0.402, pinned by root global.json; rollForward=disable, allowPrerelease=false | Official release metadata and local dotnet --info/--version |
| .NET / ASP.NET Core runtime | 10.0.13 / 10.0.13 | Included shared frameworks; local test adapter reports .NET 10.0.13 |
| Target framework | net10.0 in Directory.Build.props, inherited by all eight projects | Release build of every solution project |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.0 -> 10.0.13 | Matches the ASP.NET Core major; eight in-process API tests pass |
| Microsoft.NET.Test.Sdk | 17.6.0 retained | Both test projects discover and execute successfully |
| xunit / xunit.runner.visualstudio | 2.8.0 / 2.8.0 retained | 114 passing cases; existing VSTest model preserved |
| Node.js | 24.22.0 active locally; CI remains major 24 | Frontend checks pass; manifests/lockfile unchanged |

[Microsoft release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json) identifies SDK 10.0.402 and runtime 10.0.13 as the current .NET 10 LTS release, dated 2026-10-13, with support ending 2028-11-14. Test-host package metadata: [NuGet registration](https://api.nuget.org/v3/registration5-semver1/microsoft.aspnetcore.mvc.testing/10.0.13.json). Compatibility is established for this solution's tests, not every future dependency or deployment.

The only direct NuGet version change is the ASP.NET integration-test host. Its required transitive dependencies resolve on net10.0. Production projects still have no external NuGet PackageReference; no EF Core/Npgsql/tool package was introduced. No source code, test assertions, project references or solution membership changed. Default language/analyzer behavior follows the new SDK; the Release build reports zero warnings/errors with warnings treated as errors.

The official win-x64 portable SDK archive was downloaded outside the repository and its SHA-512 matched release metadata:

```text
ae4fd528cb7e9f6af6aaf7de36715a69f761f183744f1bb3025d19f5a92447090ec561410e46266c1d10a63781ed6f3de771760bc76c876d87daabc367b898cd
```

The system SDK installation remains 8.0.100/runtime 8.0.0. Verification used the portable SDK with process-local DOTNET_ROOT, DOTNET_ROOT_X64 and PATH. Configure normal development terminals/IDE for SDK 10.0.402; the exact pin deliberately fails rather than falling back to .NET 8. No machine-wide PATH change was made. SDK first-use setup reported creating an HTTPS development certificate; no trust command was run.

## Changes and scope separation

Runtime configuration: `Directory.Build.props`, new `global.json`, `tests/Integration/MedicationApi.IntegrationTests.csproj`, and `.github/workflows/ci.yml`. CI setup-dotnet v6 reads the root global.json instead of selecting 8.0.x and generating a runner-only file. Checkout v7, setup-node v7, triggers, permissions and frontend steps are unchanged. See [SDK selection](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) and [setup-dotnet global-json-file support](https://github.com/actions/setup-dotnet).

Runtime documentation: this record, README, CI documentation, backend/frontend startup documentation and all four task trackers. Separate Phase 2D-0 sections are appended to project status, roadmap, testing strategy, AI-TODO and HUMAN-TODO, preserving their pre-existing planning content.

The pre-existing seven-file PostgreSQL planning work is retained. `docs/phase2d-postgresql-tenant-isolation-plan.md` is byte-for-byte unchanged, SHA-256 `b425b9a2edeaa57de64118a7abb128dcf4f003ad4546a38b867623aac93bf7bd`. Its runtime-choice discussion and net8/net10 candidate table are preserved historical proposals: the later approval resolves only the .NET 10 prerequisite. They do not authorize installing the proposed persistence packages. The existing planning addition in `docs/security/authorization.md` is untouched. The five other mixed records retain their original planning additions before the separately labelled upgrade sections. A later commit review must select runtime hunks separately; nothing is staged now.

No PostgreSQL, EF Core, migrations, RLS, authentication, protected endpoint or patient functionality was implemented. Public DIN source metadata, synthetic authorization contracts and actor-only delegation semantics remain unchanged. Real recipient eligibility, grant execution, authenticated identity, production tenant isolation and deployment readiness remain deferred.

## Local verification

With SDK 10.0.402 selected, from the repository root:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
dotnet restore MedicationInformationPlatform.sln --source https://api.nuget.org/v3/index.json
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore
dotnet test MedicationInformationPlatform.sln --configuration Release --no-build --no-restore --verbosity normal
dotnet list MedicationInformationPlatform.sln package --vulnerable --include-transitive --no-restore --source https://api.nuget.org/v3/index.json
```

From frontend/ with existing dependencies:

```powershell
npm.cmd exec --no -- tsc --noEmit
npm.cmd run lint
npm.cmd run build
npm.cmd test
```

Results: restore passed; Release build passed with zero warnings/errors; 106 unit cases (61 authorization and 45 catalogue) plus eight API integration cases passed, none skipped. Standalone TypeScript, zero-warning ESLint, production build and all 28 frontend tests passed under Node 24.22.0. NuGet reported no known vulnerable packages across all eight projects, including transitive packages. No new dependencies were needed beyond the compatible integration-test hosting version.

Initial CLI invocations produced build/advisory output but delayed returning; process-local opt-out of optional telemetry/workload notifications allowed restore/build/tests/advisory commands to complete successfully. This is an environment observation, not a demonstrated application defect or a reason to disable package auditing. No broad warning suppression was introduced.

Workflow syntax/semantics passed standalone actionlint 1.7.12 with unavailable ShellCheck/Pyflakes integrations disabled. Markdown structure/local file-reference checks and git diff --check passed. Review found no secrets, generated tracked files, application changes or unexpected dependency additions. Build outputs remain ignored. Advisory checks are point-in-time evidence, not a guarantee of vulnerability absence; frontend dependencies were unchanged and no fresh npm advisory audit is claimed.

Hosted Ubuntu execution for this .NET 10 update is pending approved commit/push. No live Health Canada dependency, production database, browser/assistive-technology QA, production deployment or performance test was used. Existing synthetic tests provide regression evidence only. HUM-013 remains open. Maintain SDK/runtime servicing deliberately; the exact SDK pin must be reviewed for future patches. All persistence decisions in the preserved proposal remain separately gated.
