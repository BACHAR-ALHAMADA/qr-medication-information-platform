# Project guidance

## Purpose and status
QR Medication Information Platform is intended to let a patient scan a medication QR code, view public medication information, and—after a verification step—view private instructions assigned to them. Phase 2A provides a minimal read-only DPD product-metadata backend; Phase 2B adds an exact-DIN React lookup interface for review. QR flows, patient-specific functionality and clinical publication remain unimplemented. Consult `docs/project-status.md` for actual verification status; do not imply planned capabilities are implemented.

## Architecture and technology
- Begin as a modular monolith using C# / ASP.NET Core Web API, React + TypeScript, PostgreSQL, and Entity Framework Core.
- Use REST over HTTPS; staff authentication is planned around OpenID Connect / OAuth 2.0. Docker, Git, xUnit, integration/security testing, and OpenAPI are planned technologies.
- Keep Domain, Application, Infrastructure, and API responsibilities distinct. Modules own their domain rules and expose intentional interfaces. Do not introduce microservices without a demonstrated operational need.
- Keep the design horizontally scalable; do not add Redis, a message broker, Kubernetes, or cloud-specific infrastructure without a concrete requirement.

## Development principles
- Prefer the smallest clear solution that meets a documented requirement. Avoid unnecessary dependencies, abstractions, frameworks, and speculative features.
- Preserve existing work. Inspect before editing; never delete or overwrite unrelated files.
- Keep API handlers focused on transport concerns; put use cases in Application and business rules in Domain. Infrastructure implements persistence and external technical concerns.
- Validate untrusted input at system boundaries. Use established standards and maintained libraries for authentication and cryptography; do not invent custom mechanisms.
- Keep configuration and secrets out of source control. Use HTTPS in deployed environments.

## Security principles
- Enforce authentication and authorization on the server for every protected operation. UI visibility is not an access control.
- Apply least privilege, role/permission checks, rate limits where appropriate, secure secret handling, and audit coverage for sensitive access and changes.
- Never write sensitive patient or health information, credentials, verification factors, or bearer tokens to logs. Redact and minimize operational telemetry.
- Public medication information and private patient-specific information have distinct data and API paths. Never send private information to a browser before successful authorization/verification.

## Privacy principles
- Collect, retain, expose, and log only information needed for a defined purpose. Prefer opaque internal/public references over direct identifiers.
- Classify data and document its purpose, access, and retention assumptions. Do not invent legal or regulatory obligations; record research needs as TODO / Decision Required.

## Multi-tenancy
- Tenant context is mandatory for tenant-owned staff operations. Scope reads and writes by verified membership and tenant, and preserve tenant ownership through application and persistence boundaries.
- Do not trust a tenant ID supplied by a client as proof of membership. Avoid cross-tenant data exposure in errors, logs, and APIs.
- Choose and document the database isolation mechanism before implementing persistence; test cross-tenant access boundaries.

## QR security
- Never put patient information, medication details, health numbers, or other sensitive information inside QR codes.
- Patient-specific QR codes contain only an opaque, high-entropy, non-guessable identifier. Treat it as a bearer secret: support expiry/revocation, avoid logging it, and require a separate verification step before private data is disclosed.
- Public product references must not grant access to patient records. Store token verifiers rather than raw tokens where practical.

## Testing expectations
- Add xUnit unit tests for domain/application behavior and integration tests for API and persistence boundaries when implementation begins.
- Include security tests for authorization, tenant isolation, QR access, validation, and sensitive-data leakage. Do not claim tests exist until added and run.
- Keep tests deterministic and avoid real patient data or production secrets.

## Documentation expectations
- Update architecture, security, privacy, and decision records when decisions change. Mark unresolved matters `TODO` or `Decision Required` rather than guessing.
- README and status documentation must distinguish implemented functionality from plans.
- Follow the feature record and status conventions in `docs/product-requirements.md`. Separate approved decisions/principles, proposals awaiting Product Owner approval, assumptions, research questions, release priority, and implementation status.
- Preserve accepted ADRs; flag conflicts and propose resolutions rather than silently changing accepted architecture or security decisions.
- Use stable F-/REQ-/J-/REG-/T-/ADR- and HUM-/AI-/BUG-/FIX- identifiers with cross-links, following `docs/product-requirements.md`; never renumber or reuse identifiers. Planned test records do not mean executable tests exist or have passed.
- Treat simple mobile-first UX, progressive disclosure, multilingual accessibility, and RTL as core product principles. Patient-specific functionality remains future scope until explicit MVP approval; mandatory security principles still apply whenever introduced.
- Use synthetic patient data only, including in research and design materials. Regulatory and medical accuracy questions require evidence and qualified review; do not present them as verified conclusions.

## Task and issue tracking
- If the AI discovers a bug, record it in `tasks/BUGS.md`.
- If the AI fixes a meaningful bug, record the corrective change in `tasks/FIXES.md`.
- If something requires the human/project owner to decide, approve, provide information, or perform an external action, add it to `tasks/HUMAN-TODO.md`.
- If the AI identifies a necessary technical task it can perform later, add it to `tasks/AI-TODO.md`.
- Do not create speculative TODO items.
- Do not mark `HUMAN-TODO.md` items complete without human confirmation.
- Before starting a new phase, review the task and bug files.
- Keep these files concise and current.

## Dependency rules
- API depends on Application/module interfaces, not directly on Domain internals or persistence implementations.
- Application coordinates use cases and depends on domain abstractions, not on API or concrete Infrastructure.
- Domain contains business concepts/rules and has no dependency on API, database, framework, or Infrastructure concerns.
- Infrastructure implements persistence and technical interfaces; module-to-module communication should use explicit contracts, not direct table access.
- BuildingBlocks are for genuinely shared primitives only; do not turn them into a dumping ground.

## Coding expectations
- Use clear, conventional C# and TypeScript; prefer explicit names and small cohesive units.
- Handle errors without leaking sensitive or cross-tenant details. Avoid logging request bodies or secret-bearing URLs.
- Keep public content provenance/versioning distinguishable from patient-specific instructions.
