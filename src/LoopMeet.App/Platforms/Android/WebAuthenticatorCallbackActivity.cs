using Android.App;
using Android.Content;
using Microsoft.Maui.Authentication;

namespace LoopMeet.App;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "loopmeet",
    DataHost = "auth-callback")]
public sealed class WebAuthenticatorCallbackActivity : WebAuthenticatorCallbackActivity
{
}
