using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Services;
using Refit;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class CreateGroupViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public CreateGroupViewModel(GroupsApi groupsApi)
    {
        _groupsApi = groupsApi;
    }

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;
        var trimmedName = Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ErrorMessage = "Please provide a group name.";
            return;
        }

        IsBusy = true;
        try
        {
            await _groupsApi.CreateGroupAsync(new CreateGroupRequest { Name = trimmedName });
            await Shell.Current.GoToAsync("groups");
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            ErrorMessage = "You already have a group with that name.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ErrorMessage = "Please provide a group name.";
        }
        catch
        {
            ErrorMessage = "Could not create the group. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
