# Quickstart: Home Tab Navigation Split

**Date**: 2026-02-26

## Purpose

Validate the new signed-in tab bar experience with:
- Home placeholder tab
- Dedicated Groups tab
- Dedicated Invitations tab
- Existing group and invitation actions preserved

## Prerequisites

- .NET 10 SDK
- Xcode (for iOS simulator/device builds)
- Android SDK (for Android emulator/device builds)
- LoopMeet API configuration available for the MAUI app (same setup used by the existing groups/invitations flows)

## Environment Variables

Set the same variables used for local app/API development:

- `LOOPMEET_ENV=Development`
- `LOOPMEET_DB_CONNECTION=Host=localhost;Port=5432;Database=loopmeet_dev;Username=postgres;Password=postgres`
- `LOOPMEET_SUPABASE_URL=https://<project>.supabase.co`
- `LOOPMEET_SUPABASE_ANON_KEY=<anon_key>`
- `LOOPMEET_SUPABASE_JWT_ISSUER=https://<project>.supabase.co/auth/v1`

## Run the API (existing backend, no changes required for this feature)

```bash
dotnet restore
dotnet run --project /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.Api
```

## Build / Run the MAUI App

```bash
dotnet restore
dotnet build /Users/joel/projects/palehorse/loopmeet/src/LoopMeet.App
```

Use your IDE or platform-specific MAUI deployment commands to run on iOS/Android.

## Manual Validation Checklist

1. Sign in (or launch with a restorable session).
2. Confirm the signed-in experience opens on the `Home` tab by default.
3. Confirm the tab bar shows all three tabs: `Home`, `Groups`, `Invitations`.
4. Confirm each tab shows both:
   - an icon
   - a text label
5. Confirm the icon appears above the text label in the tab bar presentation on mobile.
6. Confirm the Home tab shows placeholder content only (no groups/invitations list mixed in).
7. Open `Groups` and confirm:
   - owned/member groups still load
   - pending invitations are not shown on this screen
   - selecting a group still opens group detail
8. Open `Invitations` and confirm:
   - pending invitations load
   - accept/decline actions still work
   - invitation detail can still open (if included in the UI)
9. Accept or decline an invitation and confirm the list refresh removes the completed pending invitation.
10. Verify empty states:
    - no groups
    - no invitations
    - neither groups nor invitations

## Automated Test Targets (planned / existing)

```bash
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Api.Tests
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Core.Tests
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Infrastructure.Tests
```

If an app-focused test project is added for this feature, also run:

```bash
dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.App.Tests
```

## Notes

- This feature is UI-only; existing API contracts are reused.
- Emoji-style tab icons are preferred. Animated GIF tab icons are not planned because native tab bar animation support is inconsistent across platforms.
