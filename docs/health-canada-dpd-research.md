# Phase 1C — Health Canada medication data research

Research date: 2026-10-17. Status: Completed read-only feasibility investigation; implementation and public use of imported content require Product Owner approval. No application code, schemas, dependencies, patient records, PMS connections or production catalogue imports were created. Real public medication product data was used as explicitly requested; these observations are not patient advice or medical review.

Trace: F-001/F-004, REQ-001/REQ-004/REQ-010, REG-004/REG-007, HUM-001/HUM-003/HUM-007/HUM-008, AI-003/AI-007 and planned T-011. Existing accepted ADRs and mandatory privacy/security requirements are unchanged.

## Conclusion

The public DPD API is a feasible candidate for product identity and ingredient metadata. Actual unauthenticated GET requests succeeded for three Canadian human drug products. Official monograph links were found on the associated DPD HTML pages, not in the tested API JSON. Recommend a small backend source-lookup adapter after approval, separated from the reviewed public catalogue. Do not turn a successful DPD lookup into automatic publication, translation approval or clinical guidance.

## Official sources and evidence boundaries

Sources accessed on the research date:

- [DPD API guide](https://health-products.canada.ca/api/documentation/dpd-documentation-en.html): base URI, query parameters, related resources and documented field limitations. It describes JSON/XML and English/French responses. Its `last_update_date` definition covers some key-field changes, not every change; do not use it alone as a complete change feed. No monograph-link endpoint was found in this guide.
- [Health Canada DPD access page](https://www.canada.ca/en/health-canada/services/drugs-health-products/drug-products/drug-product-database.html): describes nightly DPD updates and access to human-drug monographs. This does not establish an API SLA, guaranteed per-product freshness or stock availability.
- [DPD dataset listing](https://open.canada.ca/data/en/dataset/bf55e42a-63cb-4556-bfd8-44f26e5a36fe): identifies Health Canada and Open Government Licence — Canada, and lists API resources.
- [Open Government Licence — Canada](https://open.canada.ca/en/open-government-licence-canada): describes reuse including commercial use subject to attribution and conditions; excludes third-party rights the provider cannot license and does not grant endorsement. This is source evidence, not a legal opinion on this platform's use or every linked document.
- [Health Canada monograph FAQ](https://www.canada.ca/en/health-canada/services/drugs-health-products/drug-products/applications-submissions/guidance-documents/product-monograph/frequently-asked-questions-product-monographs-posted-health-canada-website.html): not all drugs have monographs, language availability can vary, and documents for products sharing an ingredient can differ. Do not substitute another product's document based on ingredient alone.
- [Microsoft IHttpClientFactory guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory): supports configuring a named/typed outbound HTTP client through dependency injection; see the proposed architecture below.

No account, API key, authorization header or patient information was supplied for the tested public requests. This proves current read access from this environment, not guaranteed future access or unrestricted reuse. Initial web-fetch attempts returned tool-level 502 errors; sandboxed PowerShell also failed to connect. Authorized direct HTTPS requests then returned HTTP 200. Those tool/environment failures are not evidence of a confirmed DPD outage.

## Read-only test results

Requests used PowerShell's existing HTTP tooling, with no installed packages. Product/ingredient/form/route/status responses below were directly observed, not copied from documentation examples. All 15 English requests (five resources for each product) returned HTTP 200 and JSON. All three products were classified Human, Tablet, Oral and Marketed in the returned data. Strength is product ingredient strength, not a recommended dose.

| Selected product | DIN (string) | DPD drug_code | Active ingredient | Strength / unit | Company | API last_update_date |
|---|---|---|---|---|---|---|
| TYLENOL EXTRA STRENGTH | `00559407` | `5255` | ACETAMINOPHEN | `500` / `MG` | KENVUE CANADA INC. | 2025-09-25 |
| ADVIL TABLETS | `01933558` | `13625` | IBUPROFEN | `200` / `MG` | HALEON CANADA ULC | 2026-09-24 |
| GLUCOPHAGE | `02099233` | `17104` | METFORMIN HYDROCHLORIDE | `500` / `MG` | SANOFI-AVENTIS CANADA INC | 2026-06-09 |

Reproducible product queries: [Tylenol DIN lookup](https://health-products.canada.ca/api/drug/drugproduct/?din=00559407&lang=en&type=json), [Advil DIN lookup](https://health-products.canada.ca/api/drug/drugproduct/?din=01933558&lang=en&type=json), [Glucophage DIN lookup](https://health-products.canada.ca/api/drug/drugproduct/?din=02099233&lang=en&type=json). These return observations of mutable source records; they are not fixed test fixtures or product endorsements.

Base for the following tested resources: `https://health-products.canada.ca/api/drug/`. Replace `{DIN}` or `{code}` only with the corresponding values in the table above:

| Tested relative GET request | Observed content and shape |
|---|---|
| `drugproduct/?din={DIN}&lang=en&type=json` | Array with one record for each tested full DIN: brand, DIN, internal drug_code, class, descriptor, ingredient count/group, company, last-update date; no monograph URL |
| `activeingredient/?id={code}&lang=en&type=json` | Array with ingredient name, strength and unit; dosage_value and dosage_unit were empty strings for these products |
| `form/?id={code}&lang=en&type=json` | Array with pharmaceutical form code/name |
| `route/?id={code}&lang=en&type=json` | Array with administration route code/name |
| `status/?id={code}&lang=en&type=json` | Single object with status, history/original-market dates, status code, expiration_date and lot_number; expiration_date was null and lot_number empty in these records |

`id` in these related calls is DPD `drug_code`, not DIN. Preserve DIN leading zeros. Preserve ingredient strength/unit and any denominator fields without converting them into dosing instructions. All samples here have one ingredient and solid tablet form; combinations, liquids, salts, modified-release forms and missing ingredients remain untested.

Additional observed probes:

- `drugproduct/?brandname=TYLENOL%20EXTRA%20STRENGTH&status=2&lang=en&type=json` returned two rows; `brandname=ADVIL` with the same filter returned 25; `brandname=GLUCOPHAGE` returned one. Brand search can include multiple variants; it must not automatically select the first result. The product selections above were confirmed by full DIN and related resources.
- French GETs for Advil product, ingredient and form (`lang=fr`) all returned HTTP 200. The brand remained `ADVIL TABLETS`; class was `Humain`, ingredient `Ibuprofène`, form `Comprimé`. Language selection does not imply all text is translated. Other products' French responses and all French monograph links were not tested.
- `drugproduct/?din=00000000&lang=en&type=json` returned HTTP 200 with `[]`. A no-match result must be distinguished from an upstream failure.
- An English Advil product response reported `application/json; charset=utf-8`, `Cache-Control: no-cache`, and `Access-Control-Allow-Origin: *`; no ETag, Last-Modified or Retry-After header was present in that sample. These observations are not a permanent header contract or proof of absent rate limits. No browser CORS test, load test, quota test or availability study was performed.

## Official product monograph links

These exact links were extracted from the official English DPD product pages. Each page returned HTTP 200. Each PDF URL then returned HTTP 200 to a HEAD request with Content-Type `application/pdf`. PDF bodies were not downloaded, parsed, medically reviewed, translated or checked for internal DIN coverage/accessibility. The page's document date is not the API product last-update date.

| Product | Official DPD page | Linked PDF | Document date displayed on page |
|---|---|---|---|
| Tylenol Extra Strength | [code 5255](https://health-products.canada.ca/dpd-bdpp/info?code=5255&lang=eng) | [00076531.PDF](https://pdf.hres.ca/dpd_pm/00076531.PDF) | 2024-07-03 |
| Advil Tablets | [code 13625](https://health-products.canada.ca/dpd-bdpp/info?code=13625&lang=eng) | [00080634.PDF](https://pdf.hres.ca/dpd_pm/00080634.PDF) | 2025-05-22 |
| Glucophage | [code 17104](https://health-products.canada.ca/dpd-bdpp/info?code=17104&lang=eng) | [00084167.PDF](https://pdf.hres.ca/dpd_pm/00084167.PDF) | 2026-04-07 |

The HTML page uses `lang=eng`; API probes used `lang=en` or `fr`. Do not construct a PDF filename from a DIN or drug_code, reuse a search-engine historical PDF, or assume the current document link is immutable. Prefer a reviewed source-page link initially; a curated direct PDF reference needs recorded language, association, date and periodic rechecking. Automated HTML extraction is not a documented monograph API contract and is not approved for implementation. Missing monographs need an explicit unavailable state, not a different drug's document.

## Gaps, risks and decisions

| Area | Finding / unresolved need | Owner / trace |
|---|---|---|
| Patient information | Tested JSON has no structured indications, warnings, adverse effects, interactions, patient dosing instructions or patient leaflet text. A linked PDF is a separate source, not a ready-to-render reviewed mobile page. | HUM-003, F-004, T-004 |
| Language/accessibility | English/French metadata does not supply RTL translations, audio, simplified material or proven screen-reader-friendly content. Any derived patient material needs medical/language review and version linkage. | HUM-002/HUM-003, F-003/F-005 |
| Licensing | Dataset listing supplies a licence signal; confirm attribution, commercial display, retention, redistribution and derivation policies. Do not assume its scope covers sponsor-authored monograph copying, hosting, extraction, translation or audio. Assess link-only versus reproduced uses separately. | HUM-003/HUM-007, REG-004 |
| Operations | No numerical quota, guaranteed response time, SLA, pagination or complete delta-feed contract was established. Define modest traffic, bounded queries, retries, freshness/revalidation and outage behavior before a live dependency. Do not treat nightly DPD updates as an API freshness guarantee. | HUM-008, AI-007 |
| Integrity | Brand search is ambiguous; endpoint shapes differ; empty strings and nulls exist; source dates differ. Combine DIN, form, ingredients, strength and status for product confirmation. Do not infer local pharmacy stock from Marketed. | F-001/F-004, T-011 |
| Publication | Upstream metadata and regulatory status are distinct from our editorial approval. Corrections/withdrawals must affect reviewed catalogue eligibility under an approved policy. A successful import cannot set our content to medically approved. | HUM-001/HUM-003, T-004 |
| Coverage | Three single-ingredient tablets are a feasibility sample only. Cancelled/dormant products, combinations, liquids, French PM availability, link changes, incomplete records and vendor/PMS compatibility were not validated. | AI-007, T-011; PMS remains HUM-011 |

No project application bug was discovered: the repository has no implemented DPD client. External response variations are integration requirements/risks, not fixed runtime defects. Existing BUG/FIX records are unchanged.

## Proposed ASP.NET Core implementation plan — approval required

1. **Approve the slice and source policy.** HUM-001 approves a nonclinical read-only DPD source lookup for the three DINs and its acceptance criteria. HUM-003/HUM-007 review reuse and monograph-link policy; HUM-008 approves traffic/freshness/outage assumptions. Research authorization does not approve a production connection or patient-facing publication. Retain the synthetic public demonstration until real product display is separately approved.
2. **Keep the existing modular boundaries.** Define a narrow source-lookup interface in MedicationCatalogue Application. Infrastructure implements the DPD adapter; Domain contains source-independent product identity and review rules. API handles validated transport input and calls the Application use case. React consumes our backend, not DPD directly. This keeps source policy/error mapping centralized without adding a microservice, broker or Redis.
3. **Use a configured outbound client.** Proposed Infrastructure client uses `HttpClient` via `IHttpClientFactory` with fixed HTTPS base address, cancellation, finite timeout and response-size limits; `System.Text.Json` maps external snake_case fields into source DTOs. [Microsoft guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) explains named/typed client configuration. Choose the supported .NET version separately; no package installation or version choice was made here.
4. **Lookup exact product identity first.** Proposed internal source-lookup capability accepts an eight-digit DIN string and supported source language. Resolve product and enforce exact DIN matching; retrieve ingredients, form, route and status using its drug_code with bounded concurrency. Normalize each resource's actual object/array shape explicitly, preserving null/empty fields and all ingredient rows. Reject inconsistent associations and incomplete required responses; do not publish a partial record as complete. Return no-match, incomplete and unavailable outcomes distinctly.
5. **Separate metadata from publication.** Return a candidate product reference with source URI, retrieval time, provider identifier and observed update dates. Keep a DPD page link initially; direct PDFs need manual association checks and the approved policy above. No automated PDF extraction, HTML scraper, generated medical summary or translation is part of this slice. No database schema is needed for an initial nonpersistent lookup; persisted provenance/revisions require a later approved design consistent with ADR-002 and HUM-006.
6. **Control outbound access and failures.** Fixed/allowlisted destinations, safe redirects and encoded parameters prevent caller-controlled URL fetching. Supply no patient data, tenant identifiers, credentials or request bodies upstream. Add bounded transient GET retries only under an agreed policy, honor throttling signals, and distinguish upstream errors from empty results. Respect source cache directives; do not assume shared caching is allowed because the content is public. Any retained snapshot and stale-content fallback needs explicit freshness and rights review. Log minimal status/timing, with no bodies or secret-bearing URLs.
7. **Verify before enabling live access.** Planned T-011 uses deterministic mocked responses for the measured cases plus combinations/liquids, leading-zero DINs, invalid inputs, wrong DIN/code, nulls, object/array differences, no-match, partial failure, timeout, malformed JSON and 429/5xx. Add API integration tests and ensure source candidates cannot bypass T-004 publication gates. Optional low-volume live smoke checks must be explicitly enabled and kept out of deterministic CI; use no patient fixtures. A separate manual link check verifies the source-page association and broken-link handling.
8. **Review evidence and approve expansion.** Demonstrate accurate metadata for the three DINs, explicit failure states and preserved provenance without publishing clinical guidance. Only then consider broader search, controlled ingestion, persistent catalogue versions, translations or live public deployment. None of those capabilities or PMS/private-patient features is approved by this plan.

### Proposed acceptance gate for the first adapter slice

- Full DINs resolve to the expected product identities, ingredient strengths, form, route and observed status; no first-brand-result selection or DIN-to-drug_code confusion.
- Source timestamps and source links are visible as provenance; they are not represented as local medical approval or guaranteed current clinical guidance.
- Unknown DIN, partial source failure, invalid language, timeout and unavailable monograph remain explicit without substituting another product or leaking internal details.
- No patient data, tenant writes, schema, automatic medical summary, unreviewed translation or public catalogue publication is introduced.
- Deterministic tests pass and the Product Owner reviews the implementation diff before any commit/deployment decision. These are planned checks, not tests already added or passed.
