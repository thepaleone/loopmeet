namespace LoopMeet.App.Features.Profile.Models;

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

public sealed class UserProfileUpdateRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarOverrideUrl { get; set; }
    public string? SocialAvatarUrl { get; set; }
}

public sealed class PasswordChangeRequest
{
    public string? Email { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
