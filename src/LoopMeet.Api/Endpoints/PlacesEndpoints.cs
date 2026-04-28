using LoopMeet.Api.Services.Places;

namespace LoopMeet.Api.Endpoints;

public static class PlacesEndpoints
{
    public static IEndpointRouteBuilder MapPlacesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/places/autocomplete", async (
                string query,
                double? latitude,
                double? longitude,
                int? radiusMeters,
                PlacesProxyService placesService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return Results.BadRequest(new { message = "Query must be at least 2 characters." });
                var result = await placesService.AutocompleteAsync(query, latitude, longitude, radiusMeters, cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization();

        app.MapGet("/places/{placeId}", async (
                string placeId,
                PlacesProxyService placesService,
                CancellationToken cancellationToken) =>
            {
                var result = await placesService.GetPlaceDetailAsync(placeId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization();

        return app;
    }
}
