# Data Model: Sign in with Apple

This feature introduces **no new database tables, columns, or persisted schemas**. Identity storage lives in Supabase's `auth.users` and `auth.identities` tables, which the Apple OAuth provider configuration already populates server-side. Client-side persistence reuses the existing `Preferences["loopmeet.auth.access_token"]`.

The "data model" here documents the in-memory entities the client constructs from Apple's identity token + Supabase's response, and the rules that govern when they trigger account creation versus account linking.

## 1) AppleNativeCredential (client-side, in-memory)

Captured from `AuthenticationServices.ASAuthorizationAppleIDCredential` on Apple platforms. Never serialized to disk.

| Field | Source | Always present? |
|---|---|---|
| `UserIdentifier` | `credential.User` | Yes — the stable Apple user identifier |
| `IdentityToken` | `credential.IdentityToken` decoded UTF-8 | Yes — JWT signed by Apple |
| `AuthorizationCode` | `credential.AuthorizationCode` decoded UTF-8 | Yes — unused for native flow, kept for future server-side exchange |
| `Email` | `credential.Email` | Only on first authorization (or never, if user picked "Hide My Email" and Apple chose not to share even the relay) |
| `FullName` | `credential.FullName.GivenName + " " + credential.FullName.FamilyName` | Only on first authorization |
| `RealUserStatus` | `credential.RealUserStatus` | Yes — Apple's fraud heuristic; not used by LoopMeet today, but logged for support diagnosis |

**Lifecycle**: created inside `AppleAuthCredentialProvider.RequestCredentialAsync` from the `ASAuthorizationController` delegate callback, returned to `AuthService.SignInWithAppleAsync`, used to build the Supabase token exchange request, then discarded. The raw nonce is held alongside the credential request and discarded after token exchange.

## 2) AppleIdentityTokenClaims (in-memory, decoded from the JWT)

Not constructed as a separate C# type — these are claims inside `IdentityToken`. Supabase validates the token; the client does not need to. Documented here so the contract is explicit.

| Claim | Meaning | Used by LoopMeet client? |
|---|---|---|
| `iss` | `https://appleid.apple.com` | No — Supabase validates |
| `sub` | Stable Apple user identifier (same value as `UserIdentifier` above) | No — Supabase keys the linkage |
| `aud` | The app's client ID (bundle ID `io.loopmeet.app`) | No — Supabase validates |
| `iat` / `exp` | Issue + expiry timestamps | No — Supabase validates |
| `nonce` | `SHA256(rawNonce)`, hex-encoded | No — Supabase validates against the raw nonce we send |
| `email` | Verified email (real or `*@privaterelay.appleid.com`) | Read by Supabase, surfaces on the returned `User.Email` |
| `email_verified` | `"true"` (always for Apple, since Apple verifies) | No |
| `is_private_email` | `"true"` when the user chose Hide My Email | No — logged-only diagnostic if present |
| `auth_time` | Time of underlying authentication | No |

## 3) OAuthSignInResult (existing, reused)

