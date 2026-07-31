using LoopMeet.App.Features.Meetups.ViewModels;

namespace LoopMeet.App.Features.Meetups.Views;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(MeetupId), "meetupId")]
public partial class MeetupDetailPage : ContentPage
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

    public MeetupDetailPage(MeetupDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void TryApplyParameters()
    {
        if (_groupId != Guid.Empty && _meetupId != Guid.Empty && BindingContext is MeetupDetailViewModel vm)
        {
            vm.ApplyParameters(_groupId, _meetupId);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Re-read on every arrival so returning from an edit shows the new values.
        if (BindingContext is MeetupDetailViewModel vm)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
