# OpenCode Agent Memory

Purpose: central, repository-local memory for OpenCode and other coding agents so behavior directives and project context survive tool/session changes.

## Sources
- `CLAUDE.md` (last updated: 2026-03-09)
- `AGENTS.md` (last updated: 2026-02-16)

## Effective Directives (Merged)
- Follow standard C#/.NET conventions.
- Keep project structure assumptions aligned with:
  - `src/`
  - `tests/`
- Treat feature plans as source-of-truth for current stack and recent changes.

## Active Technology Context (Consolidated)
- C# 13 / .NET 10
- Microsoft.Maui.Controls 10.0.30
- CommunityToolkit.Mvvm 8.4.0
- CommunityToolkit.Maui 14.0.0
- ASP.NET Core Web API / minimal APIs
- Refit 10.0.1 and Refit.HttpClientFactory 10.0.1
- Polly
- Microsoft.Extensions.Logging
- Supabase 1.1.1 / Supabase.Client / Supabase client SDKs
- EF Core + Npgsql
- Serilog
- FluentValidation 12.1.1

## Lessons Learned / Implementation Notes
- `UserProfileCache` is JSON in `Preferences`; no schema migration needed for profile-avatar caching behavior.
- `UserProfileResponse.AvatarUrl` already exists and should be reused instead of introducing duplicate fields.
- For `002-split-home-tabbar`, no new persistence/schema changes were required.
- For `003-profile-settings-tab`, Supabase Postgres uses `user_profiles` and `memberships` with RLS; avatar override/source metadata required additive migration.

## Recent Feature Memory
- `006-group-meetups`
- `005-profile-avatar`
- `004-ui-polish`
- `003-profile-settings-tab`
- `002-split-home-tabbar`
- `001-auth-groups-mvp`

## Raw Snapshot: CLAUDE.md
```markdown
# loopmeet Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-09

## Active Technologies
- C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, Refit 10.0.1, Supabase 1.1.1 (005-profile-avatar)
- `UserProfileCache` (JSON in `Preferences`) — no schema changes; `UserProfileResponse.AvatarUrl` already exists (005-profile-avatar)
- C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0, Refit.HttpClientFactory 10.0.1, Supabase 1.1.1, FluentValidation 12.1.1 (006-group-meetups)
- Supabase (PostgreSQL) via Postgrest clien (006-group-meetups)

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
- 006-group-meetups: Added C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0, Refit.HttpClientFactory 10.0.1, Supabase 1.1.1, FluentValidation 12.1.1
- 005-profile-avatar: Added C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, Refit 10.0.1, Supabase 1.1.1

- 004-ui-polish: Added C# 13 / .NET 10 + Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Maui 14.0.0, CommunityToolkit.Mvvm 8.4.0

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
```

## Raw Snapshot: AGENTS.md
```markdown
# loopmeet Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-16

## Active Technologies
- C# / .NET 10 + .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core Web API (existing backend unchanged for this feature) (002-split-home-tabbar)
- N/A for this feature (no new persistence or schema changes; existing group/invitation data sources are reused) (002-split-home-tabbar)
- C# / .NET 10 + .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core minimal APIs, Supabase client SDKs (003-profile-settings-tab)
- Supabase Postgres (`user_profiles`, `memberships`) with RLS; additive migration required for avatar override/source metadata (003-profile-settings-tab)

- C# / .NET 10 + .NET MAUI, ASP.NET Core Web API, EF Core + Npgsql, Supabase.Client, CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Serilog (001-auth-groups-mvp)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET 10

## Code Style

C# / .NET 10: Follow standard conventions

## Recent Changes
- 003-profile-settings-tab: Added C# / .NET 10 + .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core minimal APIs, Supabase client SDKs
- 002-split-home-tabbar: Added C# / .NET 10 + .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core Web API (existing backend unchanged for this feature)

- 001-auth-groups-mvp: Added C# / .NET 10 + .NET MAUI, ASP.NET Core Web API, EF Core + Npgsql, Supabase.Client, CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Serilog

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
```
