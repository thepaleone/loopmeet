namespace LoopMeet.Api.Contracts;

public sealed class PasswordChangeRequest
{
    public string? Email { get; init; }
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}
