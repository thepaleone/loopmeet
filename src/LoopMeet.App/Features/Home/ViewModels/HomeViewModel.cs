using CommunityToolkit.Mvvm.ComponentModel;

namespace LoopMeet.App.Features.Home.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Home";

    [ObservableProperty]
    private string _headline = "Home is coming soon";

    [ObservableProperty]
    private string _supportingText = "This tab is a placeholder for a future LoopMeet feature. Use Groups or Invitations to continue.";
}
