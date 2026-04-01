# Quickstart: Group Meetups

**Feature Branch**: `006-group-meetups`
**Date**: 2026-03-30

## Prerequisites

- .NET 10 SDK
- Supabase CLI (for local development / migrations)
- Google Places API key (for location autocomplete)

## Environment Setup

### 1. Google Places API Key

Obtain an API key from [Google Cloud Console](https://console.cloud.google.com/) with the **Places API (New)** enabled.

Set the environment variable on the API server:

```bash
export GOOGLE_PLACES_API_KEY="your-api-key-here"
```

For local development, add to the API project's user secrets or environment:

```bash
cd src/LoopMeet.Api
dotnet user-secrets set "GOOGLE_PLACES_API_KEY" "your-api-key-here"
```

### 2. Database Migration

Apply the new migration to create the `meetups` table:

```bash
supabase db push
```

Or for local development:

```bash
supabase migration up
```

### 3. Verify Migration

After migration, the `meetups` table should exist with RLS enabled and four policies (select, insert, update, delete) scoped to group members.

## Key Files

| Purpose | Path |
|---------|------|
| Migration | `supabase/migrations/YYYYMMDDHHMMSS_add_meetups.sql` |
| Core entity | `src/LoopMeet.Core/Models/Meetup.cs` |
| Repository | `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs` |
| API endpoints | `src/LoopMeet.Api/Endpoints/MeetupsEndpoints.cs` |
| Query service | `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs` |
| Command service | `src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs` |
| Places proxy | `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs` |
| API contracts | `src/LoopMeet.Api/Contracts/MeetupContracts.cs` |
| Refit clients | `src/LoopMeet.App/Services/MeetupsApi.cs`, `PlacesApi.cs` |
| App models | `src/LoopMeet.App/Features/Meetups/Models/MeetupModels.cs` |
| Create page | `src/LoopMeet.App/Features/Meetups/Views/CreateMeetupPage.xaml` |
| Edit page | `src/LoopMeet.App/Features/Meetups/Views/EditMeetupPage.xaml` |
| Group detail (modified) | `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` |
| Home page (modified) | `src/LoopMeet.App/Features/Home/Views/HomePage.xaml` |
| Integration tests | `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` |

## Testing

### Run API integration tests

```bash
cd tests/LoopMeet.Api.Tests
dotnet test --filter "MeetupsEndpoints"
```

### Run ViewModel unit tests

```bash
cd tests/LoopMeet.App.Tests
dotnet test --filter "MeetupViewModel"
```

## UI Patterns to Reuse

When implementing UI for this feature, replicate these existing patterns:

- **Card style**: `BubbleCardBorderStyle` (large cards) and `BubbleCardCompactBorderStyle` (list items) with `RoundRectangle CornerRadius`
- **SwipeView delete**: Same pattern as `PendingInvitationsPage.xaml` — `SwipeItems Mode="Execute"`, red background (`#DC2626`), with an added confirmation dialog via `DisplayAlert` before executing
- **FAB buttons**: 56x56, CornerRadius=28, `Primary`/`PrimaryDark` background, positioned with `VerticalOptions="End" HorizontalOptions="End"`
- **Avatar circles**: Border with `Ellipse` StrokeShape, Gray400 background, initial label + Image overlay
- **Theme support**: All colors via `{AppThemeBinding Light=..., Dark=...}`
- **Desktop fallback**: Use `{OnIdiom Phone=..., Desktop=...}` for button visibility alongside swipe actions
