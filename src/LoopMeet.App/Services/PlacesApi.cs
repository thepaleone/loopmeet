using Refit;

namespace LoopMeet.App.Services;

public sealed class PlacePrediction
{
    public string PlaceId { get; set; } = string.Empty;
    public string MainText { get; set; } = string.Empty;
    public string SecondaryText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PlaceAutocompleteResponse
{
    public List<PlacePrediction> Predictions { get; set; } = new();
}

public sealed class PlaceDetail
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public interface IPlacesApi
{
    [Get("/places/autocomplete")]
    Task<PlaceAutocompleteResponse> AutocompleteAsync([Query] string query);

    [Get("/places/{placeId}")]
    Task<PlaceDetail> GetPlaceDetailAsync(string placeId);
}

public sealed class PlacesApi
{
    private readonly IPlacesApi _api;
    public PlacesApi(IPlacesApi api) { _api = api; }
    public Task<PlaceAutocompleteResponse> AutocompleteAsync(string query) => _api.AutocompleteAsync(query);
    public Task<PlaceDetail> GetPlaceDetailAsync(string placeId) => _api.GetPlaceDetailAsync(placeId);
}
