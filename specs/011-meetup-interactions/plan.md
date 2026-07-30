# Implementation Plan: Meetup Interaction Improvements

**Branch**: `011-meetup-interactions` | **Date**: 2026-07-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-meetup-interactions/spec.md`

## Summary

Three UI slices (icon save on the meetup forms, a read-only meetup details screen, owner-gated edit access) plus the backend denormalization that makes the details screen self-sufficient.

Per the requested approach, the two data gaps found during spec review are closed by **extending the read contracts server-side** rather than by extra client calls or navigation-parameter threading:

1. `CreatedByDisplayName` — resolved in `MeetupQueryService` from `CreatedByUserId` against the meetup group's current membership, using the same membership → `IUserRepository.ListByIdsAsync` → `DisplayName` flow as `GroupQueryService`.
2. `GroupName` — now populated on the group-scoped list as well as the home/upcoming list.

**A third field is required by the same reasoning and is included: `GroupOwnerUserId`.** FR-015/FR-016 require the edit affordance to be gated on group ownership *independently of entry point*, but nothing in the meetup data identifies a group's owner. From the Home entry point the client would need one `GET /groups/{id}` per distinct group to learn ownership — precisely the extra-client-call pattern this direction rules out. The owner id comes free: the group rows are already being fetched to supply `GroupName`. See [research.md](./research.md) §D3.

Both list endpoints converge on one read contract (`MeetupListItemResponse`), replacing the near-duplicate `UpcomingMeetupResponse` — no net type growth, one shape for the client's single `MeetupSummary` model, and no lying fields on the command echo (`MeetupResponse` stays unchanged; both create/edit view models already discard it).

The details screen loads from `GET /groups/{groupId}/meetups` given only `groupId` + `meetupId`, so it works from either entry point and from a future deep link. That endpoint's cache (`meetups:{groupId}`) is already invalidated on meetup create/update/delete, which is what satisfies FR-012 (fresh values after returning from an edit).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (MAUI client + minimal-API backend). No database schema changes.
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client). No new packages.
**Storage**: Existing Supabase tables, unchanged. `meetups.created_by_user_id` already exists; display name and group name are resolved at read time, never stored on the meetup.
**Testing**: xUnit. Api: `WebApplicationFactory<Program>` + in-memory repository fakes (`tests/LoopMeet.Api.Tests/Infrastructure/InMemoryStore.cs`). App: pure unit tests for the organizer-text helper, plus source-inspection tests for XAML/wiring (established repo pattern), plus the [quickstart.md](./quickstart.md) device matrix for native map-launch and keyboard behavior.
**Target Platform**: iOS, MacCatalyst, Android, Windows (uniform behavior).
**Project Type**: Mobile app + minimal-API backend (existing layered structure).
**Performance Goals**: Details screen shows information within 1 s under normal connectivity (SC-007). Organizer/group resolution adds a fixed 3 lookups per list request (groups, memberships, users) regardless of result count — no per-row query.
**Constraints**: No extra client-side round trip to resolve organizer, group name, or ownership; details screen must be loadable from `(groupId, meetupId)` alone; existing 30-second read caches and their invalidation points stay as they are.
**Scale/Scope**: 2 API contracts changed, 1 query service extended, 1 repository signature simplified, 1 membership batch method added; 2 XAML forms restructured, 2 card templates changed, 1 new page + view model; ~20 tests.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0 — before Phase 0 and re-checked after Phase 1.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | Net simplification: `UpcomingMeetupResponse` is removed rather than a third DTO added, and `ListUpcomingByUserAsync` loses its redundant group-name lookup (the query service already fetches those rows for `GroupOwnerUserId`). No commented-out code or TODOs introduced. |
| G2 | II. Tests Required | PASS | New behavior is testable at the layer that owns it: organizer/group/owner resolution via Api endpoint tests (including the creator-left-the-group case), the FR-011 fallback text via a pure client helper unit test, and XAML/wiring via source-inspection tests. Native map launch and keyboard-overlap behavior are covered by the quickstart matrix — the documented approach for platform-only behavior. |
| G3 | III. UX First | PASS | Spec carries 3 prioritized stories with acceptance scenarios; this feature exists to remove a keyboard-overlap failure and a dead tap target. Not-found and unresolvable-organizer states have defined, humane presentations rather than blank fields. |
| G4 | IV. Simplicity | PASS | One new repository method (`ListMembersByGroupsAsync`), justified by two consumers (both meetup list paths). Organizer resolution is a private helper used twice inside `MeetupQueryService`, not a new abstraction. No new packages, projects, or caching mechanism. |
| G5 | V. Modularity | PASS | Read denormalization stays in the query service (the layer that already owns read models); the command service is untouched, so write paths gain no display concerns. The client's details view model depends only on the existing `MeetupsApi` + `GroupsApi`-free surface. |
| G6 | VI. Contract-First | PASS | [contracts/meetup-read-contracts.md](./contracts/meetup-read-contracts.md) fixes the response shapes, resolution rules, and client model changes before implementation. |
| G7 | VII. Observability | PASS | Existing structured logs on both list paths are extended with resolution counts (how many organizers resolved vs. fell back), so an unexpected fallback rate is visible rather than silent. |

**Post-Phase-1 re-check (2026-07-30)**: All gates still PASS. Design added one repository method and one DTO while deleting another; no complexity exceptions required.

## Project Structure

### Documentation (this feature)

```text
specs/011-meetup-interactions/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions D1..D9
├── data-model.md        # Phase 1 — read-model fields, resolution rules, states
├── quickstart.md        # Phase 1 — device validation matrix
├── checklists/
│   └── requirements.md  # Spec quality checklist (complete)
├── contracts/
│   └── meetup-read-contracts.md   # Phase 1
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
src/LoopMeet.Core/
├── Interfaces/IMembershipRepository.cs        # MODIFIED: + ListMembersByGroupsAsync
└── Interfaces/IMeetupRepository.cs            # MODIFIED: ListUpcomingByUserAsync returns meetups only

