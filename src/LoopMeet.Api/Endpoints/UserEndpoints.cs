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
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var user = await provisioningService.UpsertProfileAsync(userId.Value, request, cancellationToken);
                return Results.Ok(user);
            })
            .RequireAuthorization();

        return app;
    }
}
