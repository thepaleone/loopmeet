# Data Model: Location-Biased Lookup

## Entity: LocationQuerySession

- Purpose: Represents one active location-search interaction in create/edit meetup flows.
- Fields:
  - `QueryText` (string, required, min length 2 for lookup)
  - `PermissionState` (enum: NotDetermined, Granted, DeniedOrRestricted, Revoked)
  - `BiasMode` (enum: Disabled, Enabled)
  - `SearchContext` (enum: CreateMeetup, EditMeetup)
  - `StartedAt` (datetime)
- Validation:
  - Query shorter than threshold does not trigger remote lookup.
  - BiasMode can be Enabled only when PermissionState is Granted and current coordinates are available.

## Entity: DeviceLocationContext

- Purpose: Captures current device location used to bias suggestion lookup.
- Fields:
  - `Latitude` (decimal, optional)
  - `Longitude` (decimal, optional)
  - `AccuracyMeters` (number, optional)
  - `CapturedAt` (datetime, optional)
- Validation:
  - Latitude range: -90 to 90.
  - Longitude range: -180 to 180.
  - Context is considered usable only when both latitude and longitude are present.

## Entity: AutocompleteRequestContext

- Purpose: Defines contract payload for places autocomplete request.
- Fields:
  - `Query` (string, required)
  - `LocationBias` (object, optional)
    - `Latitude` (decimal)
    - `Longitude` (decimal)
    - `RadiusMeters` (integer, optional)
  - `IsLocationBiasApplied` (boolean)
- Validation:
  - Query required and must meet minimum length.
  - LocationBias omitted when permission/location unavailable.

## Entity: SuggestedPlace

- Purpose: Candidate place returned to the user for selection.
- Fields:
  - `PlaceId` (string, required)
  - `MainText` (string, required)
  - `SecondaryText` (string, optional)
  - `Description` (string, optional)
  - `IsNearbyBiasedResult` (boolean)
- Validation:
  - PlaceId and MainText required for display and detail lookup.

## Entity: PermissionState

- Purpose: Tracks user location permission state used by lookup behavior.
- States:
  - `NotDetermined` -> `Granted` | `DeniedOrRestricted`
  - `Granted` -> `Revoked`
  - `DeniedOrRestricted` -> `Granted` (if user later changes in OS settings)
  - `Revoked` -> `Granted` (if user re-enables)

## Relationship Summary

- One `LocationQuerySession` may use zero or one `DeviceLocationContext`.
- One `LocationQuerySession` produces one `AutocompleteRequestContext` per query update.
- Each `AutocompleteRequestContext` returns zero to many `SuggestedPlace` records.
