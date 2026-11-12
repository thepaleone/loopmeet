using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;

namespace LoopMeet.Api.Services.Accounts;

/// <summary>
/// Confines a caller whose account is pending deletion (or purging) to the
/// /users/account-deletion routes. Every other authenticated request gets 403
/// account_pending_deletion, which clients must not treat as a sign-out.
/// </summary>
public sealed class AccountDeletionGateMiddleware
{
    private static readonly PathString DeletionRoutes = "/users/account-deletion";

    private readonly RequestDelegate _next;
    private readonly ILogger<AccountDeletionGateMiddleware> _logger;

    public AccountDeletionGateMiddleware(RequestDelegate next, ILogger<AccountDeletionGateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || context.Request.Path.StartsWithSegments(DeletionRoutes))
        {
            await _next(context);
            return;
        }

        var userId = context.RequestServices.GetRequiredService<CurrentUserService>().UserId;
        if (userId is null)
        {
            await _next(context);
            return;
        }

        // Resolved here rather than injected into InvokeAsync: the scoped Supabase client behind the
        // repository requires a bearer token, which unauthenticated requests don't have.
        var deletionService = context.RequestServices.GetRequiredService<AccountDeletionService>();
        var deletion = await deletionService.GetCachedAsync(userId.Value, context.RequestAborted);
        if (deletion is null)
        {
            await _next(context);
            return;
        }

        _logger.LogDebug("AccountDeletionGateBlocked for {UserId} on {Path}", userId.Value, context.Request.Path.Value);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new AccountPendingDeletionResponse { DeletesAt = deletion.DeletesAt });
    }
}
