namespace LoopMeet.App.Features.Auth.Models;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateAccountRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class UserProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? SocialAvatarUrl { get; set; }
    public string? AvatarOverrideUrl { get; set; }
}

public sealed class UserProfileResponse
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string AvatarSource { get; set; } = "none";
    public DateTimeOffset UserSince { get; set; }
    public int GroupCount { get; set; }
    public bool CanChangePassword { get; set; } = true;
    public bool HasEmailProvider { get; set; } = true;
    public bool RequiresCurrentPassword { get; set; } = true;
    public bool RequiresEmailForPasswordSetup { get; set; }
}

public sealed class AuthSession
{
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class OAuthSignInResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }
}
