using LoopMeet.App.Features.Meetups.ViewModels;

namespace LoopMeet.App.Features.Meetups.Views;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(MeetupId), "meetupId")]
public partial class EditMeetupPage : ContentPage
{
    private Guid _groupId;
    private Guid _meetupId;

    public Guid GroupId
    {
        get => _groupId;
        set
        {
            _groupId = value;
            TryApplyParameters();
        }
    }

    public Guid MeetupId
    {
        get => _meetupId;
        set
        {
            _meetupId = value;
            TryApplyParameters();
        }
    }

    public EditMeetupPage(EditMeetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void TryApplyParameters()
    {
        if (_groupId != Guid.Empty && _meetupId != Guid.Empty && BindingContext is EditMeetupViewModel vm)
        {
            vm.ApplyParameters(_groupId, _meetupId);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EditMeetupViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
