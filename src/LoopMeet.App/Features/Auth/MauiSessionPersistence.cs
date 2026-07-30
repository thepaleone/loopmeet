using System.Text.Json;
using Microsoft.Maui.Storage;
using Supabase.Gotrue.Interfaces;
// The Features.Auth.Session child namespace shadows the Gotrue type name here.
using GotrueSession = Supabase.Gotrue.Session;

namespace LoopMeet.App.Features.Auth;

public sealed class MauiSessionPersistence : IGotrueSessionPersistence<GotrueSession>
{
    private const string SessionKey = "loopmeet.auth.session";

    public void SaveSession(GotrueSession session)
    {
        var json = JsonSerializer.Serialize(session);
        Preferences.Default.Set(SessionKey, json);
    }

    public void DestroySession()
    {
        Preferences.Default.Remove(SessionKey);
    }

    public GotrueSession? LoadSession()
    {
        var json = Preferences.Default.Get(SessionKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GotrueSession>(json);
        }
        catch
        {
            return null;
        }
    }
}
