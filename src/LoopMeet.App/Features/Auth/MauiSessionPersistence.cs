using System.Text.Json;
using Microsoft.Maui.Storage;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace LoopMeet.App.Features.Auth;

public sealed class MauiSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "loopmeet.auth.session";

    public void SaveSession(Session session)
    {
        var json = JsonSerializer.Serialize(session);
        Preferences.Default.Set(SessionKey, json);
    }

    public void DestroySession()
    {
        Preferences.Default.Remove(SessionKey);
    }

    public Session? LoadSession()
    {
        var json = Preferences.Default.Get(SessionKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            return null;
        }
    }
}
