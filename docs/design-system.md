# Design system direction

Status: Proposed design conventions supporting approved simplicity, mobile-first, accessibility, multilingual and RTL principles. No component library or visual specification is approved or implemented.

## Initial conventions

- Use a restrained palette, generous readable spacing, clear headings, and familiar controls. Verify contrast rather than assume a calm palette is accessible.
- Use flexible text and layouts that tolerate enlargement and translation expansion. Exact typefaces, sizes, colors, and spacing remain design decisions.
- Use logical layout directions and validate mixed RTL/LTR content. Mirror directional controls only when their meaning requires it.
- Give controls visible labels, accessible names, focus indication, and understandable states. Do not convey warnings through color or icons alone.
- Keep primary tasks visible; use clearly labelled disclosure controls for secondary details. Preserve essential safety information in the primary hierarchy.

## Candidate component inventory

Public content heading, language selector, safety notice, detail disclosure, source/revision block, loading/unavailable state, form field, error summary, and staff task status are candidates derived from J-001/J-002. Private verification components remain future scope under J-003.

Each approved component should document semantics, keyboard/focus behavior, large-text behavior, mobile layout, RTL examples, translated labels, and error states. Validate with T-002/T-003 before treating it as reusable. Avoid adopting a UI dependency before a concrete need and approval for implementation.
