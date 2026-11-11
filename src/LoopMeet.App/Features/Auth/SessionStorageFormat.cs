using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
// The Features.Auth.Session child namespace shadows the Gotrue type name here.
using GotrueSession = Supabase.Gotrue.Session;

namespace LoopMeet.App.Features.Auth;

/// <summary>A session read from storage, and whether it was in the pre-Supabase 8 format.</summary>
public readonly record struct StoredSession(GotrueSession Session, bool IsLegacyFormat);

/// <summary>
/// The JSON the app stores the Gotrue session as. Supabase.Gotrue 8 names its
/// properties with System.Text.Json attributes (<c>access_token</c>). Sessions
/// stored under 6.x have the C# property names (<c>AccessToken</c>), because
/// System.Text.Json ignored that version's Newtonsoft attributes. Both are read,
/// so updating the app doesn't sign anyone out.
/// </summary>
public static class SessionStorageFormat
{
    // Reads the legacy format: every property under its C# name, ignoring [JsonPropertyName].
    private static readonly JsonSerializerOptions LegacyOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { UseClrPropertyNames } },
    };

    public static string Serialize(GotrueSession session) => JsonSerializer.Serialize(session);

    public static StoredSession? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty("access_token", out _))
            {
                var session = document.RootElement.Deserialize<GotrueSession>();
                return session is null ? null : new StoredSession(session, IsLegacyFormat: false);
            }

            if (document.RootElement.TryGetProperty(nameof(GotrueSession.AccessToken), out _))
            {
                var session = document.RootElement.Deserialize<GotrueSession>(LegacyOptions);
                return session is null ? null : new StoredSession(session, IsLegacyFormat: true);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void UseClrPropertyNames(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.AttributeProvider is MemberInfo member)
            {
                property.Name = member.Name;
            }
        }
    }
}
