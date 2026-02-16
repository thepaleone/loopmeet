using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.Views;

namespace LoopMeet.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("create-account", typeof(CreateAccountPage));
		Routing.RegisterRoute("groups", typeof(GroupsListPage));
		Routing.RegisterRoute("group-detail", typeof(GroupDetailPage));
		Routing.RegisterRoute("create-group", typeof(CreateGroupPage));
		Routing.RegisterRoute("edit-group", typeof(EditGroupPage));
		Routing.RegisterRoute("invite-member", typeof(InviteMemberPage));
	}
}
