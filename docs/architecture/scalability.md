# Scalability

## Initial approach

Keep the first release a modular monolith with stateless API instances where practical and PostgreSQL as the system of record. This keeps deployment and debugging manageable while allowing horizontal API scaling behind a load balancer when needed.

## Preserve options without premature infrastructure

- Keep module interfaces and data ownership clear; avoid cross-module table access.
- Keep request processing stateless where possible. Any future distributed session or background work should be added only for a demonstrated need.
- Use database indexes, connection management, caching, queues, or separate services in response to measured bottlenecks and requirements.
- Do not add Redis, a message broker, Kubernetes, or cloud-specific services in this foundation phase.
- Maintain tenant identifiers and enforce scoping from the beginning so a later tenant-isolation change is tractable.

## Future extraction criteria

Consider extracting a module only when its independent scaling, availability, deployment cadence, fault isolation, or team ownership needs justify the added network, data consistency, security, and operational costs. Define module contracts first; do not assume extraction is required.

## Decision Required

Hosting topology, availability targets, expected load, backup/recovery objectives, and tenant-specific isolation requirements need stakeholder input before production design.