src/LoopMeet.Infrastructure/Repositories/
├── MembershipRepository.cs                    # MODIFIED: batch membership query
└── MeetupRepository.cs                        # MODIFIED: drop redundant group-name lookup

src/LoopMeet.Api/
├── Contracts/MeetupContracts.cs               # MODIFIED: + MeetupListItemResponse
│                                              #   (replaces UpcomingMeetupResponse);
│                                              #   MeetupResponse unchanged
└── Services/Meetups/MeetupQueryService.cs     # MODIFIED: resolve group name, owner id,
                                               #   organizer display name; + IGroupRepository,
                                               #   IMembershipRepository, IUserRepository

src/LoopMeet.App/
├── Features/Meetups/
│   ├── Models/MeetupModels.cs                 # MODIFIED: + CreatedByDisplayName,
│   │                                          #   GroupOwnerUserId, OrganizerDisplay
│   ├── MeetupOrganizerText.cs                 # NEW: pure FR-011 fallback formatter
│   ├── ViewModels/
│   │   ├── MeetupDetailViewModel.cs           # NEW: load by (groupId, meetupId), IsOwner,
│   │   │                                      #   open-in-maps, not-found state
│   │   ├── CreateMeetupViewModel.cs           # unchanged (save command already exists)
│   │   └── EditMeetupViewModel.cs             # unchanged
│   └── Views/
│       ├── MeetupDetailPage.xaml(.cs)         # NEW: read-only details, owner-only pencil
│       ├── CreateMeetupPage.xaml              # MODIFIED: title row + icon save; bottom button removed
│       └── EditMeetupPage.xaml                # MODIFIED: same
├── Features/Home/
│   ├── Views/HomePage.xaml                    # MODIFIED: card tap → details; map glyph
│   └── ViewModels/HomeViewModel.cs            # MODIFIED: + OpenMeetupDetailCommand
├── Features/Groups/
│   ├── Views/GroupDetailPage.xaml             # MODIFIED: card tap → details; map glyph
│   └── ViewModels/GroupDetailViewModel.cs     # MODIFIED: card tap opens details for all members
├── AppShell.xaml.cs                           # MODIFIED: + "meetup-detail" route
└── MauiProgram.cs                             # MODIFIED: register detail page + view model

tests/LoopMeet.Api.Tests/Endpoints/MeetupsEndpointsTests.cs   # MODIFIED: seed users; assert
                                                              #   new fields + fallback case
tests/LoopMeet.App.Tests/Features/Meetups/
├── MeetupOrganizerTextTests.cs                # NEW: FR-011 fallback unit tests
└── MeetupInteractionSurfaceTests.cs           # NEW: source-inspection wiring assertions
```

**Structure Decision**: Existing layered structure and MAUI feature folders, unchanged. The only structural addition is the `meetup-detail` route and its page/view model pair, following the established modal/detail-page convention (`Routing.RegisterRoute` in `AppShell.xaml.cs`, `Shell.Current.GoToAsync` with a parameter dictionary).

## Complexity Tracking

No constitution violations — table intentionally empty.
