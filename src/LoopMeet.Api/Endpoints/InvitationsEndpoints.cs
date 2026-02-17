using LoopMeet.Api.Contracts;
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

        app.MapPost("/groups/{groupId:guid}/invitations", async (
                Guid groupId,
                CreateInvitationRequest request,
                CurrentUserService currentUser,
                InvitationCommandService invitationCommandService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await invitationCommandService.CreateAsync(userId.Value, groupId, request.Email, cancellationToken);
                return result.Status switch
                {
                    InvitationCommandStatus.Success => Results.Created($"/invitations/{result.Invitation!.Id}", result.Invitation),
                    InvitationCommandStatus.NotFound => Results.NotFound(),
                    InvitationCommandStatus.Forbidden => Results.Json(new ErrorResponse
                    {
                        Code = "not_group_owner",
                        Message = "Only the group owner can invite members."
                    }, statusCode: StatusCodes.Status403Forbidden),
                    InvitationCommandStatus.AlreadyMember => Results.Json(new ErrorResponse
                    {
                        Code = "already_member",
                        Message = "That user is already in the group."
                    }, statusCode: StatusCodes.Status409Conflict),
                    InvitationCommandStatus.Duplicate => Results.Json(new ErrorResponse
                    {
                        Code = "invitation_exists",
                        Message = "An invitation is already pending for that email."
                    }, statusCode: StatusCodes.Status409Conflict),
                    InvitationCommandStatus.InvalidEmail => Results.Json(new ErrorResponse
                    {
                        Code = "invalid_email",
                        Message = "Please provide a valid email address."
                    }, statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        app.MapPost("/invitations/{invitationId:guid}/accept", async (
                Guid invitationId,
                CurrentUserService currentUser,
                InvitationCommandService invitationCommandService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                var email = currentUser.Email;
                if (userId is null || string.IsNullOrWhiteSpace(email))
                {
                    return Results.Unauthorized();
                }

                var result = await invitationCommandService.AcceptAsync(userId.Value, email, invitationId, cancellationToken);
                return result.Status switch
                {
                    InvitationCommandStatus.Success => Results.Ok(result.Invitation),
                    InvitationCommandStatus.NotFound => Results.NotFound(),
                    InvitationCommandStatus.AlreadyMember => Results.Json(new ErrorResponse
                    {
                        Code = "already_member",
                        Message = "You are already in this group."
                    }, statusCode: StatusCodes.Status409Conflict),
                    _ => Results.BadRequest()
                };
            })
            .RequireAuthorization();

        return app;
    }
}
