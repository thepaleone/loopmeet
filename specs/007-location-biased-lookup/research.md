# Research: Location-Biased Lookup

## Decision 1: Reuse existing Create/Edit meetup lookup flows and shared services

- Decision: Extend existing `CreateMeetupViewModel`, `EditMeetupViewModel`, and `PlacesApi` behavior instead of introducing a new lookup feature module.
- Rationale: Existing flows already own query debounce, prediction list state, and selection lifecycle. Reuse reduces risk and maintains consistent UX between create and edit pages.
- Alternatives considered:
  - Build a brand-new location lookup component and migrate both pages to it.
  - Keep current separation and duplicate enhancements in each view model.

## Decision 2: Use platform-native permission request flow

- Decision: Trigger location permission at first location search interaction in meetup forms, following OS-managed prompt behavior.
- Rationale: Satisfies platform rules, minimizes surprise prompts, and preserves user control. Also aligns with feature request to request permission and use location only when granted.
- Alternatives considered:
  - Ask for permission at app launch (rejected: poor UX for unrelated journeys).
  - Never request permission and rely on manual typing only (rejected: misses primary value goal).

## Decision 3: Use Google autocomplete location bias features through existing backend proxy

- Decision: Extend autocomplete request contract to optionally include user coordinates and radius/bias hints when available; keep non-biased request path as fallback.
- Rationale: Uses native Google Places capabilities already available in the current integration path and avoids custom ranking logic in the app.
- Alternatives considered:
  - Custom local post-filtering/reranking in app (rejected: higher complexity, less accurate).
  - Replace provider or add secondary geocoder (rejected: out of scope and unnecessary).

## Decision 4: Keep fallback behavior first-class when permission/location unavailable

- Decision: Always allow query-based suggestions without location bias when permission is denied/revoked or location cannot be obtained.
- Rationale: Required by spec acceptance scenarios and protects completion rate.
- Alternatives considered:
  - Block lookup until permission granted (rejected: violates user control and accessibility).
  - Show no suggestions on location failure (rejected: creates dead-end flow).

## Decision 5: Testing strategy focused on behavior states and contract shaping

- Decision: Add automated tests for permission states, fallback continuity, and request composition for biased vs unbiased autocomplete.
- Rationale: Matches constitution requirement that new behavior and regressions are covered by deterministic tests.
- Alternatives considered:
  - Manual-only QA (rejected: insufficient safety net).
  - Only endpoint tests without app behavior tests (rejected: misses user-flow risks).
