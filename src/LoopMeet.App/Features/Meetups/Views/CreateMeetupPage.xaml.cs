using LoopMeet.App.Features.Meetups.ViewModels;

namespace LoopMeet.App.Features.Meetups.Views;

[QueryProperty(nameof(GroupId), "groupId")]
public partial class CreateMeetupPage : ContentPage
{
    private Guid _groupId;
    public Guid GroupId
    {
        get => _groupId;
        set
        {
            _groupId = value;
            if (BindingContext is CreateMeetupViewModel vm)
            {
                vm.ApplyGroupId(value);
            }
        }
    }

    public CreateMeetupPage(CreateMeetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
