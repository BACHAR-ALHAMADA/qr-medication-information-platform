# Phase 3A: local public QR demonstration

Implemented locally on 2026-11-11; subsequent Product Owner instructions authorize final review and conditional selective commit/push/Draft PR delivery. Trace: F-001/F-002/F-003/F-004, REQ-001/REQ-002/REQ-003/REQ-007/REQ-008/REQ-009, T-018. Product Owner explicitly approved fictional, nonclinical English/Arabic/French fixtures and a local public QR journey. This supersedes the roadmap's approval-pending wording only for this bounded demonstration.

## Implemented journey and boundaries

Open `/demo`, select Aster or Pebble (both imaginary demonstration products), generate an SVG QR, then scan or open its URL. `/demo/{reference}` resolves the selected product through the API and offers EN/AR/FR switching. Arabic sets document and application `lang=ar`, `dir=rtl`; French/English reset LTR. Labels, disclaimers, errors and public content are translated. The mobile layout uses native selects, visible focus, a skip link, structured headings, a sticky disclaimer and a text-link alternative to the QR. QR generation moves focus to its result heading. Nothing requires login or personal data.

The existing DIN screen remains `/` and `GET /api/medications/{din}` is unchanged. Fictional demo data is a separate MedicationCatalogue Application component. It never calls DPD, Identity, Organizations, private services or PostgreSQL. No real DIN, dose, contraindication, interaction, warning or treatment fact is invented. The content describes the demonstration itself. Every view, including loading and errors, retains an unmistakable nonclinical disclaimer, repeated in the footer. The real DIN screen retains its own original metadata-only warning and behavior.

New read-only API contracts:

| Route | Result |
|---|---|
| `GET /api/demo/products?language=en` | Two active fictional products; `en`, `fr`, `ar` supported |
| `GET /api/demo/references/{reference}?language=ar` | Exact public fixture or generic 404 |

Response fields are only `reference`, `name`, `language`, `description`, `disclaimer`, `contentStatus`. Status is always `fictional-unreviewed`. Invalid languages return 400; invalid, unknown, withdrawn and expired references return indistinguishable 404 problem responses without echoing input or fixture details. Write methods are unavailable. Existing no-store headers apply. The frontend validates response shape/reference/language/status, renders text without HTML injection, bounds fetch to 10 seconds, aborts obsolete requests and offers safe retry. An aborted old request cannot replace a newer language response.

Reference fixtures (opaque public labels, not bearer authentication):

| Fixture | Reference |
|---|---|
| Aster | `8a60f37d45bb4218a702df2483e19b50` |
| Pebble | `c42f7190586e4a8d9b31e20765af040c` |
| Withdrawn, unavailable | `de4029a3db7446a1bbfa8e50e9a0ec30` |
| Expired, unavailable | `1246de59ad3c4031b1024b3fc8e7ae72` |

QR payload is just the current frontend origin plus `/demo/{public-reference}?language={en|ar|fr}`. It contains no patient, staff, tenant or pharmacy identifier, credential, private instruction or clinical data. The browser generates it locally; no third-party QR service receives the URL. These deterministic references may be enumerated without exposing anything private. There is no persistent lifecycle, TTL engine, editorial publication or reference mutation: unavailable fixtures demonstrate denial only. There is no production revocation claim.

## Run locally (PowerShell)

Use .NET SDK 10.0.402 and supported Node 24 as already configured. From the repository root:

```powershell
dotnet restore MedicationInformationPlatform.sln
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore
dotnet run --project src/Api --configuration Release --no-build --no-launch-profile --urls http://127.0.0.1:5088
```

In a second terminal:

```powershell
cd frontend
npm.cmd ci
$env:API_PROXY_TARGET = 'http://127.0.0.1:5088'
npm.cmd run dev -- --host 127.0.0.1 --port 5178
```

Open `http://127.0.0.1:5178/demo`. No database is required for this journey. Stop each process with Ctrl+C. The API remains loopback-only, and Vite proxies the same-origin `/api` requests. No CORS expansion, database network binding or persistent config edit is needed. A Vite SPA fallback serves direct QR paths.

### Phone on the same trusted Wi-Fi

`localhost` or `127.0.0.1` on a phone refers to the phone. Keep the API command above, restart only Vite with:

