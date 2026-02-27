using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;

namespace LoopMeet.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/profile", async (
                CurrentUserService currentUser,
                UserProvisioningService provisioningService,
                UserProfileProjectionService projectionService,
                ILogger<UserEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("User profile fetch unauthorized: missing user id");
                    return Results.Unauthorized();
                }

                var user = await provisioningService.GetProfileAsync(userId.Value, cancellationToken);
                if (user is null)
                {
                    logger.LogInformation("User profile not found for {UserId}", userId);
                    return Results.NotFound();
                }

                return Results.Ok(await projectionService.BuildAsync(user, cancellationToken));
            })
            .RequireAuthorization();

        app.MapPost("/users/profile", async (
                UserProfileRequest request,
                CurrentUserService currentUser,
                UserProvisioningService provisioningService,
                UserProfileProjectionService projectionService,
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
                return Results.Ok(await projectionService.BuildAsync(user, cancellationToken));
            })
            .RequireAuthorization();

        app.MapPatch("/users/profile", async (
                UserProfileUpdateRequest request,
                CurrentUserService currentUser,
                UserProvisioningService provisioningService,
                UserProfileProjectionService projectionService,
                ILogger<UserEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("User profile patch unauthorized: missing user id");
                    return Results.Unauthorized();
                }

                logger.LogInformation("Updating user profile for {UserId}", userId);
                var user = await provisioningService.UpdateProfileAsync(userId.Value, request, cancellationToken);
                if (user is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(await projectionService.BuildAsync(user, cancellationToken));
            })
            .RequireAuthorization();

        app.MapPost("/users/password", async (
                PasswordChangeRequest request,
                CurrentUserService currentUser,
                UserProvisioningService provisioningService,
                ILogger<UserEndpoints.LogMarker> logger,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    logger.LogWarning("User password change unauthorized: missing user id");
                    return Results.Unauthorized();
                }

                var result = await provisioningService.ChangePasswordAsync(
                    userId.Value,
                    currentUser.Email,
                    currentUser.AccessToken,
                    request,
                    cancellationToken);
                if (result.IsSuccess)
                {
                    logger.LogInformation("Password changed for {UserId}", userId);
                    return Results.NoContent();
                }

                if (result.IsCurrentPasswordInvalid)
                {
                    logger.LogWarning(
                        "Password change rejected for {UserId}. Reason: {ReasonCode}",
                        userId,
                        result.ReasonCode);
                    return Results.Json(new ErrorResponse
                    {
                        Code = "invalid_current_password",
                        Message = result.Error ?? "Current password is incorrect."
                    }, statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                logger.LogWarning(
                    "Password change failed for {UserId}. Reason: {ReasonCode}. HasEmailFromRequest: {HasEmailFromRequest}. HasEmailFromProfile: {HasEmailFromProfile}. HasEmailFromClaim: {HasEmailFromClaim}. HasEmailFromAuthUser: {HasEmailFromAuthUser}. HasEmailIdentity: {HasEmailIdentity}",
                    userId,
                    result.ReasonCode,
                    result.HasEmailFromRequest,
                    result.HasEmailFromProfile,
                    result.HasEmailFromClaim,
                    result.HasEmailFromAuthUser,
                    result.HasEmailIdentity);

                var code = result.ReasonCode switch
                {
                    PasswordChangeFailureReason.MissingFields => "password_fields_required",
                    PasswordChangeFailureReason.PasswordMismatch => "password_mismatch",
                    PasswordChangeFailureReason.PasswordPolicyFailed => "password_policy_failed",
                    PasswordChangeFailureReason.MissingEmail => "missing_account_email",
                    PasswordChangeFailureReason.IdentityLookupFailed => "identity_lookup_failed",
                    PasswordChangeFailureReason.SupabaseUpdateFailed => "password_update_failed",
                    PasswordChangeFailureReason.SupabaseUnexpectedError => "password_change_failed",
                    PasswordChangeFailureReason.ServiceNotConfigured => "password_service_unavailable",
                    _ => "invalid_password_change"
                };

                return Results.Json(new ErrorResponse
                {
                    Code = code,
                    Message = result.Error ?? "Unable to change password."
                }, statusCode: StatusCodes.Status400BadRequest);
            })
            .RequireAuthorization();

        return app;
    }

    private sealed class LogMarker
    {
    }
}
