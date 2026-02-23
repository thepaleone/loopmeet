namespace LoopMeet.Api.Contracts;

public sealed class UserProfileResponse
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
}