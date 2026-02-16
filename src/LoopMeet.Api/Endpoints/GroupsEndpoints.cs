using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Groups;
using LoopMeet.Api.Services.Invitations;

namespace LoopMeet.Api.Endpoints;

public static class GroupsEndpoints
{
    public static IEndpointRouteBuilder MapGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/groups", async (
                CurrentUserService currentUser,
                GroupQueryService groupQueryService,
                InvitationQueryService invitationQueryService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var response = await groupQueryService.GetGroupsAsync(userId.Value, cancellationToken);
                var email = currentUser.Email;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    response.PendingInvitations = await invitationQueryService.ListPendingAsync(email, cancellationToken);
                }
                return Results.Ok(response);
            })
            .RequireAuthorization();

        app.MapGet("/groups/{groupId:guid}", async (
                Guid groupId,
                GroupQueryService groupQueryService,
                CancellationToken cancellationToken) =>
            {
                var response = await groupQueryService.GetGroupDetailAsync(groupId, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .RequireAuthorization();

        return app;
    }
}
