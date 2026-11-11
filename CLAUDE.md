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
- C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). No database schema changes. + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client), `System.TimeProvider` (BCL). Deno std `@std/assert` for edge-function tests. No new NuGet packages. (012-past-meetups)
- Existing Supabase `meetups` table, columns, indexes and RLS unchanged. "Past" is derived at read time. (012-past-meetups)
- C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). SQL (PostgreSQL 17, Supabase migrations). + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest `Rpc`, `Functions`), `System.TimeProvider`. `@supabase/supabase-js` in edge functions, WebCrypto for the Apple ES256 client secret, `jsr:@std/assert@1` for tests. **New test tooling**: pgTAP through the Supabase CLI (`supabase test db`). No new NuGet or npm packages. (016-account-deletion)
- New tables `account_deletions` and `apple_authorizations`, a view `active_memberships`, 8 SQL functions, and recreated RLS policies on `groups`, `memberships`, `invitations`, `meetups` and `user_profiles` ([data-model.md](specs/016-account-deletion/data-model.md)). Avatar objects live in Supabase Storage under `{bucket}/{userId}/`. (016-account-deletion)

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
- 020-supabase-v8: Upgraded every `Supabase.*` package to 8.9.0 (lockstep versions; Newtonsoft.Json replaced by System.Text.Json). `SessionStorageFormat` reads the pre-8 stored session format, `SessionExpiry` replaces the removed `Session.Expired()`/`ExpiresAt()`. No schema changes.
- 017-loopmeet-site: Added a static site in `web/public/` (HTML, CSS, ES-module JavaScript, no build) served by the Cloudflare Worker `loopmeet-account` (static assets, `account.loopmeet.io`), with `@supabase/supabase-js` 2.x vendored and hash-pinned; tested with `deno test`/`deno lint` in `web/tests/`. ASP.NET Core CORS policy `Site` (`Cors:AllowedOrigins`) on the Api, and CORS in `apple-authorization` (`SITE_ORIGINS` secret). No new NuGet or npm packages, no schema changes.
- 016-account-deletion: Added C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). SQL (PostgreSQL 17, Supabase migrations). + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest `Rpc`, `Functions`), `System.TimeProvider`. `@supabase/supabase-js` in edge functions, WebCrypto for the Apple ES256 client secret, `jsr:@std/assert@1` for tests. **New test tooling**: pgTAP through the Supabase CLI (`supabase test db`). No new NuGet or npm packages.
- 012-past-meetups: Added C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). No database schema changes. + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client), `System.TimeProvider` (BCL). Deno std `@std/assert` for edge-function tests. No new NuGet packages.
- 011-meetup-interactions: Added C# 13 / .NET 10 (MAUI client + minimal-API backend). No database schema changes. + Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client). No new packages.


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
- `012-past-meetups`: the "is this meetup past?" rule (past once its local calendar day has ended, timezone fallback `America/Los_Angeles`) exists twice — `src/LoopMeet.Core/Meetups/MeetupLifecycle.cs` (lists, edit validation) and `supabase/functions/_shared/meetup-lifecycle.ts` (push suppression). Change both together and extend `supabase/functions/_shared/meetup-lifecycle.cases.json`, which both test suites read. Edge-function tests import `jsr:` specifiers directly: `deno test` run from the repo root ignores the per-function `deno.json` import maps.
- `016-account-deletion`: edge functions use the service-role key, which bypasses RLS, so any code that picks notification recipients must read `active_memberships` (it drops users pending deletion and their groups), never `memberships`. Shared edge modules that tests import type the client with `import type { SupabaseClient } from "npm:@supabase/supabase-js@2"`, because the root `deno test` run has no import map.
- `016-account-deletion`: every RLS policy on `groups`, `memberships`, `invitations`, `meetups` and `user_profiles` ANDs `private.account_is_active` / `private.group_is_active` so a user pending deletion vanishes for everyone else. A new or recreated policy must keep those conditions, scoped `to authenticated` (anon has no usage on `private`).
- `016-account-deletion`: the 30-day grace period exists twice: `request_account_deletion()` in SQL (authoritative) and `AccountDeletionPolicy.GracePeriod` (preview display). Change both together; tests pin both.
- `016-account-deletion`: the Api's 403 `account_pending_deletion` is not a sign-out. `AccountStatusHandler` (after `ApiAuthHandler`) raises `AccountDeletionSignal`, and `AccountDeletionGate` navigates to `//pending-deletion`. Sign-in paths in `LoginViewModel` must catch it (`AccountStatusHandler.IsPendingDeletion(ex.Content)`) before their generic catch.
- `017-loopmeet-site`: the website's origin list lives in three places: `Cors:AllowedOrigins` in the Api's `appsettings.json` (one comma-separated string, not an array, so overrides replace it whole), the `SITE_ORIGINS` Supabase secret, and Supabase's Auth Redirect URLs (as `<origin>/account/`). Change them together. The `*` matching rule exists twice, `src/LoopMeet.Api/Cors/SiteOrigins.cs` and `supabase/functions/apple-authorization/cors.ts`; extend `site-origins.cases.json`, which both suites read.
- `017-loopmeet-site`: the privacy policy (`web/public/privacy/index.html`) and `web/data-safety.md` must change with `specs/016-account-deletion/data-retention.md`; `privacy-policy.test.js` checks the retention numbers against it.
- `017-loopmeet-site`: `web/public/vendor/supabase.js` is pinned by SHA-256 in `web/vendor.lock` (`vendor.test.js`). Update it only from the npm tarball, after checking its integrity, and keep it free of `eval` (the CSP is `script-src 'self'`). It's excluded from `deno lint`.
- `017-loopmeet-site`: the Supabase Apple provider's Secret Key for web sign-in expires after at most 6 months; regenerate it before then or web Sign in with Apple stops working. Generate it with `supabase/functions/_tools/apple-client-secret.ts` (usage in its header), which reuses `createClientSecret` from `_shared/apple-auth.ts`.
- `020-supabase-v8`: the stored Gotrue session (`loopmeet.auth.session`) exists in two formats: snake_case (Supabase 8) and the C# property names written by app builds on Gotrue 6. `SessionStorageFormat` reads both and `MauiSessionPersistence` rewrites the old one on load. Keep the legacy read until no installed build predates 020; `SessionStorageFormatTests` holds a real 6.0.3 fixture.
- `supabase test db` (pgTAP) locally runs against podman: point the CLI at it first with `export DOCKER_HOST="unix://$(podman machine inspect --format '{{.ConnectionInfo.PodmanSocket.Path}}')"`.
- `IdentityModelEventSource.ShowPII` is enabled only in Development (`Program.cs`): token validation errors can carry an email, and `data-retention.md` promises Api logs never do. `LoggingPrivacyTests` enforces it.
- AGENTS.md and OPENCODE.md are separate generated snapshots of this same file and have drifted (AGENTS.md stops at 008 and still lists the stale EF Core stack). Treat CLAUDE.md + `.specify/memory/constitution.md` as the sources of truth; don't trust AGENTS.md's tech list.

