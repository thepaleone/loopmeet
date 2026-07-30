# Contract: Sign in with Apple

This contract describes the interfaces that this feature introduces, modifies, and depends on. It is the source of truth for naming, parameter shape, return value shape, error behavior, and platform conditional gating. Implementation must conform to it; tests are written against it.

## 1. `AuthService.SignInWithAppleAsync` (public method, cross-platform signature)

```csharp
namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    // Existing:
    // public Task<AuthSession> SignInWithEmailAsync(string email, string password);
    // public Task<OAuthSignInResult> SignInWithGoogleAsync();

    /// <summary>
    /// Initiates Apple's native Sign in with Apple flow on Apple platforms,
    /// exchanges the resulting identity token for a Supabase session, and
    /// returns the canonical OAuthSignInResult. On non-Apple platforms,
    /// throws PlatformNotSupportedException — but the LoginViewModel never
    /// invokes this method on non-Apple targets because its caller is
    /// compile-gated behind #if IOS || MACCATALYST.
    /// </summary>
    /// <returns>
    /// An OAuthSignInResult whose AccessToken is the Supabase session token.
    /// On user-cancel returns an OAuthSignInResult with empty AccessToken
    /// (matching the SignInWithGoogleAsync convention).
    /// </returns>
    public Task<OAuthSignInResult> SignInWithAppleAsync();
}
```

**Error contract** (matches `SignInWithGoogleAsync`):
- User cancels native Apple sheet → returns `new OAuthSignInResult()` (empty `AccessToken`). Caller treats as "did not complete, no toast".
- Apple service unreachable / identity-token validation fails on Supabase / token-exchange fails → throws (any exception type). Caller wraps in a humane error message.
- Apple returns a credential with no email **and** no existing Supabase identity linkage → Supabase creates a new `auth.users` row with `email = null`. The returned `OAuthSignInResult.Email` is `null` and the caller's `TryCreateProfileFromOAuthAsync` short-circuits to `return false` — user reaches Home without a profile row yet, identical to today's Google early-no-email case (which is rare but already handled).

**Implementation outline**:

```csharp
public async Task<OAuthSignInResult> SignInWithAppleAsync()
{
#if IOS || MACCATALYST
    var (rawNonce, hashedNonce) = AppleAuthNonce.Generate();
    var credential = await AppleAuthCredentialProvider.RequestCredentialAsync(hashedNonce);

    if (credential is null)
    {
        return new OAuthSignInResult(); // user-cancel signal, mirrors Google convention
    }

    var idToken = Encoding.UTF8.GetString(credential.IdentityToken!.ToArray());

    var session = await _client.Auth.SignInWithIdToken(
        Constants.Provider.Apple,
        idToken,
        rawNonce);

    _accessToken = session?.AccessToken;
    SaveAccessToken(_accessToken);

    var user = session?.User;
    return new OAuthSignInResult
    {
        AccessToken = _accessToken ?? string.Empty,
        DisplayName = BuildDisplayName(credential) ?? GetUserDisplayName(user),
        Email       = credential.Email ?? user?.Email ?? TryGetJwtClaim(_accessToken, "email"),
        Phone       = null,
        AvatarUrl   = null
    };
#else
    throw new PlatformNotSupportedException(
        "Sign in with Apple is only available on iOS and MacCatalyst targets.");
#endif
}

private static string? BuildDisplayName(ASAuthorizationAppleIDCredential credential)
{
    var first = credential.FullName?.GivenName;
    var last  = credential.FullName?.FamilyName;
    if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last)) return null;
    return ($"{first} {last}").Trim();
}
```

## 2. `AppleAuthCredentialProvider` (new, Apple-only)

Lives at `src/LoopMeet.App/Features/Auth/Platforms/Apple/AppleAuthCredentialProvider.cs`. **Entire file is wrapped in `#if IOS || MACCATALYST`**, so it does not appear in non-Apple build outputs.

```csharp
#if IOS || MACCATALYST
using AuthenticationServices;
using Foundation;

namespace LoopMeet.App.Features.Auth.Platforms.Apple;

internal static class AppleAuthCredentialProvider
{
    /// <summary>
    /// Presents the native Sign in with Apple sheet. Returns the granted
    /// credential, or null if the user cancels.
    /// Throws when Apple's authorization controller reports a non-cancel error.
    /// </summary>
    public static Task<ASAuthorizationAppleIDCredential?> RequestCredentialAsync(string hashedNonce);
}
#endif
```

