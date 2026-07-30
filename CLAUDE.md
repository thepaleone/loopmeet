# loopmeet Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-09

## Active Technologies
- C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, Refit 10.0.1, Supabase 1.1.1 (005-profile-avatar)
- `UserProfileCache` (JSON in `Preferences`) — no schema changes; `UserProfileResponse.AvatarUrl` already exists (005-profile-avatar)
- C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0, Refit.HttpClientFactory 10.0.1, Supabase 1.1.1, FluentValidation 12.1.1 (006-group-meetups)
- Supabase (PostgreSQL) via Postgrest clien (006-group-meetups)
- C# 13 / .NET 10 (MAUI client). No backend changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 + Supabase.Gotrue, AuthenticationServices framework (Microsoft.iOS / Microsoft.MacCatalyst — already part of those workloads, no new NuGet package). (009-apple-signin)
- None new. Sessions persist via the existing `MauiSessionPersistence` + `Preferences.Default["loopmeet.auth.access_token"]` mechanism. Supabase stores the identity linkage server-side; no Supabase schema changes are required because Supabase Apple OAuth provider is already configured. (009-apple-signin)
- C# 13 / .NET 10 (MAUI client). No backend or Supabase schema changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 (Supabase.Gotrue 6.0.3: `TokenRefresh`, `GotrueException.Reason`, `Client.RefreshToken()`, `AddStateChangedListener`), CommunityToolkit.Mvvm 8.4.0, Refit.HttpClientFactory 10.1.6, OneSignalSDK.DotNet 6.1.8. (010-fix-auth-session)
- `Preferences.Default` — Gotrue session JSON under `loopmeet.auth.session` (via existing `MauiSessionPersistence`) becomes the *only* credential store; the legacy `loopmeet.auth.access_token` key is removed (with one-time cleanup). `UserProfileCache` (`loopmeet.profile.cache`) unchanged in shape, cleared on sign-out. (010-fix-auth-session)

- C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Maui 14.0.0, CommunityToolkit.Mvvm 8.4.0 (004-ui-polish)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 13 / .NET 10

## Code Style

C# 13 / .NET 10: Follow standard conventions

## Recent Changes
- 010-fix-auth-session: Added C# 13 / .NET 10 (MAUI client). No backend or Supabase schema changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 (Supabase.Gotrue 6.0.3: `TokenRefresh`, `GotrueException.Reason`, `Client.RefreshToken()`, `AddStateChangedListener`), CommunityToolkit.Mvvm 8.4.0, Refit.HttpClientFactory 10.1.6, OneSignalSDK.DotNet 6.1.8.
- 009-apple-signin: Added C# 13 / .NET 10 (MAUI client). No backend changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 + Supabase.Gotrue, AuthenticationServices framework (Microsoft.iOS / Microsoft.MacCatalyst — already part of those workloads, no new NuGet package).
- 006-group-meetups: Added C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0, Refit.HttpClientFactory 10.0.1, Supabase 1.1.1, FluentValidation 12.1.1


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
