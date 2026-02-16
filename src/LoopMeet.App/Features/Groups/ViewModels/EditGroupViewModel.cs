using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Services;
using Refit;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class EditGroupViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;

    [ObservableProperty]
    private Guid _groupId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public EditGroupViewModel(GroupsApi groupsApi)
    {
        _groupsApi = groupsApi;
    }

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    public void ApplyGroup(Guid groupId, string? name)
    {
        GroupId = groupId;
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }
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
        if (GroupId == Guid.Empty)
        {
            ErrorMessage = "Missing group information.";
            return;
        }

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ErrorMessage = "Please provide a group name.";
            return;
        }

        IsBusy = true;
        try
        {
            await _groupsApi.UpdateGroupAsync(GroupId, new UpdateGroupRequest { Name = trimmedName });
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            ErrorMessage = "Only the group owner can edit this group.";
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
            ErrorMessage = "Could not update the group. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
