using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Invitations;

namespace LoopMeet.Api.Endpoints;

public static class InvitationsEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/invitations", async (
                CurrentUserService currentUser,
                InvitationQueryService invitationQueryService,
                CancellationToken cancellationToken) =>
            {
                var email = currentUser.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.Unauthorized();
                }

                var invitations = await invitationQueryService.ListPendingAsync(email, cancellationToken);
                return Results.Ok(new Contracts.InvitationsResponse { Invitations = invitations });
            })
            .RequireAuthorization();

        return app;
    }
}
