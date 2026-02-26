# Implementation Plan: Home Tab Navigation Split

**Branch**: `002-split-home-tabbar` | **Date**: 2026-02-26 | **Spec**: [specs/002-split-home-tabbar/spec.md](specs/002-split-home-tabbar/spec.md)
**Input**: Feature specification from `/specs/002-split-home-tabbar/spec.md` plus user planning note to use the existing tech stack and include tab icons + text (icon above text), with emoji preferred over GIF where platform behavior is more reliable.

## Summary

Introduce a signed-in tab bar with a new placeholder Home tab, move group browsing into a dedicated Groups tab, and move pending invitation actions into a dedicated Invitations tab. Implement the change within the current .NET MAUI Shell app structure, preserving existing API contracts and group/invitation behaviors while improving navigation clarity and discoverability. Use tab icons with text labels on each tab; prefer emoji-style icons rendered reliably by the platform (or static emoji-like assets) rather than animated GIF tab icons.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core Web API (existing backend unchanged for this feature)  
**Storage**: N/A for this feature (no new persistence or schema changes; existing group/invitation data sources are reused)  
**Testing**: xUnit (existing test suites), MAUI app ViewModel/unit tests for split-screen logic (new or expanded app-focused test project), manual MAUI navigation smoke test for tab rendering on target devices/simulators  
**Target Platform**: .NET MAUI mobile app (Android, iOS; MacCatalyst supported in current project) with existing LoopMeet API backend  
**Project Type**: mobile + API (UI-focused change in mobile app)  
**Performance Goals**: Tab switching feels immediate (<250ms perceived transition); no regression in existing groups/invitations loading behavior for typical user datasets; first signed-in screen remains responsive while data tabs load on demand  
**Constraints**: Use existing stack and app architecture; no backend schema or auth-flow changes; tab bar must show both icon and text with icon visually above text; prefer emoji icon styling over GIF due native tab bar animation limitations; preserve existing group detail and invitation accept/decline flows  
**Scale/Scope**: 1 new signed-in placeholder screen, 1 new invitations list screen, 1 refactor of current groups list screen to remove invitation content, AppShell tab navigation update, DI registrations/routes updates, no net-new backend endpoints

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Privacy & Safety by Default: No new user data is collected; existing group/invitation visibility and acceptance controls remain unchanged and are carried forward in the split UI design.
- [x] Independent, Testable User Stories: Spec defines independent Home/Groups/Invitations stories; plan includes unit and manual smoke validation for each tab flow.
- [x] Contract-First Interfaces: Phase 1 will produce contracts for the app-consumed endpoints and the signed-in tab navigation configuration; no backend version bump required because endpoints are reused unchanged.
- [x] Observability & Reliability: Plan preserves existing logging/error handling for group/invitation loads and adds explicit guidance for tab-level empty/error states and reload behavior.
- [x] Simplicity & Incremental Delivery: Use MAUI Shell `TabBar` and existing services/view models where possible; avoid custom tab bar implementation unless a platform limitation blocks icon+label requirements.

Re-check after Phase 1 design: all gates pass. Design artifacts keep the change UI-scoped, preserve privacy defaults, define contracts before implementation, and avoid unnecessary backend changes.

## Project Structure

### Documentation (this feature)

```text
specs/002-split-home-tabbar/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── LoopMeet.App/
│   ├── AppShell.xaml
│   ├── AppShell.xaml.cs
│   ├── MauiProgram.cs
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Groups/
│   │   │   ├── Models/
│   │   │   ├── ViewModels/
│   │   │   └── Views/
│   │   ├── Home/
│   │   │   ├── ViewModels/      # new for placeholder home tab (planned)
│   │   │   └── Views/           # new for placeholder home tab (planned)
│   │   ├── Invitations/
│   │   │   ├── Models/
│   │   │   ├── ViewModels/
│   │   │   └── Views/
│   │   └── Profile/
│   ├── Resources/
│   │   ├── Images/              # tab icon assets if emoji-rendered images are used
│   │   └── Styles/
│   └── Services/
├── LoopMeet.Api/                # existing endpoints/services reused; no planned code changes
├── LoopMeet.Core/
└── LoopMeet.Infrastructure/

tests/
├── LoopMeet.Api.Tests/
├── LoopMeet.Core.Tests/
├── LoopMeet.Infrastructure.Tests/
└── LoopMeet.App.Tests/          # planned if needed for ViewModel/navigation logic unit tests
```

