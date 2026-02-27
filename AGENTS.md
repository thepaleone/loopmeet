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
