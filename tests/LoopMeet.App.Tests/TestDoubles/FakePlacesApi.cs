namespace LoopMeet.App.Tests.TestDoubles;

public sealed class FakePlacesApi
{
    public string LastQuery { get; private set; } = string.Empty;
    public double? LastLatitude { get; private set; }
    public double? LastLongitude { get; private set; }
    public int? LastRadiusMeters { get; private set; }

    public Task AutocompleteAsync(string query, double? latitude = null, double? longitude = null, int? radiusMeters = null)
    {
        LastQuery = query;
        LastLatitude = latitude;
        LastLongitude = longitude;
        LastRadiusMeters = radiusMeters;

        return Task.CompletedTask;
    }
}
