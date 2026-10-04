# ADR-001: Start with a modular monolith

- Status: Accepted for initial architecture
- Date: 2026-10-04

## Context

The platform needs clear boundaries across public catalogue, patient medication, QR access, identity, organizations, verification, and audit concerns. The initial team and operational requirements do not establish a need for independent service deployment.

## Decision

Build initially as a modular monolith with explicit module boundaries and Domain, Application, Infrastructure, and API responsibilities. Keep cross-module interaction behind contracts. Do not introduce microservices at this stage.

## Consequences

This keeps deployment and operations simpler while preserving a path to extract modules if independent scaling, availability, release cadence, or ownership later justifies the cost. Modules must avoid shared-table coupling to keep that option credible.

## Revisit when

Measured operational constraints or organizational ownership make independent deployment beneficial enough to outweigh distributed-system complexity.
