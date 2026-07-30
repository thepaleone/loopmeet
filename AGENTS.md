# loopmeet Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-07-30

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

**This file is kept in sync with `CLAUDE.md`'s auto-generated sections above (both are produced by the same `.specify/scripts/bash/update-agent-context.sh`, which updates every existing agent file it finds).** The stale `001-auth-groups-mvp` entry that used to list EF Core + Npgsql has been removed here: that stack was planned but never built — the real codebase persists everything through the Supabase Postgrest client (see `LoopMeet.Infrastructure/Repositories/`).

For architecture, real conventions, known gotchas, and build/test commands, **see `CLAUDE.md`'s "Manual Additions" section** — this file intentionally does not duplicate that content, to avoid the two files drifting apart again. `.specify/memory/constitution.md` supersedes all guidance in either file.

<!-- MANUAL ADDITIONS END -->