```powershell
cd frontend
$env:API_PROXY_TARGET = 'http://127.0.0.1:5088'
npm.cmd run dev -- --host 0.0.0.0 --port 5178
```

Find the computer's Wi-Fi IPv4 address using `ipconfig`. Open `http://<computer-LAN-IP>:5178/demo` on the computer before generating the QR, so its payload uses that reachable origin. Scan it on the phone, or open the same LAN URL there. Vite accepts IP hosts with its default host checks; do not set `allowedHosts: true`. This is an explicit temporary development-only frontend binding, not a changed default. Use a trusted private network; if Windows Firewall asks, allow only the development frontend on the private network. No firewall or router rules have been changed automatically. Do not enable public-network access, port forwarding or an external tunnel. The database stays on its existing loopback port 55432; never expose it or its credentials.

Client/AP isolation, host firewall or corporate Wi-Fi policy may block phone access. Stop Vite or return to `--host 127.0.0.1` afterward. HTTP is limited to this fictional local exercise; deployed transport/security are not approved or verified. Physical-iPhone scans were subsequently reported successful by the Product Owner; see the validation record below.

## Manual walkthrough

1. Start both processes; open the frontend `/demo` using loopback for desktop or LAN origin for phone scanning.
2. Select Aster; choose **Generate public QR**. Confirm the nonclinical notice and public URL. Pebble can be selected independently.
3. Scan with a phone camera or choose **Open demonstration page**. Confirm the same fictional product and disclaimer.
4. Choose العربية: check right alignment, RTL reading flow, Arabic content/disclaimer and usable language selector. Choose Français, then English: LTR returns and product identity is retained.
5. Navigate to `/demo/unknown`, then the withdrawn and expired references above. Confirm safe unavailable message, retained disclaimer and no product details. Disconnect the API temporarily to check safe retry.
6. Use keyboard Tab/Enter and native selects; check the skip link and result-heading focus. Test 320px width, browser text enlargement and actual assistive technology before claiming broad accessibility success.
7. Return to `/` for the separate unchanged DIN metadata screen. Demo references are not DINs or private-access credentials.

## Verification and preservation

