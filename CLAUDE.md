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
- C# 13 / .NET 10 (MAUI client + minimal-API backend). No database schema changes. + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client). No new packages. (011-meetup-interactions)
- Existing Supabase tables, unchanged. `meetups.created_by_user_id` already exists; display name and group name are resolved at read time, never stored on the meetup. (011-meetup-interactions)

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
- 011-meetup-interactions: Added C# 13 / .NET 10 (MAUI client + minimal-API backend). No database schema changes. + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client). No new packages.
- 010-fix-auth-session: Added C# 13 / .NET 10 (MAUI client). No backend or Supabase schema changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 (Supabase.Gotrue 6.0.3: `TokenRefresh`, `GotrueException.Reason`, `Client.RefreshToken()`, `AddStateChangedListener`), CommunityToolkit.Mvvm 8.4.0, Refit.HttpClientFactory 10.1.6, OneSignalSDK.DotNet 6.1.8.
- 009-apple-signin: Added C# 13 / .NET 10 (MAUI client). No backend changes. + Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 + Supabase.Gotrue, AuthenticationServices framework (Microsoft.iOS / Microsoft.MacCatalyst — already part of those workloads, no new NuGet package).


<!-- MANUAL ADDITIONS START -->

## Constitution

`.specify/memory/constitution.md` (v0.1.0) supersedes all other guidance. Key gates every plan/PR must satisfy: tests are a required deliverable (not optional, regression test required for every bug fix), no dead code / commented-out code / unresolved TODOs in mainline, abstractions only justified by ≥2 real use cases, contract-first interfaces, structured logging on key flows, and documented privacy/retention rules for any social or stored user data.

## Real architecture (ignore stale tech listed above for 001-auth-groups-mvp)

Strict layering: `LoopMeet.Core` (Models/Interfaces/Validators) ← `LoopMeet.Infrastructure` (Repositories wrapping `Supabase.Client.From<T>()`, manual `Map()` methods, no AutoMapper) ← `LoopMeet.Api` (minimal-API endpoints + paired `{Domain}CommandService`/`{Domain}QueryService`). `LoopMeet.App` (MAUI) talks to `LoopMeet.Api` only via Refit (`I{Domain}Api` + a thin non-Refit wrapper ViewModels depend on), but talks to `Supabase.Client` directly for auth/session (Gotrue).

There is **no EF Core / Npgsql / Testcontainers anywhere in the real codebase** — 001-auth-groups-mvp's plan committed to that stack but it was never built; everything persists through the Supabase Postgrest client. Don't resurrect EF Core based on old specs or AGENTS.md.

Api's `Supabase.Client` is constructed per-request, scoped, and impersonates the caller's bearer token (not a service-role key) so Postgres RLS applies — don't swap this for a service-role client for convenience.

## Conventions (real, but not written down elsewhere)

- ViewModels: `sealed partial class : ObservableObject` using CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`); command methods are always suffixed `Async`.
- Navigation: `Shell.Current.GoToAsync("kebab-route", new Dictionary<string,object>{...})`; routes registered in `AppShell.xaml` (tabs) or imperatively via `Routing.RegisterRoute` in `AppShell.xaml.cs` (modals/detail pages).
- Api layer: expected failures use a Result-enum pattern (e.g. `GroupCommandResult(GroupCommandStatus, ...)`), not exceptions. Exceptions are reserved for truly exceptional states.
- QueryServices keep a 30-second in-memory TTL cache keyed like `"groups:{ownerUserId}"` — reuse this pattern for new query services rather than inventing a new caching approach.
- Testing: xUnit, **no mocking framework** (no Moq/NSubstitute) — hand-roll fakes under `tests/*/TestDoubles/`. Api tests use `WebApplicationFactory<Program>` + in-memory repository fakes.
- For MAUI/XAML code that can't be unit tested, the team uses "source-inspection" tests: xUnit tests that `File.ReadAllText` a `.cs`/`.xaml` file and assert specific strings are present/absent (bindings, method calls). This is the accepted way to satisfy Constitution Principle II (tests required) for native/XAML-only logic — use it rather than skipping tests on that code.
- `quickstart.md` manual device-matrix testing counts as first-class coverage for hardware/native-only behavior (camera picker, Apple Sign-In UI, push notification opens) — include one when a feature touches native APIs.
- `specs/*/plan.md` Constitution Check sections use a `| Principle | Status | Notes |` table (all specs from 004 onward) — follow this table format, not the older checkbox-list style from 001-003.
- `tasks.md` is a living execution log: keep `[x]` checkmarks updated in place as work completes rather than treating it as a disposable planning doc.

## Known gotchas

- `006-group-meetups`: meetup RLS at the DB layer intentionally allows all group members to CRUD, while the UI restricts mutation to owners (per FR-010). This is deliberate — don't "fix" the RLS to match the UI.
- `IdentityModelEventSource.ShowPII = true` is left on unconditionally in the Api — this is a latent PII-leak risk in logs, not an intentional decision; worth turning off outside local dev if you're touching that code.
- AGENTS.md and OPENCODE.md are separate generated snapshots of this same file and have drifted (AGENTS.md stops at 008 and still lists the stale EF Core stack). Treat CLAUDE.md + `.specify/memory/constitution.md` as the sources of truth; don't trust AGENTS.md's tech list.

## Real commands

- Build/deploy Android staging: `dotnet build -c Staging -t:Run -f net10.0-android src/LoopMeet.App`, or `deploy/deploy-android.sh -c Debug|Release [-d <adb-serial>]` (this script disables AOT/trimming as a documented workaround for a `MauiApplication.n_onCreate` `UnsatisfiedLinkError` — don't re-enable them without addressing that first).
- Tests: `dotnet test` against `tests/**/*.csproj` (CI sets fake `Supabase__*` env vars; see `.github/workflows/tests.yml`).
- Api container: `docker-compose.yml` requires `SUPABASE__URL`, `SUPABASE__ANONORPUBLISHABLEKEY`, `SUPABASE__SERVICEORSECRETKEY`, `SUPABASE__JWTISSUER`, `GOOGLE__PLACESAPIKEY` — fails fast if unset.

<!-- MANUAL ADDITIONS END -->
