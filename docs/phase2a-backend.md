# Phase 2A — Exact-DIN source metadata backend

Status: Phase 2A was committed and pushed as `a4d2e29` after explicit Product Owner approval. The historical .NET 8 build succeeded with zero warnings/errors; all 53 mocked automated tests passed (45 unit, 8 API integration). This is a development source-lookup capability, not a reviewed public medication catalogue or patient guidance service. Phase 1C documentation was committed separately as `c31ac78`; subsequent uncommitted frontend work is documented in [Phase 2B](phase2b-frontend.md).

## Scope and architecture

One read-only endpoint retrieves product, active ingredients, dosage forms, routes and status from the official Health Canada DPD API. No patient information, instructions, monograph text, summaries, translations, persistence, frontend, PMS or FHIR integration is implemented. Source status (including Marketed) does not confer local clinical approval or publication.

- MedicationCatalogue Application defines `IMedicationProductLookup`, input validation, source metadata records and explicit outcomes.
- MedicationCatalogue Infrastructure implements outbound HTTP using `IHttpClientFactory`. API handlers reference only Application contracts; `Program.cs` is the composition root and calls the Infrastructure registration extension. No business rules or persistence implementations are in API handlers.
- No Domain project was invented for this read-only transport/data-normalization slice. Future business rules belong in Domain; PostgreSQL/EF Core remain the accepted persistence direction but are not needed or configured here.
- Current builds use .NET 10 with the SDK pinned by root `global.json`; see the [runtime upgrade](phase2d-0-dotnet10-upgrade.md). Historical Phase 2A verification used .NET 8. Runtime code has no new NuGet package references; test-only references are xUnit, its test runner, Microsoft.NET.Test.Sdk and Microsoft.AspNetCore.Mvc.Testing.

Trace: F-001/F-004, REQ-001/REQ-004/REQ-010 and T-011. Only source metadata lookup is authorized by the Phase 2A Product Owner instruction; public catalogue publication, QR workflows and private capability remain separate scope.

## Local startup

Prerequisite: .NET SDK 10.0.402 as pinned by root `global.json`. From the repository root, with the approved test-only package restore:

```powershell
dotnet restore MedicationInformationPlatform.sln
dotnet build MedicationInformationPlatform.sln --no-restore
dotnet test MedicationInformationPlatform.sln --no-build --no-restore
dotnet run --project src/Api/MedicationInformationPlatform.Api.csproj --no-launch-profile -- --urls http://localhost:5080
```

HTTP here is for local loopback development only; deployment requires HTTPS. The application has no deployment configuration and is not approved for public hosting. No database or identity provider is needed for this public-product-only read endpoint. Protected staff/tenant operations do not exist.

Example request (public medication DIN, not patient data):

```http
GET http://localhost:5080/api/medications/00559407?language=en
```

The optional `language` defaults to `en`; only `en` and `fr` are accepted. DIN must be exactly eight ASCII digits; leading zeros are preserved. No search-by-brand, caller-supplied upstream URL or patient identifier is accepted. French source metadata is not a generated translation or a guarantee that every field is French.

Configuration in `src/Api/appsettings.json`:

- `HealthCanadaDpd:BaseUrl`: defaults to `https://health-products.canada.ca/api/drug/`. Startup validation permits only this official HTTPS host/path, default port and no credentials/query/fragment. Configuration remains server-owned, not an API parameter. `HealthCanadaDpd__BaseUrl` is the corresponding environment override, subject to the same validation.
- `HealthCanadaDpd:TimeoutSeconds`: total lookup deadline, default 15 seconds, permitted range 1–30; includes all five sequential requests and body reads. Environment override: `HealthCanadaDpd__TimeoutSeconds`.
- Response bodies are limited to 1 MiB per upstream resource; redirects and cookies are disabled. There are no automatic retries, application caching, retained snapshots or database writes. Responses to callers use `Cache-Control: no-store`.

## Response and error contract

