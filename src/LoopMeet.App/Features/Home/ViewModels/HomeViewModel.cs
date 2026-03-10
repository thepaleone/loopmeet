using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Home.ViewModels;

public sealed partial class HomeViewModel(UserProfileCache userProfileCache) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty]
    private string _headline = "Home is coming soon";

    [ObservableProperty]
    private string _supportingText = "This tab is a placeholder for a future LoopMeet feature. Use Groups or Invitations to continue.";

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private bool _hasAvatar;

    [RelayCommand]
    private Task LoadAsync()
    {
        var cached = userProfileCache.GetCachedProfile();
        Title = cached is not null && !string.IsNullOrWhiteSpace(cached.DisplayName)
            ? $"Hello {cached.DisplayName}"
            : "Hello";
        AvatarUrl = cached?.AvatarUrl;
        HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl);
        return Task.CompletedTask;
    }
}
