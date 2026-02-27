namespace LoopMeet.Api.Contracts;

public sealed class UserProfileRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Password { get; init; }
    public string? SocialAvatarUrl { get; init; }
    public string? AvatarOverrideUrl { get; init; }
}

public sealed class UserProfileUpdateRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? SocialAvatarUrl { get; init; }
    public string? AvatarOverrideUrl { get; init; }
}