**Structure Decision**: Keep the existing mobile + API repository structure. Implement the feature entirely in `src/LoopMeet.App` by adding a Home tab feature module, extracting a dedicated invitations list screen under `Features/Invitations`, and simplifying `Features/Groups` to group-only content. Backend projects remain unchanged.

## Phase 0: Outline & Research

- Confirm MAUI Shell tab bar configuration patterns for signed-in navigation with separate Home/Groups/Invitations tabs.
- Validate tab icon approach that satisfies "icon above text" and "emoji preferred" without relying on unreliable animated GIF behavior in native tab bars.
- Choose the simplest view-model split strategy for reusing existing groups and invitation actions while minimizing regression risk.
- Confirm contract scope for a UI-only feature (reused API endpoints + explicit navigation/tab configuration contract).

Output: [specs/002-split-home-tabbar/research.md](specs/002-split-home-tabbar/research.md)

## Phase 1: Design & Contracts

- Define UI-facing entities/state models for tab navigation, home placeholder content, groups screen state, and pending invitations screen state.
- Produce API contract(s) for endpoints consumed by the split tabs (groups list/detail, invitations list, accept/decline) and a UI navigation contract for tab metadata (title + icon).
- Document quickstart steps for manual verification of the signed-in tab experience and invitation/group flows after the split.
- Update agent context from the completed plan so project metadata stays synchronized.

Outputs:
- [specs/002-split-home-tabbar/data-model.md](specs/002-split-home-tabbar/data-model.md)
- [specs/002-split-home-tabbar/contracts/home-navigation-api.yaml](specs/002-split-home-tabbar/contracts/home-navigation-api.yaml)
- [specs/002-split-home-tabbar/contracts/tabbar-navigation.yaml](specs/002-split-home-tabbar/contracts/tabbar-navigation.yaml)
- [specs/002-split-home-tabbar/quickstart.md](specs/002-split-home-tabbar/quickstart.md)

## Phase 2: Task Planning Approach (Stop Point for `/speckit.plan`)

- Organize tasks by user story (Home tab, Groups tab split, Invitations tab split) so each slice remains independently testable.
- Include shared refactor tasks for AppShell routing/tab setup and DI registrations.
- Add test tasks for view-model behavior and tab navigation smoke validation.
- Include explicit regression tasks for invitation accept/decline and group detail navigation.

## UX Review Checkpoint

- Verify each tab shows both an icon and a text label, with the icon visually above the text in the native tab bar presentation.
- Confirm Home tab placeholder content is clearly labeled as future functionality and does not look broken or empty by mistake.
- Confirm Groups tab excludes pending invitation rows and keeps group actions easy to find.
- Confirm Invitations tab keeps invitation response actions discoverable on phone and desktop idioms.

## Testing Strategy

- ViewModel unit tests (or equivalent app-logic tests) for split loading state, empty-state behavior, and invitation list refresh after accept/decline.
- Manual MAUI smoke tests on Android and iOS simulators/devices for tab icon/title rendering, default selected tab, and navigation between tabs.
- Regression checks for existing flows: group detail navigation, create group entry point, invitation detail, accept, and decline actions.
- API contract review only (no backend implementation changes planned); ensure UI relies on existing endpoints without schema assumptions beyond documented contracts.

## Observability & Reliability

- Preserve structured app logs in groups/invitations view models and add clear log messages for tab-specific loads and invitation action refreshes.
- Maintain current user-facing error handling for API unavailable/unauthorized states; ensure the tab split does not hide these states behind blank screens.
- Ensure invitation accept/decline completion triggers deterministic UI refresh on the Invitations tab and does not require app restart or re-login.

## Complexity Tracking

No constitution violations requiring justification.
