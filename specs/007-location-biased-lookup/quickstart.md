# Quickstart: Validate Location-Biased Lookup

## Prerequisites

- Mobile app and API run in development mode.
- Valid Google Places key configured in API settings.
- Test user can access CreateMeetupPage and EditMeetupPage.

## Scenario A: Permission Granted (Biased Lookup)

1. Open create meetup flow.
2. Focus location search and type 3-5 characters of a common place term.
3. Grant location permission when prompted.
4. Verify nearby relevant places appear in top suggestions.
5. Repeat in edit meetup flow and confirm equivalent behavior.

Expected outcome:
- Suggestions are locally relevant and selectable in both flows.

## Scenario B: Permission Denied (Fallback Lookup)

1. Reset or deny app location permission.
2. Open create meetup flow and type a location query.
3. Deny permission when prompted (or keep denied state).
4. Verify suggestions still load without location bias.
5. Repeat in edit meetup flow.

Expected outcome:
- User can still complete location selection with query-only suggestions.

## Scenario C: Granted But Location Unavailable

1. Allow location permission.
2. Disable device location services or simulate no fix.
3. Type a location query in create/edit flow.

Expected outcome:
- Lookup gracefully falls back to non-biased suggestions and does not block form completion.

## Regression Checks

- Selecting a prediction still populates place name, address, coordinates, and place ID.
- Clearing location still resets fields and hides predictions.
- Debounced search behavior remains responsive and avoids duplicate/stale results.

## Automated Test Targets

- App tests: permission state transitions, fallback continuity, create/edit behavior parity.
- API tests: `/places/autocomplete` query-only compatibility and optional location-parameter handling.
