# Project status

Last updated: 2026-10-04

## Current phase

**Phase 1 — foundation and documentation.** Initial solution/repository structure, working agreements, architecture notes, security/privacy starting guidance, and decision records are being established.

## Implemented

- Project foundation and documentation only.

## Not implemented

- API or frontend behavior.
- Authentication, authorization enforcement, patient verification, or QR generation/resolution.
- Database schema, EF Core model, migrations, or data.
- Dashboards, external integrations, deployment, or medical content.

## Decisions required / TODO

- Patient verification factor and delivery channel.
- Staff identity provider, MFA, account lifecycle, and role/permission matrix.
- Patient matching data and minimization approach.
- Tenant isolation details, including whether PostgreSQL row-level security will supplement application scoping.
- Authoritative public content sources, provenance and editorial/revision workflow.
- Deployment/hosting, availability, scale, backup and recovery objectives.
- Retention, deletion, privacy workflows, and qualified legal/regulatory research.
- QR token lifecycle and printed-label reassignment/revocation procedures.
- Threat-model workshop and security/privacy review before pilot.

No legal or regulatory conclusion is made by this project status document.
