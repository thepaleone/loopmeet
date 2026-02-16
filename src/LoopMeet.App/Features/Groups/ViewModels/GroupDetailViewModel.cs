using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupDetailViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;

    public ObservableCollection<GroupMember> Members { get; } = new();

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private Guid _groupId;

    public GroupDetailViewModel(GroupsApi groupsApi)
    {
        _groupsApi = groupsApi;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (GroupId == Guid.Empty)
        {
            return;
        }

        var group = await _groupsApi.GetGroupAsync(GroupId);
        GroupName = group.Name;
        Members.Clear();
        foreach (var member in group.Members)
        {
            Members.Add(member);
        }
    }
}
