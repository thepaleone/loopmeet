# Implementation Plan: Group Meetups

**Branch**: `006-group-meetups` | **Date**: 2026-03-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-group-meetups/spec.md`

## Summary

Add meetup management to groups: a new `meetups` table in Supabase with RLS policies permitting all group members to CRUD (UI restricted to owner for now). Server-side CRUD endpoints follow the existing Groups/Invitations pattern (minimal API + Query/Command services). A server-side proxy wraps Google Places API (New) autocomplete to keep the API key off the client. The MAUI app adds a Meetups feature module with create/edit pages, SwipeView delete on the group detail page (with confirmation dialog), and replaces the home page placeholder with upcoming meetup cards. All new UI reuses existing styles (BubbleCardBorderStyle, BubbleCardCompactBorderStyle, FAB pattern, avatar circles).

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Mvvm 8.4.0, CommunityToolkit.Maui 14.0.0, Refit.HttpClientFactory 10.0.1, Supabase 1.1.1, FluentValidation 12.1.1
**Storage**: Supabase (PostgreSQL) via Postgrest client
**Testing**: XUnit 2.9.3, TestWebApplicationFactory with InMemoryStore
**Target Platform**: iOS, Android, macOS Catalyst, Windows (.NET MAUI multi-platform)
**Project Type**: Mobile app + ASP.NET Core API backend
**Performance Goals**: Location autocomplete < 2s perceived latency; meetup list loads instantly from API cache (30s TTL matching existing pattern)
**Constraints**: Google Places API key must not be exposed on the client; existing BubbleCard styles and navigation patterns must be reused
**Scale/Scope**: Low meetup volume per group expected; no pagination required initially

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality | PASS | Follows existing service/repository/ViewModel patterns; no new abstractions |
| II. Tests Required | PASS | Unit tests for ViewModels, integration tests for API endpoints, migration tests via Supabase |
| III. UX First | PASS | 6 user stories with acceptance scenarios defined in spec; existing BubbleCard/SwipeView styles reused |
| IV. Simplicity | PASS | Direct Postgrest queries (no repository abstraction beyond existing pattern); server-side Places proxy is minimal passthrough |
| V. Modularity | PASS | New `Features/Meetups/` module in App project; new `Services/Meetups/` in Api project; clear boundaries |
| VI. Contract-First | PASS | API contracts defined in contracts/ before implementation |
| VII. Observability | PASS | Structured logging in command/query services following existing Serilog pattern |

**Additional Constraints:**
- Privacy defaults: Meetup locations visible only to group members (enforced by RLS) — PASS
- Data retention: Meetups are hard-deleted; no retention beyond deletion — documented in spec
- Acceptance scenarios: Defined for all 6 user stories — PASS

## Project Structure

### Documentation (this feature)

```text
specs/006-group-meetups/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── meetups-api.md
│   └── places-proxy-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── LoopMeet.App/
│   ├── Features/
│   │   ├── Meetups/                    # NEW: Meetup feature module
│   │   │   ├── Views/
│   │   │   │   ├── CreateMeetupPage.xaml(.cs)
│   │   │   │   └── EditMeetupPage.xaml(.cs)
│   │   │   ├── ViewModels/
│   │   │   │   ├── CreateMeetupViewModel.cs
│   │   │   │   └── EditMeetupViewModel.cs
│   │   │   └── Models/
│   │   │       └── MeetupModels.cs
│   │   ├── Groups/
│   │   │   ├── Views/
│   │   │   │   └── GroupDetailPage.xaml  # MODIFIED: Add meetups list + SwipeView delete + add FAB
│   │   │   └── ViewModels/
│   │   │       └── GroupDetailViewModel.cs  # MODIFIED: Load meetups, delete command
│   │   └── Home/
│   │       ├── Views/
│   │       │   └── HomePage.xaml        # MODIFIED: Replace placeholder with meetup cards
│   │       └── ViewModels/
│   │           └── HomeViewModel.cs     # MODIFIED: Load upcoming meetups
│   ├── Services/
│   │   ├── MeetupsApi.cs               # NEW: Refit interface + wrapper
│   │   └── PlacesApi.cs                # NEW: Refit interface + wrapper for location autocomplete
│   └── AppShell.xaml.cs                # MODIFIED: Register meetup routes
│
├── LoopMeet.Api/
│   ├── Endpoints/
│   │   └── MeetupsEndpoints.cs         # NEW: Meetup CRUD endpoints
│   ├── Services/
│   │   └── Meetups/
│   │       ├── MeetupQueryService.cs   # NEW: Query meetups with caching
│   │       └── MeetupCommandService.cs # NEW: Create/update/delete meetups
│   ├── Services/
│   │   └── Places/
│   │       └── PlacesProxyService.cs   # NEW: Google Places API proxy
│   └── Contracts/
│       ├── MeetupContracts.cs          # NEW: Request/response DTOs
│       └── PlacesContracts.cs          # NEW: Autocomplete DTOs
│
├── LoopMeet.Core/
│   └── Models/
│       └── Meetup.cs                   # NEW: Meetup domain entity
│
└── LoopMeet.Infrastructure/
    └── Repositories/
        └── MeetupRepository.cs         # NEW: Supabase Postgrest queries

supabase/
└── migrations/
    └── YYYYMMDDHHMMSS_add_meetups.sql  # NEW: meetups table + RLS policies

tests/
├── LoopMeet.Api.Tests/
│   └── MeetupsEndpointsTests.cs        # NEW: Integration tests
└── LoopMeet.App.Tests/
    ├── CreateMeetupViewModelTests.cs   # NEW: ViewModel unit tests
    └── HomeViewModelTests.cs           # NEW: Updated home page tests
```

**Structure Decision**: Follows the existing Mobile + API architecture. New code lives in the established project structure: feature module in `App/Features/Meetups/`, services in `Api/Services/Meetups/`, entity in `Core/Models/`, repository in `Infrastructure/Repositories/`. No new projects are needed.

## Complexity Tracking

> No constitution violations. All patterns follow existing conventions.
