using LoopMeet.App.Features.Safety.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Safety.Views;

public partial class ReportConcernPage : ContentPage, IQueryAttributable
{
    public ReportConcernPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<ReportConcernViewModel>();
    }

    /// <summary>Group Detail's member actions pass the group and person to preselect.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is ReportConcernViewModel viewModel)
        {
            viewModel.Preselect(ReadGuid(query, "groupId"), ReadGuid(query, "userId"));
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportConcernViewModel { IsLoaded: false } viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    private static Guid? ReadGuid(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) && Guid.TryParse(value?.ToString(), out var id) ? id : null;
}
