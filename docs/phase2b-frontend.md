# Phase 2B — Exact-DIN medication lookup interface

Status: Implemented locally for Product Owner review; no commit, push or deployment. Phase 2A backend is preserved at `a4d2e29`. This milestone adds only an English product-metadata interface, not a reviewed patient-information catalogue. Trace: F-001/F-003/F-004, REQ-001/REQ-002/REQ-003/REQ-004/REQ-010, T-012.

## Architecture and dependencies

`frontend/` contains a small Vite/React/TypeScript application. Runtime dependencies are React and React DOM only; there is no router, UI framework, state library, external font service, analytics, local storage or patient-data collection. Plain CSS provides the visual system; small inline SVGs provide decorative icons. The existing `.gitkeep` remains intact.

- `src/App.tsx`: form, request state, cancellation, stale-response protection and page composition.
- `src/api/medications.ts`: same-origin fetch with an AbortSignal, 20-second client timeout, typed runtime checks and safe error categories. Only `GET /api/medications/{DIN}?language=en` is used. Unknown response fields and upstream error bodies are not displayed. Mismatched DINs, unsafe source links or approval/publication flags inconsistent with metadata-only output are rejected.
- `src/components/MedicationCard.tsx`: factual metadata, source page link, source dates and a native disclosure for provenance. `Icon.tsx` contains decorative SVGs.
- `src/messages/en.ts`: interface copy separated for future localization. No translations are generated or claimed. `index.html` declares English/LTR; logical CSS properties and bidi isolation prepare for future RTL validation.
- Vite 8.3.4/plugin-react 6.1.2 build and serve the app; TypeScript 5.9.3 checks types. ESLint 10.13.0 with typescript-eslint and React Hooks rules provides linting. Vitest 5.0.5, Testing Library and happy-dom run deterministic DOM/HTTP-mocked tests. `package-lock.json` records exact resolved dependencies from the official npm registry.

Current environment (2026-10-22): Node.js 24.22.0 is active. Historical verification on 2026-10-17 encountered system Node 22.11.0, which could not support all maintained lint/build tools, and used portable Node 24.22.0 LTS downloaded from nodejs.org and checked against its official SHA-256; that session did not replace the system runtime. The initial compatibility approach was superseded before completion: no deprecated ESLint 9 or Vite 6 direct dependency remains. Use Node 24 LTS for the commands below; the declared minimum is Node 22.13 or newer supported even/LTS releases. Initial sandbox process-launch failures were resolved by authorized execution; no security controls or certificate checks were disabled.

## Start both applications on Windows in VS Code

Open the repository folder in VS Code. Use two integrated PowerShell terminals. First verify `node --version` is Node 24 LTS (or a supported version satisfying package.json) and the .NET SDK 10.0.402 pinned by root `global.json` is available (see the [runtime upgrade](phase2d-0-dotnet10-upgrade.md)). If PowerShell blocks `npm.ps1`, use `npm.cmd` as below; no execution-policy change is needed. HUM-013 remains open pending explicit human confirmation, despite Node.js 24.22.0 being active in the current environment. The historical portable verification runtime was not a tracked dependency or a permanent system installation.

Terminal 1, repository root:

```powershell
dotnet restore MedicationInformationPlatform.sln --source https://api.nuget.org/v3/index.json
dotnet run --project src/Api/MedicationInformationPlatform.Api.csproj --no-launch-profile -- --urls http://127.0.0.1:5080
```

Terminal 2, repository root:

```powershell
cd frontend
npm.cmd ci --registry=https://registry.npmjs.org --offline=false
npm.cmd run dev
```

Open **http://127.0.0.1:5173/**. The backend listens on **http://127.0.0.1:5080/**; an exact backend request is **http://127.0.0.1:5080/api/medications/00559407?language=en**. The same request through the frontend proxy is **http://127.0.0.1:5173/api/medications/00559407?language=en**. Both processes stay on loopback; stop each with Ctrl+C. HTTP is local-development-only; production requires a separate HTTPS hosting/security design.

Optional configuration: copy `frontend/.env.example` to ignored `frontend/.env.local` and restart Vite after changes. `VITE_API_BASE_URL=/api` selects the same-origin route prefix. `API_PROXY_TARGET=http://127.0.0.1:5080` selects the local backend origin and is not exposed to browser code. The proxy allows HTTP(S) loopback origins only and uses `changeOrigin` to match backend host filtering. Browser code rejects external/protocol-relative base URLs. No backend CORS relaxation was needed. Client-side environment values are public and must never contain secrets.

