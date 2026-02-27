namespace LoopMeet.Api.Contracts;

public sealed class UserProfileResponse
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? AvatarUrl { get; init; }
    public string AvatarSource { get; init; } = "none";
    public DateTimeOffset UserSince { get; init; }
    public int GroupCount { get; init; }
    public bool CanChangePassword { get; init; } = true;
    public bool HasEmailProvider { get; init; } = true;
    public bool RequiresCurrentPassword { get; init; } = true;
    public bool RequiresEmailForPasswordSetup { get; init; }
}
