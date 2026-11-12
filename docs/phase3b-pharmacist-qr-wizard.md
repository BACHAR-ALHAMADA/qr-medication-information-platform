# Phase 3B: synthetic pharmacist QR wizard

Status: implemented locally for review, 2026-11-11 (America/Rankin_Inlet). The Product Owner explicitly approved this synthetic four-step demonstration only. The subsequent final-review instruction authorizes selective commit, normal push and a Draft PR targeting main only after passing review gates. No merge, ready-for-review action, public deployment or further development phase is authorized.

Trace: F-001/F-002/F-003/F-004, REQ-001/REQ-002/REQ-003/REQ-007/REQ-008/REQ-009/REQ-010, T-019, AI-004/AI-008, HUM-001/HUM-002/HUM-003. Accepted ADRs remain unchanged; this is not staff authentication or an approved clinical workflow.

## Scope and acceptance

Purpose: demonstrate a guided QR preparation experience for a simulated pharmacist, with fictional packs only. Target users are the owner/reviewers exercising a synthetic workflow. Release priority is this bounded local demonstration; broader pharmacist workspace and medical-use delivery remain separately gated.

Route: `/demo/wizard`, with optional `?language=en`, `ar` or `fr`. The exact route is reserved before the existing public-reference route. `/demo`, existing QR destinations and `/` DIN lookup remain available.

| Step | Implemented acceptance behavior |
|---|---|
| 1. Select product | Existing public catalogue provides Aster/Pebble names and descriptions. Native labelled radio cards require an active valid selection before Continue. |
| 2. Preview content | Read-only preview shares the public reader's content component and API fixture data. EN/AR/FR controls, document language and Arabic RTL; explicit notice that language previews are not medically reviewed translations. No editor. |
| 3. Generate QR | Shared URL builder and native SVG matrix renderer use the selected public reference, current frontend origin and selected language. Product, link and QR stay consistent. Encoder failure gives a safe retry and Back path. Regeneration reuses the reference; it creates no lifecycle record. |
| 4. Share or print | Copy Link with manual-copy fallback, standalone SVG download retaining label/destination/warnings, and Print. Scoped print styling hides controls and preserves the QR, public URL, fictional product label and prominent warning. |

All four steps have visible progress with exactly one `aria-current="step"`. Back/Continue and restarting preserve language and product selection; choosing another product updates the QR target. Step changes focus the heading. Native controls and visible focus support keyboard input. Catalogue requests are no-store, bounded to ten seconds, aborted on unmount, and protected against stale language responses. Loading, empty, invalid response, API error and retry states retain disclaimers; missing selected products cannot advance. Copy, download and print failures show safe messages without internal error details. Pending clipboard results cannot overwrite a newer step/language/action.

## Boundaries and implementation

No new dependency, backend behavior or backend test was needed. Shared frontend helpers extract the existing validation, URL construction, public content and matrix rendering with unchanged public-read behavior. The wizard accepts only the two existing active fixture references and validates language/status/shape before use. State is browser memory only, lost on refresh; no storage, audit, patient input, tenant context, staff session, authenticated operation or QR persistence exists.

QR payloads contain only the public frontend origin, opaque public fixture reference and supported language. No patient record, health number, date of birth, staff identity, pharmacy secret, credential or private tenant data is collected or encoded. Rendering uses React text; SVG export uses numeric matrix paths and DOM text nodes, without HTML injection, external assets or third-party QR requests. Temporary download object URLs are revoked. All output remains fictional/nonclinical and explicitly unsuitable for medication use. The printout does not replace prescription labels, pharmacist counselling or required patient information.

Identity, authorization, RLS, migrations, runtime roles, database schema and original DIN implementation are unchanged. Existing BUG-010/AI-011 development-tool advisories remain outside this feature; dependency manifests and lockfile are unchanged. The final Phase 3B runtime audit is clear; full audit still reports 10 moderate and 1 high development-tool findings (BUG-010/AI-011), with no dependency changes; this does not establish that the entire toolchain is vulnerability-free.

## Verification

