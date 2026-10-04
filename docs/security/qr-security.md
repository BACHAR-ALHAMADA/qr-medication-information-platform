# QR security

## Token contents

Patient-specific QR codes must contain only an opaque, cryptographically random, high-entropy, non-guessable identifier (typically in a URL). Never encode a patient name, medication, dose, health number, tenant details, database key, or other sensitive information. Public product references must not grant access to patient records.

## Token lifecycle

Treat the token as a bearer secret because anyone with a copy can present it. Generate it with an established cryptographic random-number generator and sufficient entropy; store a verifier/hash rather than the raw token where practical. Associate it server-side with a narrow target, status, creation metadata, and expiry/rotation policy. Support revocation and replacement.

## Access flow

Resolving a token is not authorization to disclose private data. Show public content independently. Require a separate patient verification step, rate-limit attempts, make challenges short-lived, and issue only narrow short-lived access after success. Do not disclose whether a target exists through distinguishable errors.

## Leakage controls

Do not log full QR URLs or tokens. Consider browser referrer policy and avoid third-party resources on token-bearing pages. Use HTTPS and prevent tokens from appearing in analytics, error telemetry, or support screenshots where practical.

## Decision Required

Token length/encoding, lifetime, one-time versus reusable behavior, reassignment/reprint workflow, and challenge policy depend on the final threat model and operational workflow.