## Real commands

- Build/deploy Android staging: `dotnet build -c Staging -t:Run -f net10.0-android src/LoopMeet.App`, or `deploy/deploy-android.sh -c Debug|Release [-d <adb-serial>]` (this script disables AOT/trimming as a documented workaround for a `MauiApplication.n_onCreate` `UnsatisfiedLinkError` — don't re-enable them without addressing that first).
- Tests: `dotnet test` against `tests/**/*.csproj` (CI sets fake `Supabase__*` env vars; see `.github/workflows/tests.yml`).
- CI runs only the suites a change needs. `.github/scripts/ci-scope.sh` maps changed paths to suites, and docs (`*.md`, `specs/`, `.specify/`) select nothing. When a test starts reading files from another tree (as Api.Tests and Core.Tests read `supabase/functions`), add that path to the map and a case to `ci-scope.test.sh` (`bash .github/scripts/ci-scope.test.sh`), or CI will skip the test when those files change. `Tests Gate` and `Build Gate` always report; they are the checks to mark required.
- Api container: `docker-compose.yml` requires `SUPABASE__URL`, `SUPABASE__ANONORPUBLISHABLEKEY`, `SUPABASE__SERVICEORSECRETKEY`, `SUPABASE__JWTISSUER`, `GOOGLE__PLACESAPIKEY` — fails fast if unset.

<!-- MANUAL ADDITIONS END -->
