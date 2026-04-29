# Contract: Location Autocomplete (Meetup Forms)

## Purpose

Define the request/response contract for meetup location autocomplete with optional location biasing, while preserving compatibility for clients that send query-only requests.

## Endpoint

- Method: `GET`
- Route: `/places/autocomplete`
- Auth: Required

## Request

### Required query parameters

- `query`: user-entered search text (minimum 2 characters)

### Optional query parameters

- `latitude`: current user latitude
- `longitude`: current user longitude
- `radiusMeters`: optional bias radius

## Request Rules

- If `latitude`/`longitude` are both supplied and valid, backend applies location bias for provider lookup.
- If optional location params are missing, backend performs standard autocomplete.
- Invalid coordinates are treated as a validation error or ignored per existing endpoint error policy; manual query results must remain available.

## Response

### Success (200)

```json
{
  "predictions": [
    {
      "placeId": "string",
      "mainText": "string",
      "secondaryText": "string",
      "description": "string"
    }
  ]
}
```

### Validation Error (400)

```json
{
  "message": "Query must be at least 2 characters."
}
```

## Compatibility

- Existing clients using only `query` remain fully supported.
- Enhanced clients can progressively add optional location params without breaking previous behavior.

## UX Contract Notes

- Create and edit meetup forms MUST produce equivalent request behavior for the same permission and query state.
- Permission denied/revoked flow MUST continue to call query-only mode.