Successful metadata includes DIN, DPD drug_code, brand/company/class, optional descriptor/update date, all ingredients and their strength units/denominators, form and route lists, status metadata, and source provider/API URL/product-page URL/language/retrieval time/licence attribution. It always includes `contentClassification: RegulatoryProductMetadata`, `isApprovedPatientGuidance: false`, `isPublished: false` and an explicit metadata-only notice. Ingredient strengths are not doses.

The official product-page reference is constructed from the verified internal drug_code. No monograph URL is guessed and no HTML/PDF scraping is performed. An upstream response can never set local approval/publication flags or inject unrecognized properties into the response.

| HTTP result | Meaning |
|---|---|
| 200 | Complete, associated source metadata, including non-marketed statuses if returned; not clinical approval |
| 400 | Invalid DIN or language |
| 404 | DPD returned an empty product collection for the valid exact DIN |
| 502 | HTTP/network failure, malformed/incomplete JSON or wrong DIN/drug_code association; no partial product is returned |
| 504 | Upstream operation exceeded the lookup deadline or reported cancellation without caller cancellation |

Caller cancellation propagates through all outbound requests and stream reads. It is not relabelled as a product-not-found result. Safe Problem Details messages contain no upstream bodies, exception details or product payload on failure. HTTP client/hosting request diagnostics are suppressed below Warning to avoid routine request-URL logging; no body logging is enabled.

## Verification and limitations

Automated fixtures use a fictional product and mocked HTTP; tests cover exact matching, association, multiple ingredients, denominator preservation, object/array handling, missing/null/malformed fields, duplicate product matches, invalid configuration, unknown products, HTTP/network errors, timeouts/cancellation, safe API errors and fixed publication flags. All 45 unit and 8 API integration tests passed using `dotnet test MedicationInformationPlatform.sln --no-build --no-restore --nologo -m:1`; none were skipped. Build completed with zero warnings/errors. See the test projects under `tests/Unit` and `tests/Integration`. No live smoke test was performed in this restoration/testing step.

Limitations: no production availability/rate-limit policy or SLA is established; there is no cache, retry/backoff, publisher, monograph ingestion or clinical review. Status eligibility is a future catalogue-publication decision, not a filter on raw source metadata. Source reuse/monograph rights, accessibility and reviewed patient-language content remain HUM-003/HUM-007 decisions. AI-007 retains source coverage/operational follow-up. HUM-008 includes choosing a current patched runtime before deployment; historical verification used SDK 8.0.100/runtime 8.0.0. Current verification is recorded in the [runtime upgrade](phase2d-0-dotnet10-upgrade.md).

Phase 2A implementation was committed/pushed after Product Owner approval. Accepted ADRs are unchanged; Phase 2B remains uncommitted pending its separate review.

### Package restoration evidence

Historical Phase 2A evidence (preserved; current test-host version is in the runtime upgrade): the approved restore explicitly used `https://api.nuget.org/v3/index.json`. Direct test dependencies are xUnit 2.8.0, xunit.runner.visualstudio 2.8.0 (test discovery/execution), Microsoft.NET.Test.Sdk 17.6.0 and Microsoft.AspNetCore.Mvc.Testing 8.0.0. They built and ran successfully against net8.0. No production project has a NuGet PackageReference or resolved NuGet package dependency; runtime code uses the .NET/ASP.NET shared framework only.

Package-cache provenance was checked: 49 unique resolved test packages reported nuget.org; the remaining transitive System.Reflection.Metadata 1.6.0 came from Microsoft's Visual Studio offline cache. Its cached archive SHA-512 was verified against the official nuget.org package download and matched. The initial no-restore build failed with NETSDK1004 because restore assets did not exist; after the explicit restoration approval there were no restore, compilation or test failures.

## Live verification through the local API

Performed 2026-10-17 at approximately 20:25 America/Winnipeg (source retrieval timestamps were 2026-10-18T01:25:02Z). Started the existing built application with:

