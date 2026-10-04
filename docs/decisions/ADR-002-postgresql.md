# ADR-002: Use PostgreSQL as the planned relational database

- Status: Accepted for planned stack; implementation pending
- Date: 2026-10-04

## Context

The system is expected to store structured catalogue, organization, assignment, and access-control data with relationships and transactional workflows. The chosen technology direction names PostgreSQL and Entity Framework Core.

## Decision

Plan to use PostgreSQL as the system of record, accessed through Entity Framework Core. Database schema, migrations, tenant isolation mechanisms, hosting, backup, and recovery are out of scope for Phase 1.

## Consequences

Relational constraints and transactions suit the expected data relationships. Application/module boundaries should own data access. Tenant scoping must be designed into persistence from the first implementation; the exact use of row-level security remains unresolved.

## Decision Required

Select provider/hosting and production operational settings. Evaluate whether PostgreSQL row-level security supplements application-level isolation.