Already defined in [`src/LoopMeet.App/Features/Auth/Models/AuthModels.cs`](../../src/LoopMeet.App/Features/Auth/Models/AuthModels.cs#L47-L54). Apple sign-in populates the same shape Google sign-in produces:

```csharp
public sealed class OAuthSignInResult
{
    public string  AccessToken { get; init; } = string.Empty; // Supabase session token
    public string? DisplayName { get; init; }                  // From credential.FullName, only on first auth
    public string? Email       { get; init; }                  // From credential.Email or JWT email claim
    public string? Phone       { get; init; }                  // Always null for Apple (no phone claim)
    public string? AvatarUrl   { get; init; }                  // Always null for Apple (no picture claim)
}
```

**Mapping rules**:
- `AccessToken` — from `session.AccessToken` returned by `_client.Auth.SignInWithIdToken(...)`.
- `DisplayName` — `credential.FullName` when present; null otherwise. The viewmodel's existing branch keeps the prior display name when null.
- `Email` — `credential.Email` when present, else the `email` claim parsed from the identity JWT, else null. When null, the viewmodel cannot create a new profile; this is the spec's "Apple ID provides no email" edge case, and is the case where the merging falls back to the stable Apple user identifier handled by Supabase.
- `Phone`, `AvatarUrl` — always null. Apple does not surface phone numbers and does not provide avatar URLs.

## 4) Server-side state (Supabase)

The Supabase `auth.users` row and matching `auth.identities` row are populated by Supabase's GoTrue server when `SignInWithIdToken(Provider.Apple, …)` succeeds. No client code writes these tables directly. The flow is:

| Scenario | Server behavior |
|---|---|
| **First Apple sign-in, email does not match any existing user** | Server creates a new `auth.users` row + `auth.identities` row with `provider = 'apple'`. |
| **First Apple sign-in, email matches an existing email-registered user** | Server attaches a new `auth.identities` row with `provider = 'apple'` to the existing `auth.users` row. Future Apple sign-ins return the same `auth.users` row. |
| **First Apple sign-in, email matches an existing Google-registered user** | Same as above — server attaches the Apple identity to the existing user, so all three providers (email if set, Google, Apple) open the same account. |
| **Subsequent Apple sign-in by a previously-linked user** | Server looks up the existing `auth.identities` row by `provider = 'apple'` + Apple user identifier (`sub` claim), returns the same `auth.users` row. Email no longer matters for the lookup; the Apple `sub` is the key. |
| **Apple-only sign-in by a user who declined to share email** | Server uses the Apple `sub` claim alone to attach or look up the identity. New users get an `auth.users` row with `email = null`. |
| **Soft-deleted / suspended account** | GoTrue returns the same auth error the Google and email flows already surface; viewmodel maps to the same user-facing message. |

LoopMeet's own `user_profiles` table (managed by the existing `_usersApi`) is touched only when a profile does not already exist for the returning Supabase user — exactly mirroring the Google flow's behavior in [`LoginViewModel.SignInWithGoogleAsync`](../../src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs#L120-L173).

## 5) Local persistence

| Key | Type | Set when | Reused from |
|---|---|---|---|
| `loopmeet.auth.access_token` | `string` (JWT) | Apple sign-in succeeds → `AuthService.SaveAccessToken(_accessToken)` runs after `SignInWithIdToken` | Existing email + Google flows |

No new Preferences keys. No new files written to disk. No new MAUI Secure Storage entries.

## 6) State transitions

```
                      tap "Continue with Apple"
                                 |
                                 v
         (Apple) ASAuthorizationController.PerformRequests()
                                 |
            +--- user cancels --> back to LoginPage idle, no error
            |
            +--- Apple error  --> back to LoginPage with humane error
            |
            v (success)
         AppleAuthCredentialProvider returns AppleNativeCredential
                                 |
                                 v
    AuthService.SignInWithAppleAsync builds OAuthSignInResult by calling
       _client.Auth.SignInWithIdToken(Provider.Apple, idToken, rawNonce)
                                 |
            +--- server: new user --+
            |                      |
            +--- server: linked    +--> Supabase returns Session
                 to existing user           |
                                            v
                              AuthService.SaveAccessToken(session.AccessToken)
                                            |
                                            v
                  LoginViewModel.TryGetProfileAsync (profile lookup by user)
                                            |
            +--- profile exists ---+--- profile null ---+
            |                                            |
            +---> (optionally update avatar)             +---> TryCreateProfileFromOAuthAsync (upsert)
            |                                            |
            +-----+----------------+---------------------+
                  |
                  v
            CacheProfileSummaryAsync (UserProfileCache)
                  |
                  v
            AuthSessionService.HandleSuccessfulSignInAsync
            (notification setup, device registration, profile timezone sync)
                  |
                  v
            Shell.Current.GoToAsync(SignedInTabs.HomeShellPath)
```

The branching logic — and every method named in this diagram other than the Apple-specific bits — already exists from the Google flow.
