using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;
using Refit;

namespace LoopMeet.App.Features.Invitations.ViewModels;

public sealed partial class InviteMemberViewModel : ObservableObject
{
    private readonly InvitationsApi _invitationsApi;

    [ObservableProperty]
    private Guid _groupId;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public InviteMemberViewModel(InvitationsApi invitationsApi)
    {
        _invitationsApi = invitationsApi;
    }

    partial void OnErrorMessageChanged(string value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }

    public void ApplyGroupId(Guid groupId)
    {
        GroupId = groupId;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;
        var trimmedEmail = Email.Trim();
        if (GroupId == Guid.Empty)
        {
            ErrorMessage = "Missing group information.";
            return;
        }

        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            ErrorMessage = "Please provide an email address.";
            return;
        }

        IsBusy = true;
        try
        {
            await _invitationsApi.CreateInvitationAsync(GroupId, new CreateInvitationRequest
            {
                Email = trimmedEmail
            });
            await Shell.Current.GoToAsync("//groups");
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            ErrorMessage = "Only the group owner can invite members.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            ErrorMessage = "That user is already invited or in the group.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            ErrorMessage = "Please provide a valid email address.";
        }
        catch
        {
            ErrorMessage = "Could not send the invitation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
