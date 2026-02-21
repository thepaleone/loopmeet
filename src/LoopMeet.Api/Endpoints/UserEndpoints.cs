using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;

namespace LoopMeet.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/profile", async (
                UserProfileRequest request,
                CurrentUserService currentUser,
                UserProvisioningService provisioningService,
                ILogger<UserEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("User profile upsert unauthorized: missing user id");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Upserting user profile for {UserId}", userId);
                var user = await provisioningService.UpsertProfileAsync(userId.Value, request, cancellationToken);
                logger.LogInformation("Upserted user profile for {UserId}", userId);
                return Results.Ok(user);
            })
            .RequireAuthorization();

        return app;
    }

    private sealed class LogMarker
    {
    }
}
