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
}

public sealed class AuthSession
{
    public string AccessToken { get; set; } = string.Empty;
}
