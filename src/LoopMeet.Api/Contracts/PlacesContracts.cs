namespace LoopMeet.Api.Contracts;

public sealed class PlacePrediction
{
    public string PlaceId { get; init; } = string.Empty;
    public string MainText { get; init; } = string.Empty;
    public string SecondaryText { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class PlaceAutocompleteResponse
{
    public IReadOnlyList<PlacePrediction> Predictions { get; init; } = Array.Empty<PlacePrediction>();
}

public sealed class PlaceDetailResponse
{
    public string PlaceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FormattedAddress { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public sealed class PlacesOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