**Behavior**:
- Construct `ASAuthorizationAppleIDProvider`, request the scopes `[Email, FullName]`, set `request.Nonce = hashedNonce`.
- Present via `ASAuthorizationController.PerformRequests()`. The controller's `Delegate` callbacks bridge to a `TaskCompletionSource<ASAuthorizationAppleIDCredential?>`.
- On `DidComplete` with an `ASAuthorizationAppleIDCredential`: complete with the credential.
- On `DidComplete` with any other credential type: complete with `null` (defensive — currently unreachable per Apple's API contract for AppleID requests).
- On `DidFail` with `Canceled` reason: complete with `null`.
- On `DidFail` with any other reason: complete with the exception, surfacing it to the caller for humane-error mapping.

**Why a separate file**: confines all `AuthenticationServices.*` references to one compile-gated translation unit. The cross-platform `AuthService` remains free of Apple-SDK references at the call site outside the `#if` block.

## 3. `AppleAuthNonce` (new, cross-platform — uses only `System.Security.Cryptography`)

```csharp
namespace LoopMeet.App.Features.Auth;

internal static class AppleAuthNonce
{
    /// <summary>
    /// Generates a (rawNonce, hashedNonce) pair. The raw nonce is sent to
    /// Supabase; the SHA-256 hex of the raw nonce is sent to Apple.
    /// </summary>
    public static (string Raw, string Hashed) Generate();
}
```

**Behavior**:
- 32 random bytes from `RandomNumberGenerator.GetBytes(32)`.
- Encode as URL-safe base64 → that is the raw nonce.
- SHA-256 hash the raw nonce, then lowercase-hex encode → that is the hashed nonce.

This file is cross-platform (no Apple SDK references), so it doesn't need an `#if` guard. On non-Apple platforms it's dead code that the linker can trim — but it's tiny.

## 4. `LoginViewModel` additions

```csharp
public sealed partial class LoginViewModel : ObservableObject
{
    // Existing properties / commands unchanged.

    /// <summary>
    /// True only on iOS and MacCatalyst. The "Continue with Apple" button's
    /// IsVisible binding subscribes to this. On non-Apple targets, the
    /// command itself is not registered (see #if guard below).
    /// </summary>
    public bool ShowAppleSignIn =>
#if IOS || MACCATALYST
        true;
#else
        false;
#endif

#if IOS || MACCATALYST
    [RelayCommand]
    private async Task SignInWithAppleAsync()
    {
        // Mirrors SignInWithGoogleAsync line-for-line, swapping the service call:
        //
        //   var authResult = await _authService.SignInWithAppleAsync();
        //
        // Branch logic, error handling, navigation, profile-creation fallback,
        // and post-sign-in side-effect dispatch are identical to the Google
        // branch — copied verbatim except for log messages and error text
        // (s/Google/Apple/).
    }
#endif
}
```

The command name `SignInWithAppleCommand` is auto-generated by the `[RelayCommand]` source generator from the `SignInWithAppleAsync` method name — mirroring how `SignInWithGoogleCommand` is generated today.

## 5. `LoginPage.xaml` additions

```xml
<Button Text="Continue with Apple"
        IsVisible="{Binding ShowAppleSignIn}"
        Command="{Binding SignInWithAppleCommand}">
    <Button.Triggers>
        <DataTrigger TargetType="Button" Binding="{Binding IsBusy}" Value="True">
            <Setter Property="IsEnabled" Value="False" />
        </DataTrigger>
    </Button.Triggers>
</Button>
```

Placed immediately after the existing "Continue with Google" button (line ~32). The `IsVisible="{Binding ShowAppleSignIn}"` binding ensures the button is collapsed (`IsVisible=false` defaults to layout-collapsed in MAUI) on non-Apple builds; the markup remains in the XAML file but renders nothing.

## 6. Entitlements

Add the same key to both `Platforms/iOS/Entitlements.Debug.plist` and `Platforms/iOS/Entitlements.Release.plist`:

```xml
<key>com.apple.developer.applesignin</key>
<array>
    <string>Default</string>
</array>
```

No changes to `Info.plist`. No changes to `AndroidManifest.xml`. No changes to `Platforms/MacCatalyst/Entitlements.plist` (MacCatalyst inherits the iOS-style code-signing chain; if the user separately builds for MacCatalyst, they will need the same entitlement applied — out of scope for this PR, can be added as a follow-up if/when MacCatalyst is built).

## 7. Apple Developer Portal (out-of-source prerequisites)

Documented here for completeness; the user performs these once per developer environment, not part of the source change:

1. App ID `io.loopmeet.app` → enable **Sign in with Apple** capability.
2. Regenerate (do not just edit) the iOS App Development provisioning profile so it includes the capability.
3. Regenerate the Ad Hoc / App Store provisioning profile for Release builds.
4. Re-download both profiles, double-click to install locally.
5. In Supabase dashboard → Authentication → Providers → Apple, confirm the Service ID, Team ID, Key ID, and `.p8` private key are populated and **Enabled**. (Spec confirms these are already in place.)

## 8. Identity-merge trigger conditions (server-side, documented for verification)

Supabase will attach the new `provider = 'apple'` identity to an existing `auth.users` row when **all** of:

- The Apple identity token's `email` claim equals the existing user's `email` (case-insensitive), **and**
- The existing user's email is verified, **and**
- The existing user is not soft-deleted / suspended.

Otherwise Supabase creates a new `auth.users` row. The Apple `sub` claim is always recorded on the new `auth.identities` row regardless. Subsequent Apple sign-ins look up by `(provider='apple', identity_id=sub)` and bypass email entirely.

These rules are GoTrue server defaults and are not configured client-side. They match exactly what the Google flow relies on.
