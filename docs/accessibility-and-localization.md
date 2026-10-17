# Accessibility and localization

Status: Core product principles approved; launch languages, conformance target, and support matrix await HUM-002. No compliance claim is made.

## Design baseline (F-003 / REQ-003)

Consider large text, screen readers, keyboard operation, focus visibility, semantic headings, contrast, reflow, understandable errors, and touch interaction from the initial design. Test mobile screens and text expansion rather than relying on a desktop translation pass.

Treat RTL as an initial layout concern. Validate reading order, mixed-direction names/numbers, punctuation, focus sequence, and directional icons with fluent reviewers. Do not assume every icon or data value should be mirrored.

Language selection should be easy to find and understandable in the target language. Do not silently replace unavailable translated safety material with content that appears medically reviewed in that language. Fallback presentation and language availability must be decided through content governance and usability research.

## Content and optional formats

Translations require qualified medical/language review, source-version linkage, and correction handling (F-004). Simplified language must preserve safety meaning. Audio is optional proposed scope (F-005): evaluate user-controlled playback, visible text equivalents, pronunciation review, and privacy before choosing production methods.

## Validation and open questions

T-003 combines manual accessibility checks, assisted technology sessions, fluent RTL review, and participant testing from the [UX blueprint](ui-ux-blueprint.md). Automated checks alone are insufficient.

Decision Required: initial languages, reviewer availability, accessibility standard/version and level, supported browsers/devices/assistive tools, acceptance targets, and fallback behavior (HUM-002, HUM-003). Any legal accessibility obligations require research under REG-003; no standard is asserted here as a verified legal requirement.