Verified merged main `cea1fdbbe92d6298b33d954a9ceadc45f3f070b6`; [post-merge run 43124430866](https://github.com/BACHAR-ALHAMADA/qr-medication-information-platform/actions/runs/43124430866) passed Backend, PostgreSQL integration and Frontend before branch creation. Initial worktree had only untracked `docs/NEXT-SESSION.md`; separate byte copy and preservation manifest are ignored under `TestResults/public-qr-preservation-c1ef2e3b8ce44a4381ac528d1f474c1b`. No stash was popped or overwritten.

Local verification: **174 backend (152 unit + 22 API; 14 new), 177 PostgreSQL and 41 frontend (13 new) tests pass**, zero final failures/skips. Both Release builds, TypeScript, zero-warning lint and frontend production build pass. The first frontend run had one new test timing failure (checking before the catalogue loaded); awaiting the rendered button fixed the assertion, with no product behavior change.

Real headless Edge on 390px mobile and 1280px desktop exercised API + Vite proxy + UI. An independent `jsQR` decoder read the rendered SVG screenshot and recovered exactly the generated public URL; that URL then opened the API-backed reader. EN/AR/FR, document direction, mobile no-horizontal-overflow, unknown/expired/withdrawn denial and original DIN entry screen passed without browser errors. Axe WCAG 2/2.1 A/AA-tagged scans of the chooser and each reader language found zero violations. This is bounded automated evidence, not accessibility certification, linguistic review or physical-device usability evidence.

Ignored screenshots: `TestResults/public-qr-browser/screenshots/{desktop-choose-qr,mobile-choose-qr,mobile-read-en,mobile-read-ar,mobile-read-fr}.png`. Browser/decoder/axe utilities are isolated in ignored TestResults, not application dependencies. Persistence evidence: `TestResults/identity-mapping-verification-8a8ea413bbcb4e6182f172eabdf963ab`; all tests ran on a new disposable cluster and original schema/role/verifier/history/baseline-count fingerprints and credential/recovery/handoff hashes stayed unchanged. No existing Identity, Organizations, RLS, role, migration or authorization source changed. No hosted CI run for this uncommitted slice is claimed.

### Dependency review

`qrcode` 1.5.4 (MIT), latest registry version at review, supplies the browser encoder; `@types/qrcode` 1.5.6 is development-only. Official [source and browser API](https://github.com/soldair/node-qrcode) and [MIT license](https://github.com/soldair/node-qrcode/blob/master/license) reviewed. Registry metadata reports last modification 2025-11-13; release cadence is limited and no maintenance/SLA guarantee is claimed. Native SVG uses only the numeric matrix, a four-module quiet zone and black-on-white modules; no injected SVG strings, canvas dependency, tracking or external QR calls. The runtime bundle grew from about 73 KB to 86 KB gzip during this slice. Exact dependency versions are locked; existing package versions and libc metadata are preserved.

`npm audit --omit=dev` reports zero vulnerabilities. Full audit reports 11 development-only findings (10 moderate, 1 high), affecting existing pinned happy-dom/ESLint/minimatch tooling; no existing version was upgraded. The high happy-dom advisory and the moderate chain are recorded as BUG-010/AI-011 for separate remediation. No automatic audit fix or major tool downgrade was attempted. This limits any claim that the entire development dependency graph is vulnerability-free.


## Final review and physical-iPhone validation - 2026-11-11

The Product Owner explicitly reports **7/7 manual checks passed** on a physical iPhone connected to the same local Wi-Fi:

1. English content displays correctly and clearly.
2. Arabic content displays correctly with right-to-left layout.
3. French content displays correctly.
4. Language switching works without errors.
5. Mobile typography and controls are usable, without problematic horizontal scrolling.
6. The fictional, nonclinical disclaimer is clearly visible.
7. QR codes for both Aster and Pebble scan successfully and open their pages.

This is owner-reported manual validation, not independently performed by the agent. Local network only; no public deployment. No clinical or medical-professional review, full accessibility certification, production QR lifecycle, production approval or real pharmacy/patient pilot is claimed. Screen-reader/zoom and qualified translation review remain limited/unperformed.

Refreshed origin/main equals baseline cea1fdbbe92d6298b33d954a9ceadc45f3f070b6, whose hosted run 43124430866 passed all three jobs. Final source review covered every modified/untracked file and the complete lockfile diff. Public fixtures, DTOs and QR payloads contain no private identifiers, secrets or clinical instructions; invalid/unknown/withdrawn/expired references fail safely. Source review covered EN/AR/FR clarity, lang/dir, accessible names, headings, native keyboard controls, result focus, visible focus and contrast, supplemented by the prior bounded browser/axe evidence. Qualified translation or assistive-technology certification is not claimed. Existing DIN, Identity/authorization, PostgreSQL schema, migrations, RLS and runtime roles are unchanged.

Final rerun: **152 unit + 22 API = 174 backend; 177 PostgreSQL; 41 frontend; total 392 passed**, zero final failures/skips. Both Release builds passed with zero warnings/errors; explicit TypeScript, zero-warning lint and frontend build passed. API tests verify safe public fields, languages, no-store, 400/404 and rejected writes. PostgreSQL used a fresh disposable target; original database fingerprints and credential/backup/handoff hashes matched. Evidence remains ignored under TestResults/identity-mapping-verification-9137c422848b47a293089bd3ba0ea7cb. Initial sandbox process restrictions and host memory exhaustion prevented startup; reruns outside restrictions with one MSBuild worker passed. Git whitespace checks pass with LF-to-CRLF normalization notices only. Runtime audit: zero vulnerabilities. Full audit: unchanged 10 moderate and 1 high development-tool findings (BUG-010/AI-011); no unrelated upgrades.

The Product Owner authorizes selective staging, the scoped feature commit, normal push and a Draft PR targeting main after review gates pass. No merge or ready-for-review action is authorized. Exclude and preserve docs/NEXT-SESSION.md, credentials, backups, stash, private screenshots and generated evidence. New hosted CI remains pending until publication and must be checked before claiming success.

## Review gate and next step

The bounded owner-reported physical-iPhone check is complete. Broader keyboard/screen-reader/zoom and qualified language review remain under HUM-002/HUM-003 and AI-008. Resolve development dependency findings in a scoped maintenance task. Broader lifecycle persistence, clinical content/review, authentication, private records, production hosting and external access require separate approval. Stop at the authorized Draft PR delivery gate; do not merge or mark ready.
