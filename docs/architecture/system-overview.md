# System overview

## Intended system

The platform will serve a public medication information page and, after a patient verification step, private instructions for a specific patient-medication assignment. Staff workflows will support authorized professionals and organizations. This is planned behavior; none is implemented in Phase 1.

## Shape

Start with a modular monolith: an ASP.NET Core REST API, a React + TypeScript web client, and PostgreSQL accessed through Entity Framework Core. Keep module responsibilities explicit and use contracts for cross-module operations. Deploy as one application initially; separate modules only when operational evidence justifies it.

```text
Patient browser ──HTTPS──> React application ──HTTPS──> ASP.NET Core API
                                                       │
                                     module contracts / application use cases
                                                       │
                                               PostgreSQL (planned)
```

The QR link resolves to a public medication view or an opaque patient access reference. Public content and private patient data use separate server-side paths. Private information is fetched only after verification and authorization.

## Cross-cutting concerns

Authentication for staff is planned around OIDC/OAuth 2.0. Tenant membership and permissions must be checked server-side. Audit, input validation, rate limiting, secure configuration, and sensitive-data-safe logging are design requirements.

## Decisions still needed

- Decision Required: deployment topology, hosting provider, and operational ownership.
- Decision Required: patient verification factor and delivery channel.
- TODO: research applicable privacy, health-information, and records-retention obligations with qualified stakeholders.
- Decision Required: source and editorial process for authoritative public medication content.
