# ADR-003: Opaque QR access references

- Status: Accepted as a security principle; implementation details pending
- Date: 2026-10-04

## Context

QR codes may be photographed, copied, or exposed. Encoding patient or medication facts would disclose sensitive information and couple printed artifacts to internal identifiers.

## Decision

Patient-specific QR codes contain only an opaque, high-entropy, non-guessable random identifier. The server maps it to a narrowly defined target. Treat the identifier as a bearer secret and require a separate verification step before private information is returned. Store a token verifier/hash rather than the raw token where practical.

## Consequences

Token possession alone does not authorize private data. Revocation, expiry, rate limiting, and careful logging are required design concerns. The public catalogue identifier remains separate from the patient access token.

## Decision Required

Choose token encoding/length, lifetime, reuse policy, revocation and reprint workflow, verification factor, and threat-model details during implementation planning.
