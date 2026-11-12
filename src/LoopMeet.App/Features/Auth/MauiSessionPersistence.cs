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
        Preferences.Default.Set(SessionKey, SessionStorageFormat.Serialize(session));
    }

    public void DestroySession()
    {
        Preferences.Default.Remove(SessionKey);
    }

    public GotrueSession? LoadSession()
    {
        var stored = SessionStorageFormat.Read(Preferences.Default.Get(SessionKey, string.Empty));
        if (stored is not { } found)
        {
            return null;
        }

        // A session saved before Supabase 8 is rewritten once in the current format.
        if (found.IsLegacyFormat)
        {
            SaveSession(found.Session);
        }

        return found.Session;
    }
}
