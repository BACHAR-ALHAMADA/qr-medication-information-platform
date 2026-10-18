# QR Medication Information Platform

A planned platform for medication information accessed by scanning a QR code. The intended patient experience is to show general medication information immediately and reveal patient-specific instructions only after a simple verification step.

## Problem and goal

Medication packages and labels can direct patients to useful information, but public product guidance and a patient's individual directions have different privacy and access needs. This project aims to present both through a simple scan flow while protecting private health information.

QR codes are intended to carry only opaque, random references—not patient details, medication details, health numbers, or other sensitive information.

## Architecture

The planned starting point is a modular monolith: one ASP.NET Core API and a React application, organized into modules with clear Domain, Application, Infrastructure, and API boundaries. PostgreSQL is the planned database. Modules should communicate through explicit contracts so they can be extracted later if a real operational need arises. Microservices are not part of the current plan.

## Planned technology

- Backend: C# / ASP.NET Core Web API
- Frontend: React + TypeScript
- Database and ORM: PostgreSQL + Entity Framework Core
- API: REST over HTTPS with OpenAPI documentation
- Staff authentication: OpenID Connect / OAuth 2.0
- Containers and version control: Docker and Git
- Testing: xUnit plus integration and security testing

These are planned technologies; application functionality has not been implemented.

## Development status

**Phase 1 — project foundation, discovery and documentation.** This repository contains product discovery, requirements, UX/UI planning, architecture, security, and privacy documentation. There is no implemented API, frontend, database schema, authentication, QR flow, or medication content yet.

## Planned modules

- Medication Catalogue: public medication information and official document references.
- Patient Medication: patient-specific assignments and instructions.
- QR Access: opaque QR references and access lifecycle.
- Identity: staff identity and access integration.
- Organizations: pharmacies, manufacturers, and tenant membership.
- Verification: patient verification flow.
- Audit: security-relevant access and change records.

## Repository layout

```text
MedicationInformationPlatform.sln
AGENTS.md, README.md, .gitignore
.codex/skills/                 Local project skill space (currently empty)
docs/architecture/             System and module architecture
docs/security/                 Threat model and access/QR security
docs/privacy/                  Data classification and privacy model
docs/decisions/                Architecture decision records
src/                            Planned API, building blocks, and modules
frontend/                       Planned React application
tests/                          Planned unit, integration, security, architecture tests
infrastructure/                 Planned environment-specific infrastructure space
```

## Product discovery

The current work is brainstorming, requirements analysis, and UX/UI planning within Phase 1. The initial target market is Manitoba, Canada. Calm mobile-first design, simple UX with progressive disclosure, multilingual accessibility, and RTL support are core product principles. Patient-specific instructions remain a future capability until MVP scope is explicitly approved; existing security requirements remain mandatory whenever introduced.

Phase 1B documents a standalone central reviewed catalogue, pharmacist information finding without full-description authoring, and no-account public QR reads. Patient understanding, inclusion, avoidable-paper reduction where permitted, and future digital healthcare efficiency are objectives to evaluate, not demonstrated benefits. See [integration strategy](docs/integration-strategy.md), [pilot and impact evaluation](docs/pilot-and-impact-evaluation.md), and the [proposed public demonstration](docs/project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration). No implementation, PMS connection or real-world pilot is authorized by these plans.

Start with the [product vision](docs/product-vision.md), [requirements and documentation standard](docs/product-requirements.md), [feature catalogue](docs/feature-catalogue.md), and [roadmap](docs/project-roadmap.md).

- Experience: [personas](docs/user-personas.md), [journeys](docs/user-journeys.md), [UX blueprint and research](docs/ui-ux-blueprint.md), [design system](docs/design-system.md), [accessibility/localization](docs/accessibility-and-localization.md).
- Governance: [medication content](docs/medication-content-governance.md), [privacy/security overview](docs/privacy-security.md), [regulatory research](docs/regulatory-research.md), [business discovery](docs/business-model.md).
- Engineering direction: [technical architecture](docs/technical-architecture.md), [accepted ADRs](docs/decisions/), [testing strategy](docs/testing-strategy.md).
- Tracking: [AI tasks](tasks/AI-TODO.md), [human decisions](tasks/HUMAN-TODO.md), [bugs](tasks/BUGS.md), [fixes](tasks/FIXES.md).

See [project status](docs/project-status.md) for current progress and unresolved decisions. Proposed features and planned tests are not approved MVP scope or implemented functionality.
