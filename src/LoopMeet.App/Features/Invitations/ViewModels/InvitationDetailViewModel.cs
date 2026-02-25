using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Invitations.ViewModels;

public sealed partial class InvitationDetailViewModel : ObservableObject
{
    private readonly InvitationsApi _invitationsApi;
    private Guid _invitationId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _ownerDisplay = string.Empty;

    [ObservableProperty]
    private string _senderDisplay = string.Empty;

    [ObservableProperty]
    private string _sentDisplay = string.Empty;

    public InvitationDetailViewModel(InvitationsApi invitationsApi)
    {
        _invitationsApi = invitationsApi;
    }

    public void ApplyInvitation(InvitationSummary invitation)
    {
        _invitationId = invitation.Id;
        GroupName = invitation.GroupName;
        OwnerDisplay = FormatPerson(invitation.OwnerName, invitation.OwnerEmail);
        SenderDisplay = FormatPerson(invitation.SenderName, invitation.SenderEmail);
        SentDisplay = invitation.CreatedAt?.ToLocalTime().ToString("f") ?? "Unknown";
    }

    [RelayCommand]
    private Task CloseAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (IsBusy || _invitationId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        try
        {
            await _invitationsApi.AcceptInvitationAsync(_invitationId);
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            ShowError = true;
            ErrorMessage = "Could not accept the invitation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeclineAsync()
    {
        if (IsBusy || _invitationId == Guid.Empty)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        try
        {
            await _invitationsApi.DeclineInvitationAsync(_invitationId);
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            ShowError = true;
            ErrorMessage = "Could not decline the invitation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatPerson(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.IsNullOrWhiteSpace(email) ? "Unknown" : email;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return name;
        }

        return $"{name} ({email})";
    }
}
