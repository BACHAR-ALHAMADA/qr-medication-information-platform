# Proposed user journeys

Status: Discovery scenarios, not approved release scope. Use synthetic content and no real patient records.

## J-001 — Find public information (F-001, F-003, F-004)

1. Person scans a public medication reference on a phone.
2. Entry page identifies the public information and offers a discoverable language choice.
3. Person sees essential information and safety content, then expands additional detail as needed.
4. Person can inspect source/revision context and navigate without losing their place.

Research invalid references, unavailable content/translations, small screens, slow connections, text enlargement, RTL, and screen reader use. Safe fallback wording and operational escalation routes await approval; do not fabricate medical advice. Validate through T-001 to T-004.

## J-002 — Pharmacist workspace (F-002)

1. Authorized pharmacist enters a staff workspace.
2. The service establishes identity, permissions, and verified tenant membership.
3. Pharmacist performs a task selected through workflow research.
4. Clear status and recovery feedback support completion.

The task set, publication powers, and QR handling are Decision Required (HUM-001, HUM-003, HUM-005). Test permission denial, tenant boundaries, interruption, and error recovery through T-002, T-005, T-006.

Phase 1B adds a public-only variant of this pharmacist discovery journey: find an item in the central reviewed catalogue, confirm identity/language availability and open its public QR. It needs no full-description authoring, patient data or PMS connection. The [proposed demonstration](project-roadmap.md#proposed-next-milestone--public-catalogue-to-qr-demonstration) uses synthetic public fixtures and grants no protected workspace capability. The protected J-002 flow above remains subject to its authorization decisions.

## J-003 — Future private instructions (F-006)

A patient-specific reference is presented, a separate verification step is completed, and only then may narrowly authorized private instructions be returned. Before authorization, public information uses its own path and no private fields are fetched into the browser. Failed, expired, revoked, or invalid access receives non-enumerating feedback.

This scenario is future scope. ADR-003 and existing security rules remain mandatory. Do not prototype with real patient data or choose a verification factor without HUM-004. Validate the future boundary through T-006.
