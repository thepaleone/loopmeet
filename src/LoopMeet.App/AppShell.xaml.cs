using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.Views;
using LoopMeet.App.Features.Meetups.Views;
using LoopMeet.App.Features.Profile.Views;

namespace LoopMeet.App;

public partial class AppShell : Shell
{
#if DEBUG || STAGING
	public bool ShowDevTools => true;
#else
	public bool ShowDevTools => false;
#endif

	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("create-account", typeof(CreateAccountPage));
		Routing.RegisterRoute("group-detail", typeof(GroupDetailPage));
		Routing.RegisterRoute("create-group", typeof(CreateGroupPage));
		Routing.RegisterRoute("edit-group", typeof(EditGroupPage));
		Routing.RegisterRoute("invite-member", typeof(InviteMemberPage));
		Routing.RegisterRoute("invitation-detail", typeof(InvitationDetailPage));
		Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));
		Routing.RegisterRoute("create-meetup", typeof(CreateMeetupPage));
		Routing.RegisterRoute("edit-meetup", typeof(EditMeetupPage));
		Routing.RegisterRoute("meetup-detail", typeof(MeetupDetailPage));

		this.DevToolsTab.IsVisible = ShowDevTools;
	}
}
