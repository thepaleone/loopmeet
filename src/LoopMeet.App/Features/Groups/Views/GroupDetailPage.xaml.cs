using LoopMeet.App.Features.Groups.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Groups.Views;

public partial class GroupDetailPage : ContentPage
{
    public GroupDetailPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<GroupDetailViewModel>();
    }
}
