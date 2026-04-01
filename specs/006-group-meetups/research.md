# Research: Group Meetups

**Feature Branch**: `006-group-meetups`
**Date**: 2026-03-30

## R1: Google Places API Integration Approach

**Decision**: Use the Google Places API (New) — specifically the Autocomplete (New) endpoint — accessed via a server-side proxy in the ASP.NET Core API. The MAUI client calls the LoopMeet API, which proxies to Google.

**Rationale**:
- The API key must not be embedded in the mobile client (security requirement, easily extracted from APKs/bundles).
- The server-side proxy pattern is consistent with the existing architecture where all external calls flow through the LoopMeet API.
- Google Places API (New) is the current-generation API, replacing the legacy Places SDK. It uses simple REST endpoints with JSON responses.
- The Autocomplete (New) endpoint (`POST https://places.googleapis.com/v1/places:autocomplete`) accepts a text query and returns place predictions with place IDs.
- Place Details (New) (`GET https://places.googleapis.com/v1/places/{placeId}`) resolves a place ID to full details (name, formatted address, coordinates).

**Alternatives Considered**:
- *Client-side Google Places SDK for MAUI*: No official .NET MAUI SDK exists. Community wrappers are unmaintained. Rejected: fragile dependency, API key exposure.
- *MapBox Places API*: Viable alternative but adds a new vendor relationship. Rejected: unnecessary when Google Places is the user's stated preference.
- *OpenStreetMap Nominatim*: Free but has strict usage limits (1 req/sec), no autocomplete, and lower quality place data. Rejected: poor UX for autocomplete.

## R2: Storing and Opening Locations

**Decision**: Store location as discrete fields: `place_name` (varchar), `place_address` (varchar), `latitude` (double precision), `longitude` (double precision), and `place_id` (varchar, the Google Place ID for potential future use). Use `Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync()` to open locations in the device's default maps app.

**Rationale**:
- Discrete fields are simpler to query, filter, and display than a JSON blob.
- `Map.Default.OpenAsync(latitude, longitude, options)` is a built-in MAUI API that works across iOS (Apple Maps), Android (Google Maps), macOS, and Windows — no additional packages needed.
- Storing the Google Place ID allows future features like fetching updated details, photos, or reviews.
- `place_name` and `place_address` are stored separately for flexible display (e.g., show just the name in a compact card, full address in detail view).

**Alternatives Considered**:
- *JSON column for location*: Flexible but harder to query and validate at the DB level. Rejected: discrete columns are simpler and sufficient.
- *PostGIS geography type*: Overkill for storing point locations with no spatial queries planned. Rejected: unnecessary complexity.
- *Platform-specific map intents*: Would require conditional compilation. Rejected: MAUI's `Map.Default.OpenAsync()` handles this natively.

## R3: Dual FAB Layout on Group Detail Page

**Decision**: Place two FABs stacked vertically at the bottom-right of the group detail page. The invite FAB stays at its current position (bottom, Margin="0,0,16,32"). The new add-meetup FAB is positioned above it (Margin="0,0,16,100"). Both are visible only to the group owner. The add-meetup FAB uses a calendar/plus icon to distinguish it from the invite icon.

**Rationale**:
- Stacked FABs is a well-established Material Design pattern for pages with two primary actions.
- Keeps both actions one tap away without introducing a speed-dial or bottom bar (which would require more complex state management).
- Consistent with the existing FAB styling (56x56, CornerRadius=28, Primary/PrimaryDark color).
- No additional layout containers needed — both FABs are direct children of the page Grid with absolute positioning via VerticalOptions/HorizontalOptions/Margin.

**Alternatives Considered**:
- *Speed-dial FAB*: Single FAB that expands to show options. More complex, requires animation state, and the CommunityToolkit.Maui doesn't provide one out of the box. Rejected: over-engineered for two actions.
- *Bottom action bar*: Would change the page layout significantly and conflict with the tab bar. Rejected: inconsistent with existing UI.
- *Replace single FAB with contextual menu*: Hides the invite action behind an extra tap. Rejected: regresses existing UX.

## R4: Server-Side Caching for Meetups

**Decision**: Follow the existing 30-second in-memory cache pattern used by `GroupQueryService`. Cache meetup lists per group (`meetups:{groupId}`) and per user for the home page (`home-meetups:{userId}`). Invalidate on create/update/delete.

**Rationale**:
- Consistent with existing caching in `GroupQueryService` (30s TTL, key-based invalidation).
- Low meetup volume means cache miss cost is minimal.
- The home page aggregation query (meetups across all groups) benefits from caching to avoid repeated joins.

**Alternatives Considered**:
- *No caching*: Acceptable given low volume, but inconsistent with existing patterns and loses the benefit for repeated page visits. Rejected: inconsistency.
- *Longer TTL*: Would delay visibility of new meetups to other members. Rejected: 30s is the established balance.

## R5: Date/Time Storage and Time Zone Handling

**Decision**: Store meetup date/time as `scheduled_at` (timestamptz) in UTC. The client converts from local time to UTC on save and from UTC to local time on display. No separate time zone column.

**Rationale**:
- `timestamptz` is already the convention for all timestamps in the schema (created_at, updated_at, accepted_at).
- Supabase/PostgreSQL stores timestamptz in UTC internally.
- The MAUI client's `DateTimeOffset` naturally handles UTC↔local conversion.
- For a social meetup app where members are expected to be in the same geographic area, UTC storage with local display is sufficient.

**Alternatives Considered**:
- *Storing with explicit IANA time zone*: Useful for events spanning time zones (e.g., virtual meetings). Rejected: out of scope for in-person group meetups.
- *Storing as naive datetime (timestamp without timezone)*: Ambiguous and error-prone. Rejected: timestamptz is the established convention.

## R6: Meetup Filtering (Past vs. Upcoming)

**Decision**: Filter on the server side. The API endpoints for group detail and home page return only meetups where `scheduled_at > now()`. The "upcoming" check uses the database server's current time (UTC), not the client's time.

**Rationale**:
- Server-side filtering reduces payload size and avoids clock-skew issues between client and server.
- Consistent with how the API already shapes responses (e.g., `GroupsResponse` separates owned vs. member groups server-side).
- Simple SQL `WHERE scheduled_at > now()` clause.

**Alternatives Considered**:
- *Client-side filtering*: Simpler API but sends unnecessary data and trusts client clocks. Rejected: wasteful and unreliable.
- *Configurable time window (e.g., include meetups from last 24h)*: Adds complexity without a clear user need. Rejected: spec says exclude past meetups.
