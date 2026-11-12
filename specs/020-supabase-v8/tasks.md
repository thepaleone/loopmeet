# 020: Supabase .NET 8 upgrade

A dependency upgrade, so there's no spec: this file lists the work, and `quickstart.md` lists the device checks.

**From:** `Supabase` 1.1.1, with `Supabase.Gotrue` 6.0.3 and `Supabase.Postgrest` 4.0.3.
**To:** `Supabase` 8.9.0. Every `Supabase.*` package is now versioned in lockstep.

8.10.0 was a day old at the time, and all of its changes are to Realtime, which we don't use, so 8.9.0 is pinned.

The breaking changes are in the [8.0.0 migration guide](https://github.com/supabase/supabase-csharp/blob/master/docs/migrations/v8.0.0.md) and the changelog.

## What changes for us

| Change in Supabase 8 | Effect here | Handling |
| --- | --- | --- |
| Gotrue's `Session` names its JSON properties with System.Text.Json attributes (`access_token`) | `MauiSessionPersistence` stored sessions with the C# property names (`AccessToken`), because System.Text.Json ignored 6.x's Newtonsoft attributes. Under 8.x those load with every field empty, which would sign every user out once. | `SessionStorageFormat` reads both formats. `MauiSessionPersistence` rewrites a legacy session in the new format the first time it loads it. |
| `Session.Expired()` and `Session.ExpiresAt()` removed (not in the migration guide) | `SessionCoordinator` used both. | `SessionExpiry` uses the same rule: `CreatedAt + ExpiresIn`, and an unknown expiry counts as expired. |
| Gotrue classifies errors by `error_code` (8.3.1), and adds `NetworkError` and `CloudflareNetworkError` | A network reason with a 4xx status would have fallen through to "definitive" and signed the user out. | `SessionFailureClassifier` lists both as transient. |
| A failed refresh no longer destroys the session (8.0.0) | 010's coordinator already keeps the session itself. Its workarounds still apply. | None in code. Device checks in `quickstart.md`. |
| `IPostgrestClient.AddDebugHandler` deprecated | The Api's only build warning. | Removed. It only logged query text at Debug level, and the error middleware already logs failures. |
| Postgrest models serialize with System.Text.Json | The 7 models are plain `[Column]` and `[PrimaryKey]` with no converters. Api tests use in-memory fakes, so they don't exercise this. | Staging checks in `quickstart.md`. |
| `Delete()` returns the deleted rows, `Single()` throws on more than one row, OAuth `State` removed, Realtime `postgres_changes` ordering, `PingCheckAsync` rename, `netstandard2.1` | Not used, or ignored by our code. | None. |

## Tasks

- [x] T001 Bump `Supabase` to 8.9.0 in the Api, Infrastructure and App projects, and `Supabase.Gotrue` to 8.9.0 in `LoopMeet.App.Tests`.
- [x] T002 Tests first:
  - `SessionStorageFormatTests`: a legacy fixture generated with the real 6.0.3 package keeps its tokens, `ExpiresIn`, `CreatedAt` and user. The current format round-trips. Unusable input returns null.
  - `SessionExpiryTests`
  - New `SessionFailureClassifierTests` cases.
  - `SessionSurfaceTests` source checks for `MauiSessionPersistence` and `SessionCoordinator`.
- [x] T003 Add `SessionStorageFormat` and `SessionExpiry`, and wire them into `MauiSessionPersistence` and `SessionCoordinator`.
- [x] T004 Add `NetworkError` and `CloudflareNetworkError` to `SessionFailureClassifier`.
- [x] T005 Remove the Api's Postgrest debug handler.
- [x] T006 Builds and tests:
  - Api builds with 0 warnings.
  - Android app builds with 0 errors.
  - Api 142, App 220, Core 29 and Infrastructure 1 tests pass.
- [x] T007 Staging: deploy the Api from a `staging/**` tag, then run `quickstart.md` rows 1–5.
- [x] T008 Android: run `quickstart.md` rows 6–10, including updating over a build of the old version.