The proxy is a development configuration; `npm.cmd run preview` serves the production bundle at http://127.0.0.1:4173/ but does not provide the development API proxy. A production gateway must route the same-origin `/api` path to the backend. Do not deploy this milestone as-is or configure a remote patient-data service.

## Screens and behavior

One responsive lookup page presents a DIN form beside the results area on wide screens and stacks them on narrow screens. It offers an initial empty state, loading, inline invalid-input feedback, no-match, source/network/timeout unavailable state and retry. There is no name search or catalogue listing. The DIN remains text, with numeric keyboard hints and leading zeros preserved; only eight ASCII digits are accepted after trimming surrounding whitespace. No request is made on page load.

Success displays brand, DIN, ingredients with strength and any denominator, dosage forms, routes, DPD marketing status, company and official attribution. Source update/retrieval details are progressively disclosed. The permanent visible notice says that this is not prescribing instructions or approved patient guidance, and that ingredient strength is not a dose. Product source approval is never inferred from Marketed status.

Editing the input cancels any pending request and immediately clears previous results. Sequence checks prevent late responses from replacing newer input. Retry uses the failed exact DIN; changing the input clears that retry state. Explicit labels, linked hints/errors, a skip link, visible focus indicators, Enter submission, live status/alert regions and result-heading focus support keyboard and assistive technology use. Reduced-motion preferences disable loading animation. Source links open a new tab with noopener/noreferrer; backend payloads are rendered as text, never HTML.

The shell is English-only. CSS uses logical spacing/alignment and DIN/strength values are bidi-isolated; this is preparation, not proof of full RTL or medical translation support. Browser/participant validation remains necessary (HUM-002/HUM-009, AI-008).

## Manual browser QA checklist (AI-008 / T-012)

Status: **Not performed in this finalization pass.** Record tester, date, browser/version, viewport, zoom, each result and any defect evidence. Use only public DINs or synthetic fixtures; never enter patient data. Start both applications using the instructions above with existing dependencies. Do not run installation commands merely to perform QA.

- [ ] Desktop: check initial empty state, persistent metadata-only notice, a successful exact-DIN lookup, leading zeros, source disclosure/link and clearing results when the DIN changes. Verify no lookup occurs on initial load.
- [ ] Mobile: inspect 320px and 375px widths and portrait/landscape; confirm stacked layout, readable notice/table, usable controls and no clipped content or horizontal page scrolling. Record whether tested on a physical device or browser emulation.
- [ ] 200% browser zoom: repeat form, success and error views at a recorded desktop viewport; check reflow, readable text, visible focus and access to every control without overlap or truncation.
- [ ] Keyboard only: Tab to the visible skip link, activate it, reach the DIN field, submit with Enter, inspect invalid-input focus, successful result-heading focus, source disclosure and link, retry and reverse navigation. Confirm logical order and no keyboard trap.
- [ ] Error/loading states: try `1234` and letters (inline error, no API call), `00000000` (no-match), and DevTools offline/request blocking (safe unavailable message and retry after restoring connectivity). Throttle a request to inspect loading/disabled submit; edit the DIN before it completes and confirm stale results never appear. For a request held pending beyond 20 seconds, check timeout/retry; mark not tested if the browser setup cannot hold the request pending.
- [ ] RTL shell readiness: temporarily set the document root `dir` to `rtl` in DevTools without editing files; inspect alignment, stacking, controls, arrow direction and source disclosure. Confirm DIN and strengths remain LTR and isolated. Restore `ltr` or reload. English copy remains English; this does not validate translations or complete RTL support.
- [ ] Assistive technology (separate evidence): if available, record screen reader/version and inspect labels, errors, loading announcements and result focus. Otherwise mark **not tested**. DOM tests and keyboard checks do not establish screen-reader success or accessibility conformance.

AI-008 remains pending until actual results and unresolved defects are recorded. Participant recruitment/data collection still requires HUM-009 authorization.

## Verification

From `frontend/`, using the supported Node runtime:

```powershell
npm.cmd run build
npm.cmd run lint
npm.cmd test
```

The first implementation run passed typecheck/production build, zero-warning lint, and 27 mocked tests in two files. Tests cover no automatic fetch, invalid/leading-zero DINs, Enter submission and result focus, displayed metadata/source links, loading state, HTTP errors, retry, cancellation/stale responses, malformed or unsafe source payloads, fixed nonpublication boundaries, timeout, configurable safe API prefix and text-only rendering. BUG-002/FIX-002 records the corrected busy/live-region relationship and its regression assertion. The final results below include the additional contrast regression check.