Preparation: refreshed `origin/main` at `d9a3f5fe69f079f874b46a643c06d8b734d1022c` (PR #7 merge). [Post-merge run 43137471288](https://github.com/BACHAR-ALHAMADA/qr-medication-information-platform/actions/runs/43137471288) passed Backend, Frontend and PostgreSQL integration. Created `feature/pharmacist-qr-wizard` from that baseline. Initial worktree contained only untracked `docs/NEXT-SESSION.md`; ignored preservation hashes/copy and stash listing are in `TestResults/wizard-preservation`.

Final local results: **174 backend (152 unit + 22 API), 177 PostgreSQL, 61 frontend (20 new wizard cases), total 412 passed**, zero final failed/skipped. Both Release builds passed with zero warnings/errors. TypeScript, zero-warning lint and frontend production build passed. Initial new-test syntax, print-mock and wrapped-SVG assertion/type issues were corrected before the passing final run; no regression-suite failure remains.

Commands used:

```powershell
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore -m:1 /nodeReuse:false
dotnet test MedicationInformationPlatform.sln --configuration Release --no-build --no-restore --verbosity minimal -m:1 /nodeReuse:false
dotnet build tests/Persistence/Foundation.PersistenceTests.csproj --configuration Release --no-restore -m:1 /nodeReuse:false
# Inspected, pre-existing ignored harness: disposable database plus preservation checks.
powershell -NoProfile -ExecutionPolicy Bypass -File TestResults/run-identity-mapping-verification.ps1
cd frontend
npm.cmd test
npm.cmd exec --no -- tsc --noEmit
npm.cmd run lint
npm.cmd run build
```

PostgreSQL evidence: `TestResults/identity-mapping-verification-d570acfd57a14aa6803c28a5fe2e1f32`. All migrations/setup ran only on a fresh disposable cluster. Original development schema/role/verifier/history/count fingerprints and credential/recovery/handoff hashes matched afterward. The expected refusal of a second bootstrap was successful safety verification. Backups, credentials, safety stash and handoff remain preserved.

Live headless Edge used the actual API and Vite proxy at desktop 1280px and mobile 390px, with a 320px overflow check. All four steps, keyboard selection/Continue, heading focus, faithful preview, both products, EN/AR/FR/RTL, language switching, clipboard, download, print-media CSS/PDF, API failure/retry and existing demo/DIN entry/unavailable paths passed with no browser errors. Independent jsQR decoding recovered the exact public destination from on-screen QR, standalone downloads in all three languages and the print-media QR. Download text fitted its SVG bounds. Axe WCAG 2/2.1 A/AA-tagged scans at every step and all sharing languages found no violations. Screenshots were visually inspected, including desktop selection, Arabic mobile sharing and print layout. Ignored browser evidence is in `TestResults/public-qr-browser/wizard-results`; no browser tools were added to application dependencies.

These are bounded automated checks, not full accessibility certification, qualified translation/clinical review, a physical printer test or new iPhone evidence. The owner's prior 7/7 physical-iPhone report applies to Phase 3A, not this wizard. Real pharmacy/patient pilots, production QR lifecycle, public hosting and clinical approval remain absent.

## Local startup and iPhone walkthrough

From the repository root, using the configured .NET SDK and Node 24:

```powershell
dotnet restore MedicationInformationPlatform.sln
dotnet build MedicationInformationPlatform.sln --configuration Release --no-restore -m:1 /nodeReuse:false
dotnet run --project src/Api --configuration Release --no-build --no-launch-profile --urls http://127.0.0.1:5088
```

In a second terminal:

```powershell
cd frontend
npm.cmd ci
$env:API_PROXY_TARGET = 'http://127.0.0.1:5088'
npm.cmd run dev -- --host 127.0.0.1 --port 5178
```

Open `http://127.0.0.1:5178/demo/wizard`. No database is required. For a physical iPhone on the same trusted private Wi-Fi, keep the API loopback-only and restart only Vite:

```powershell
cd frontend
$env:API_PROXY_TARGET = 'http://127.0.0.1:5088'
npm.cmd run dev -- --host 0.0.0.0 --port 5178
```

Use `ipconfig` to find the computer's Wi-Fi IPv4 address. Open `http://<computer-LAN-IP>:5178/demo/wizard` on the computer **before generating**, or open the wizard at that address on the phone. The QR/copy/download/print destination then uses that same reachable origin. `localhost`/loopback on an iPhone refers to the phone. A LAN HTTP origin may lack clipboard access; the manual-copy fallback remains available. Client isolation/firewall settings may block LAN access; any private-network firewall permission is an owner choice. No firewall, router, database binding, host-check bypass, public tunnel or deployment was added.

1. Select Aster, Continue, inspect the read-only preview and warning; switch EN/AR/FR and confirm RTL/LTR.
2. Continue to generate; scan the QR on the iPhone and confirm product/language/disclaimer at the public page.
3. Continue to share; copy/open the link, download the labelled SVG, and inspect print preview. Confirm every destination matches and the warning remains prominent.
4. Go Back and restart with Pebble; repeat the scan and check state preservation. Review narrow layout, keyboard, text enlargement and assistive technology separately.
5. Stop both servers with Ctrl+C after testing or restore Vite's loopback binding. LAN HTTP is for this fictional local exercise only; no production transport claim.

## Local review gate

Owner review should cover the four-step workflow, all languages/RTL, physical-iPhone scans for both products, SVG sharing and actual print output/warnings, plus keyboard/zoom/screen-reader needs (HUM-002/HUM-003, AI-008). No new physical-device or printer result is assumed. Broader clinical, staff-authentication and persistence scope needs separate approval. Final scoped source review and repeat local verification passed; proceed only through the authorized Draft PR delivery and private end-of-day handoff, then stop. No Phase 3C or further development today.

## Final delivery review checkpoint

The Product Owner separately authorized final review, GitHub Draft delivery and an end-of-day handoff. Refreshed origin/main still equals the expected PR #7 merge baseline; its three CI jobs remain successful. Reviewed every modified/untracked Phase 3B implementation, test and documentation file, including shared helpers and the excluded handoff. No blocking security/privacy, medical-safety, accessibility or regression defect was found within the documented synthetic scope. No product code changed during final review.

Repeat verification passed: 174 backend + 177 PostgreSQL + 61 frontend = 412 tests, zero failures/skips; both Release builds have zero warnings/errors; TypeScript, lint and frontend build pass. PostgreSQL final evidence: TestResults/identity-mapping-verification-d2e6a2bfd33c497185e552815a26fe56. Original database fingerprints and credential/backup/handoff hashes matched. Refreshed audit: zero runtime findings; unchanged 11 development-tool findings. Final browser walkthrough again passed all four steps, both products, languages/RTL, keyboard/focus, copy, SVG download, print, independent decoding, bounded axe and original demo/DIN entry/unavailable paths. Physical-iPhone testing of this NEW wizard and actual-printer verification remain pending; earlier Phase 3A iPhone checks do not validate Phase 3B.

Selectively exclude docs/NEXT-SESSION.md, credentials, backups, stash and disposable/generated evidence. Hosted CI is pending until publication and must be inspected for actual counts. The PR description and private handoff will record final delivery/hosted results; no hosted success is assumed in this pre-commit checkpoint. Preserve the previous handoff text and byte copy when prepending the current end-of-day record.
