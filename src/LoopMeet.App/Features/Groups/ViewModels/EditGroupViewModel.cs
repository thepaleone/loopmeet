using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class EditGroupViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly ILogger<EditGroupViewModel> _logger;

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

    public EditGroupViewModel(GroupsApi groupsApi, ILogger<EditGroupViewModel> logger)
    {
        _groupsApi = groupsApi;
        _logger = logger;
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

        _logger.LogInformation("Editing group {GroupId} ({GroupName})", groupId, name ?? "");
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
            _logger.LogWarning("Edit group attempted without a group id.");
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
            _logger.LogInformation("Saving group {GroupId} with name {GroupName}", GroupId, trimmedName);
            await _groupsApi.UpdateGroupAsync(GroupId, new UpdateGroupRequest { Name = trimmedName });
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(ex, "Edit group forbidden for {GroupId}", GroupId);
            ErrorMessage = "Only the group owner can edit this group.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Duplicate group name for {GroupId}", GroupId);
            ErrorMessage = "You already have a group with that name.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(ex, "Invalid group name for {GroupId}", GroupId);
            ErrorMessage = "Please provide a group name.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update group {GroupId}", GroupId);
            ErrorMessage = "Could not update the group. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
