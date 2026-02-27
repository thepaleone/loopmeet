# Implementation Plan: Profile Tab and Profile Management

**Branch**: `003-profile-settings-tab` | **Date**: 2026-02-26 | **Spec**: [specs/003-profile-settings-tab/spec.md](specs/003-profile-settings-tab/spec.md)
**Input**: Feature specification from `/specs/003-profile-settings-tab/spec.md`

## Summary

Add a new `Profile` tab as the last signed-in tab, deliver a profile screen for viewing and editing display name/avatar, present password changes in a dedicated modal flow, and extend profile provisioning so social avatar values populate new profiles only when a user override is not established. The implementation spans MAUI app navigation/UI, profile API contracts, profile persistence shape, and targeted app/API test updates for all changed behavior.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: .NET MAUI (Shell/XAML), CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Microsoft.Extensions.Logging, ASP.NET Core minimal APIs, Supabase client SDKs  
**Storage**: Supabase Postgres (`user_profiles`, `memberships`) with RLS; additive migration required for avatar override/source metadata  
**Testing**: xUnit (`tests/LoopMeet.App.Tests`, `tests/LoopMeet.Api.Tests`, plus regression coverage updates for changed profile/auth flows)  
**Target Platform**: .NET MAUI mobile app (Android, iOS, MacCatalyst target in project) + LoopMeet API backend  
**Project Type**: mobile + API  
**Performance Goals**: Profile tab first render under 1 second on warm app state; profile save actions complete with user feedback under 2 seconds under normal network conditions  
**Constraints**: `Profile` tab remains last in tab bar; password updates must occur in a dedicated modal (no inline password fields); user avatar override always takes precedence over social avatar; changed/new functionality must include corresponding automated test updates  
**Scale/Scope**: 1 new signed-in tab + profile screen, 1 password modal workflow, user profile API expansion, one additive DB migration for profile avatar metadata, app and API regression test additions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Privacy & Safety by Default: Profile edits are self-service only for authenticated user identity; avatar source precedence is explicit; password-change flow is isolated and non-inline.
- [x] Independent, Testable User Stories: Each story remains independently testable (tab discovery/view, profile edits, password modal, social avatar copy rules) with explicit planned test coverage.
- [x] Contract-First Interfaces: Phase 1 defines profile management API and tab/navigation contracts before implementation; migration and additive contract changes are documented.
- [x] Observability & Reliability: Existing structured logging patterns are retained for profile fetch/update and password change operations; clear user-facing error behavior is planned for failed save/change actions.
- [x] Simplicity & Incremental Delivery: Reuses existing Shell navigation, auth/profile services, and repository patterns; changes are additive and scoped to profile capabilities.

Re-check after Phase 1 design: all gates pass. Design artifacts keep privacy defaults, define contracts/migration upfront, preserve reliability expectations, and include required test updates for new/changed behavior.

## Project Structure

### Documentation (this feature)

```text
specs/003-profile-settings-tab/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── profile-management-api.yaml
│   └── profile-tab-navigation.yaml
└── tasks.md              # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/
├── LoopMeet.App/
│   ├── AppShell.xaml
│   ├── MauiProgram.cs
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Groups/
│   │   ├── Home/
│   │   ├── Invitations/
│   │   └── Profile/                 # new module for this feature
│   └── Services/
├── LoopMeet.Api/
│   ├── Endpoints/
│   ├── Contracts/
│   └── Services/
├── LoopMeet.Core/
│   ├── Interfaces/
│   └── Models/
└── LoopMeet.Infrastructure/
    ├── Repositories/
    └── Supabase/Models/

tests/
├── LoopMeet.App.Tests/
├── LoopMeet.Api.Tests/
├── LoopMeet.Core.Tests/
└── LoopMeet.Infrastructure.Tests/

supabase/
└── migrations/
```

**Structure Decision**: Keep the current mobile + API monorepo shape. Add profile-specific UI/view-model code under `src/LoopMeet.App/Features/Profile`, expand existing user/profile contracts/services in `src/LoopMeet.Api`, and add one additive migration under `supabase/migrations` for profile avatar override/source columns.

## Phase 0: Outline & Research

- Confirm tab placement and routing approach for appending `Profile` as the last signed-in tab without regressing existing tab routes.
- Confirm avatar precedence model and minimal persistence shape needed to enforce: user override > social avatar > none.
- Confirm password-change interface strategy (modal UX + backend/API contract) and validation/error semantics.
- Confirm profile projection strategy for `User since` and group membership count.
- Confirm mandatory automated test updates for all changed/new behavior per constitution and user request.

Output: [specs/003-profile-settings-tab/research.md](specs/003-profile-settings-tab/research.md)

## Phase 1: Design & Contracts

- Define profile-centric entities (profile aggregate, avatar source state, password change request, profile summary projection).
- Define REST contracts for profile read/update and password change operations plus profile tab/navigation behavior contract.
- Define migration notes for additive profile fields supporting avatar override and social avatar copy behavior.
- Define implementation quickstart and validation flow, including required automated test execution/update commands.
- Update agent context via `.specify/scripts/bash/update-agent-context.sh codex`.

Outputs:
- [specs/003-profile-settings-tab/data-model.md](specs/003-profile-settings-tab/data-model.md)
- [specs/003-profile-settings-tab/contracts/profile-management-api.yaml](specs/003-profile-settings-tab/contracts/profile-management-api.yaml)
- [specs/003-profile-settings-tab/contracts/profile-tab-navigation.yaml](specs/003-profile-settings-tab/contracts/profile-tab-navigation.yaml)
- [specs/003-profile-settings-tab/quickstart.md](specs/003-profile-settings-tab/quickstart.md)

## Phase 2: Task Planning Approach (Stop Point for `/speckit.plan`)

- Organize tasks by user story so each slice remains independently deliverable:
  - Add `Profile` tab and profile view projection
  - Edit display name/avatar with override precedence
  - Password modal + change-password flow
  - Social avatar copy behavior during profile bootstrap/create
- Add explicit tasks for additive migration and contract updates before implementation.
- Add app/API test tasks for each changed behavior and regression path.
- Include verification tasks for edge cases (no avatar, no groups, failed password change, override precedence).

## UX Review Checkpoint

- Validate `Profile` tab is visually and functionally the final tab in signed-in tab order.
- Validate profile page clearly presents: display name, avatar state, `User since`, and group count.
- Validate password changes are initiated and completed in a dedicated popup modal, never inline.
- Validate edit/save confirmations and error states are clear and actionable.

## Testing Strategy

- Add or update app tests for profile view-model logic, including display name save, avatar override save semantics, and profile projection state.
- Add or update API endpoint tests for profile response fields (`userSince`, `groupCount`, avatar source/effective avatar) and password change responses.
- Add regression tests for social-login bootstrap behavior to verify social avatar is copied only when no override exists.
- Run and keep passing at minimum:
  - `dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.App.Tests`
  - `dotnet test /Users/joel/projects/palehorse/loopmeet/tests/LoopMeet.Api.Tests`
- Enforce rule for this feature: any new or changed functionality must include matching automated test updates before merge.

## Observability & Reliability

- Preserve structured logs for profile read/update and extend logs for avatar source decisions and password-change success/failure.
- Return actionable validation errors for profile edits and password changes; keep users on the current flow with clear recovery messaging.
- Ensure profile data load failures do not crash navigation; show deterministic error state and retry path.
- Ensure password modal failure states do not lose unsaved display name/avatar edits on the underlying profile screen.

## Complexity Tracking

No constitution violations requiring justification.
