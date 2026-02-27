using System.Security.Claims;

namespace LoopMeet.Api.Services.Auth;

public sealed class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                ?? user?.FindFirst("email")?.Value;
        }
    }

    public string? AccessToken
    {
        get
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return null;
            }

            return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : null;
        }
    }
}
