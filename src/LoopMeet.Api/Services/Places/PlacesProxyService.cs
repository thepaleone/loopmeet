using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LoopMeet.Api.Contracts;
using Microsoft.Extensions.Options;

namespace LoopMeet.Api.Services.Places;

public sealed class PlacesProxyService
{
    private readonly HttpClient _httpClient;
    private readonly PlacesOptions _options;
    private readonly ILogger<PlacesProxyService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public PlacesProxyService(HttpClient httpClient, IOptions<PlacesOptions> options, ILogger<PlacesProxyService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PlaceAutocompleteResponse> AutocompleteAsync(
        string query,
        double? latitude = null,
        double? longitude = null,
        int? radiusMeters = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Places autocomplete query={Query} hasBias={HasBias}", query, latitude.HasValue && longitude.HasValue);
        object requestPayload;
        if (latitude.HasValue && longitude.HasValue && IsValidCoordinates(latitude.Value, longitude.Value))
        {
            requestPayload = new
            {
                input = query,
                locationBias = new
                {
                    circle = new
                    {
                        center = new { latitude, longitude },
                        radius = radiusMeters.GetValueOrDefault(50_000)
                    }
                }
            };
        }
        else
        {
            requestPayload = new { input = query };
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:autocomplete")
        {
            Content = JsonContent.Create(requestPayload, options: JsonOptions)
        };
        request.Headers.Add("X-Goog-Api-Key", _options.ApiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Places autocomplete failed status={StatusCode}", response.StatusCode);
                return new PlaceAutocompleteResponse();
            }

            var json = await response.Content.ReadFromJsonAsync<GoogleAutocompleteResponse>(JsonOptions, cancellationToken);
            var predictions = json?.Suggestions?
                .Where(s => s.PlacePrediction is not null)
                .Select(s => new PlacePrediction
                {
                    PlaceId = s.PlacePrediction!.PlaceId ?? string.Empty,
                    MainText = s.PlacePrediction.Text?.Text ?? string.Empty,
                    SecondaryText = s.PlacePrediction.SecondaryText?.Text ?? string.Empty,
                    Description = s.PlacePrediction.Text?.Text ?? string.Empty
                })
                .ToList() ?? [];
            _logger.LogInformation("Places autocomplete results={Count}", predictions.Count);
            return new PlaceAutocompleteResponse { Predictions = predictions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Places autocomplete error");
            return new PlaceAutocompleteResponse();
        }
    }

    private static bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    public async Task<PlaceDetailResponse?> GetPlaceDetailAsync(string placeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Places detail placeId={PlaceId}", placeId);
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://places.googleapis.com/v1/places/{placeId}?fields=id,displayName,formattedAddress,location");
        request.Headers.Add("X-Goog-Api-Key", _options.ApiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Places detail failed placeId={PlaceId} status={StatusCode}", placeId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<GooglePlaceDetail>(JsonOptions, cancellationToken);
            if (json is null) return null;
            return new PlaceDetailResponse
            {
                PlaceId = placeId,
                Name = json.DisplayName?.Text ?? string.Empty,
                FormattedAddress = json.FormattedAddress ?? string.Empty,
                Latitude = json.Location?.Latitude ?? 0,
                Longitude = json.Location?.Longitude ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Places detail error placeId={PlaceId}", placeId);
            return null;
        }
    }

    // Google API internal response models
    private sealed class GoogleAutocompleteResponse { public List<GoogleSuggestion>? Suggestions { get; set; } }
    private sealed class GoogleSuggestion { public GooglePlacePrediction? PlacePrediction { get; set; } }
    private sealed class GooglePlacePrediction { public string? PlaceId { get; set; } public GoogleTextValue? Text { get; set; } public GoogleTextValue? SecondaryText { get; set; } }
    private sealed class GoogleTextValue { public string? Text { get; set; } }
    private sealed class GooglePlaceDetail { public GoogleTextValue? DisplayName { get; set; } public string? FormattedAddress { get; set; } public GoogleLatLng? Location { get; set; } }
    private sealed class GoogleLatLng { public double Latitude { get; set; } public double Longitude { get; set; } }
}
