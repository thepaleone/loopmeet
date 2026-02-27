using LoopMeet.Core.Models;

namespace LoopMeet.Api.Services.Auth;

public sealed class ProfileAvatarResolver
{
    public const string NoneSource = "none";
    public const string SocialSource = "social";
    public const string UserOverrideSource = "user_override";

    public void ApplyFromRequest(User user, string? socialAvatarUrl, string? avatarOverrideUrl)
    {
        if (!string.IsNullOrWhiteSpace(avatarOverrideUrl))
        {
            user.AvatarOverrideUrl = avatarOverrideUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(socialAvatarUrl) && string.IsNullOrWhiteSpace(user.AvatarOverrideUrl))
        {
            user.SocialAvatarUrl = socialAvatarUrl.Trim();
        }
    }

    public string ResolveEffectiveAvatarUrl(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.AvatarOverrideUrl))
        {
            return user.AvatarOverrideUrl;
        }

        return user.SocialAvatarUrl ?? string.Empty;
    }

    public string ResolveSource(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.AvatarOverrideUrl))
        {
            return UserOverrideSource;
        }

        if (!string.IsNullOrWhiteSpace(user.SocialAvatarUrl))
        {
            return SocialSource;
        }

        return NoneSource;
    }
}
