# API Contract: Places Proxy

**Feature Branch**: `006-group-meetups`
**Date**: 2026-03-30
**Base Path**: `/places`

Server-side proxy for Google Places API (New). Keeps the API key on the server. All endpoints require authentication.

---

## GET /places/autocomplete?query={query}

Returns place predictions based on a text query. Proxies to Google Places Autocomplete (New).

**Authorization**: Authenticated user.

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `query` | `string` | Yes | Search text (min 2 characters) |

**Response** `200 OK`:

```json
{
  "predictions": [
    {
      "placeId": "ChIJN1t_tDeuEmsRUsoyG83frY4",
      "mainText": "Central Park",
      "secondaryText": "New York, NY, USA",
      "description": "Central Park, New York, NY, USA"
    }
  ]
}
```

**Response** `400 Bad Request`: Query too short (< 2 characters).
**Response** `503 Service Unavailable`: Google Places API is unreachable or returned an error.

---

## GET /places/{placeId}

Returns full place details for a selected prediction. Proxies to Google Places Details (New).

**Authorization**: Authenticated user.

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `placeId` | `string` | Google Place ID from autocomplete |

**Response** `200 OK`:

```json
{
  "placeId": "ChIJN1t_tDeuEmsRUsoyG83frY4",
  "name": "Central Park",
  "formattedAddress": "New York, NY 10024, USA",
  "latitude": 40.7829,
  "longitude": -73.9654
}
```

**Response** `404 Not Found`: Place ID not found.
**Response** `503 Service Unavailable`: Google Places API is unreachable.

---

## Refit Interface (Client-Side)

```csharp
public interface IPlacesApi
{
    [Get("/places/autocomplete")]
    Task<PlaceAutocompleteResponse> AutocompleteAsync([Query] string query);

    [Get("/places/{placeId}")]
    Task<PlaceDetail> GetPlaceDetailAsync(string placeId);
}
```

## Server-Side Configuration

The Google Places API key is configured via environment variable `GOOGLE_PLACES_API_KEY` on the API server. It is never sent to or stored on the client.

```csharp
// In Program.cs or appsettings configuration:
builder.Services.Configure<PlacesOptions>(options =>
{
    options.ApiKey = builder.Configuration["GOOGLE_PLACES_API_KEY"]
        ?? throw new InvalidOperationException("GOOGLE_PLACES_API_KEY is required");
});
```