```powershell
dotnet run --project src/Api/MedicationInformationPlatform.Api.csproj --no-build --no-restore --no-launch-profile -- --urls http://127.0.0.1:5080
```

All requests went through `http://127.0.0.1:5080/api/medications/{DIN}?language=en`, exercising the real Application/Infrastructure/HTTP path. No external requests were substituted for local API verification.

| DIN | HTTP | Product returned | DPD code | Ingredient / strength | Form / route | Status |
|---|---|---|---|---|---|---|
| 00559407 | 200 | TYLENOL EXTRA STRENGTH | 5255 | ACETAMINOPHEN / 500 MG | Tablet / Oral | Marketed, code 2 |
| 01933558 | 200 | ADVIL TABLETS | 13625 | IBUPROFEN / 200 MG | Tablet / Oral | Marketed, code 2 |
| 02099233 | 200 | GLUCOPHAGE | 17104 | METFORMIN HYDROCHLORIDE / 500 MG | Tablet / Oral | Marketed, code 2 |
| 00000000 | 404 | No product payload | — | — | — | Safe Problem Details: Product not found |

All successful results preserved leading-zero DINs and returned Human class, company, source update date, status dates and the expected related metadata. Optional descriptor/dosage denominator fields were null rather than invented. Provenance identified Health Canada Drug Product Database, `https://health-products.canada.ca/api/drug/`, language `en`, the corresponding official page with `code=5255`, `13625` or `17104`, retrieval UTC time and licence attribution. All responses carried `Cache-Control: no-store`; success JSON included fixed metadata-only classification, `isApprovedPatientGuidance: false`, `isPublished: false` and the explicit notice. Ingredient strengths are not dosing recommendations.

Unknown-DIN response body:

```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Product not found","status":404}
```

It used `application/problem+json` without exception details, upstream bodies or patient data. No discrepancies were observed against the Phase 1C identities and metadata. The actual HTTP responses were inspected, not inferred from the mocked tests. This narrow smoke test does not establish clinical correctness, availability guarantees, broader DPD coverage or publication readiness.

No code changes were needed, so the existing zero-warning build and 53 passing mocked tests were not repeated. A pre/post file-hash comparison excluding Git/build artifacts confirmed the live run changed or deleted no source/document files. The server was stopped after verification. Only this document, project status and the testing strategy were subsequently updated to record results; no implementation files were staged, committed or pushed.

## Implementation file inventory for review

- Build: `Directory.Build.props`; `MedicationInformationPlatform.sln` now includes the five projects.
- API: `src/Api/MedicationInformationPlatform.Api.csproj`, `Program.cs`, `MedicationEndpoint.cs`, `appsettings.json`.
- Application: `src/Modules/MedicationCatalogue/Application/MedicationCatalogue.Application.csproj`, `MedicationLookup.cs`.
- Infrastructure: `src/Modules/MedicationCatalogue/Infrastructure/MedicationCatalogue.Infrastructure.csproj`, `DependencyInjection.cs`, `HealthCanadaDpdOptions.cs`, `HealthCanadaDpdClient.cs`.
- Tests: `tests/Fixtures/DpdStubHandler.cs`; `tests/Unit/MedicationCatalogue.UnitTests.csproj`, `MedicationLookupTests.cs`; `tests/Integration/MedicationApi.IntegrationTests.csproj`, `MedicationEndpointTests.cs`.
- Documentation aligned with actual scope/status: `AGENTS.md`, `README.md`, `docs/architecture/system-overview.md`, `docs/feature-catalogue.md`, `docs/integration-strategy.md`, `docs/project-status.md`, `docs/technical-architecture.md`, `docs/testing-strategy.md`, this document, `tasks/AI-TODO.md`, `tasks/BUGS.md`, `tasks/HUMAN-TODO.md`.

Accepted ADRs, existing placeholder files, frontend, other modules, infrastructure environment directories, FIXES history and the committed Phase 1C research report were preserved. No unrelated source, database schema or patient-data files were introduced. No new bug/fix record was necessary for the successful live verification.