Historical checks on 2026-10-17 after the accessibility corrections passed: TypeScript/production build, lint with zero warnings, **28 tests across three files**, and npm audit with **zero reported vulnerabilities**. No backend code changed, so the previously verified 53 backend tests were not rerun. BUG-003/FIX-003 records the placeholder contrast correction; the added test derives its actual CSS colors and checks at least 4.5:1 contrast. The corrected value is approximately 5.58:1. Body, muted text, button and notice palette checks were approximately 12.14:1, 5.58:1, 7.13:1 and 5.75:1 for the checked foreground/background pairs. These targeted checks use the [W3C contrast design criterion](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html); they do not establish full accessibility conformance.

Read-only HTTP checks through the running Vite server on 2026-10-17 returned:

| Frontend proxy request DIN | HTTP | Brand / ingredient strength | Form / route / status |
|---|---|---|---|
| 00559407 | 200 | TYLENOL EXTRA STRENGTH / ACETAMINOPHEN 500 MG | Tablet / Oral / Marketed |
| 01933558 | 200 | ADVIL TABLETS / IBUPROFEN 200 MG | Tablet / Oral / Marketed |
| 02099233 | 200 | GLUCOPHAGE / METFORMIN HYDROCHLORIDE 500 MG | Tablet / Oral / Marketed |
| 00000000 | 404 | Product not found | Safe Problem Details |

The HTML page returned HTTP 200; all successful proxy results retained Health Canada provenance and false patient-guidance/publication flags. These checks exercised Vite → existing ASP.NET API → live DPD; they were not browser-rendered interaction tests. No real patient data or monograph text was requested.

During the 2026-10-17 verification session, no enabled browser surface was available: both in-app and Chrome preview entry points reported unavailable. DOM interaction tests and live proxy HTTP checks ran; screenshots, real-browser mobile reflow/zoom and screen-reader operation could not be verified. AI-008 tracks that concrete remaining QA. No accessibility conformance, clinical accuracy, translation approval or production readiness is claimed.

## Scope and review status

Finalization verification on 2026-10-22 used active Node.js 24.22.0 and .NET SDK 8.0.100. Production build, standalone TypeScript checking, zero-warning lint, all 28 frontend tests and all 53 backend tests (45 unit, eight integration; none skipped) passed using existing dependencies. Authorized process permissions were used for the build/test tools. The historical npm audit and live DPD checks above were not repeated. Manual browser and assistive-technology QA remains unperformed in this pass; use the checklist above. BUG-004/FIX-004 records the documentation corrections.

No backend source code, accepted ADR, schema, authentication, QR generation, PMS/FHIR integration or patient-specific capability changed. Selective staging of Phase 2B is authorized for final review; commit and push remain unauthorized. Dependencies/build caches, dist output, temporary Node files and actual local environment files are excluded from Git; only `.env.example` is shareable configuration. Review the frontend source, tests, lockfile and associated documentation before approving any commit or next milestone.

Both historical verification servers were stopped at teardown. Historical final local checks found no broken Markdown file links/anchors, undefined stable identifiers, sensitive-value pattern matches or whitespace issues (including new files). Every resolved package URL in the lockfile uses the official npm registry.

## Files for review

Created frontend files (20):

```text
frontend/.env.example
frontend/eslint.config.js
frontend/index.html
frontend/package.json
frontend/package-lock.json
frontend/tsconfig.json
frontend/vite.config.ts
frontend/vitest.config.ts
frontend/src/main.tsx
frontend/src/App.tsx
frontend/src/App.test.tsx
frontend/src/api/medications.ts
frontend/src/api/medications.test.ts
frontend/src/components/Icon.tsx
frontend/src/components/MedicationCard.tsx
frontend/src/messages/en.ts
frontend/src/styles.css
frontend/src/styles.test.ts
frontend/src/test/fixture.ts
frontend/src/test/setup.ts
```

Created documentation: this file. Updated existing files (16): `AGENTS.md`, `README.md`, `docs/accessibility-and-localization.md`, `docs/architecture/system-overview.md`, `docs/design-system.md`, `docs/feature-catalogue.md`, `docs/phase2a-backend.md` (committed-state clarification only), `docs/project-roadmap.md`, `docs/project-status.md`, `docs/technical-architecture.md`, `docs/testing-strategy.md`, `docs/ui-ux-blueprint.md`, `tasks/AI-TODO.md`, `tasks/BUGS.md`, `tasks/FIXES.md`, `tasks/HUMAN-TODO.md`. This inventory describes the Phase 2B change set only. The combined working tree also contains separate, unapproved design documentation and additions to shared documents; those additions are excluded from Phase 2B staging.
