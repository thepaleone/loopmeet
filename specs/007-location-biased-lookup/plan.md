# Implementation Plan: Location-Biased Lookup

**Branch**: `007-location-biased-lookup` | **Date**: 2026-04-28 | **Spec**: `/Users/joel/projects/palehorse/loopmeet/specs/007-location-biased-lookup/spec.md`
**Input**: Feature specification from `/specs/007-location-biased-lookup/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Improve meetup location autocomplete relevance on create/edit flows by biasing results to the user's current position when permission is granted, while preserving a reliable fallback when permission is unavailable. The plan reuses existing meetup form view models and places service contracts, and extends current behavior with permission-aware location biasing through Google Places capabilities already exposed by the backend proxy.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: .NET MAUI, CommunityToolkit.Mvvm, Refit, ASP.NET Core minimal APIs, Google Places (via existing backend proxy)  
**Storage**: N/A (no new persistence required)  
**Testing**: xUnit (.App.Tests and .Api.Tests)  
**Target Platform**: .NET MAUI mobile app (iOS/Android) + ASP.NET Core API
**Project Type**: Mobile app + web API  
**Performance Goals**: Maintain suggestion updates within 1 second per keystroke in normal network conditions; improve first-5 suggestion relevance for local queries  
**Constraints**: Must follow OS location permission rules; must continue working when permission denied/unavailable; reuse existing components and native Google functionality where available  
**Scale/Scope**: CreateMeetupPage and EditMeetupPage lookup behavior, plus `/places/autocomplete` request shaping

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Is Non-Negotiable**: PASS. Reuse existing view models/services; isolate new behavior in focused components to avoid duplication.
- **Tests Are a Required Deliverable**: PASS. Add/extend automated tests for permission state handling, fallback behavior, and autocomplete ranking inputs.
- **User Experience Comes First**: PASS. Keeps user in control of permission and ensures graceful fallback; acceptance scenarios already captured in spec.
- **Simplicity Over Cleverness**: PASS. Prefer incremental extension of existing APIs over introducing new modules unless needed.
- **Modularity Over Monolithic Design**: PASS. Keep lookup, permission, and request-shaping responsibilities in separate app/service boundaries.
- **Contract-First Interfaces**: PASS. Define updated autocomplete contract (optional location bias inputs) before implementation.
- **Observability & Reliability**: PASS. Preserve existing logging and add signals for bias-on/bias-off request outcomes.

## Project Structure

### Documentation (this feature)

```text
specs/007-location-biased-lookup/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/
├── LoopMeet.App/
│   ├── Features/Meetups/ViewModels/
│   ├── Features/Meetups/Views/
│   └── Services/
├── LoopMeet.Api/
│   ├── Endpoints/
│   └── Services/Places/

tests/
├── LoopMeet.App.Tests/
└── LoopMeet.Api.Tests/
```

**Structure Decision**: Use the existing mobile + API structure and extend existing meetup view models and places proxy paths. No new top-level modules are introduced.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

## Post-Design Constitution Check

- **Code Quality Is Non-Negotiable**: PASS. Design reuses existing components and keeps new behavior scoped to lookup and contract shaping.
- **Tests Are a Required Deliverable**: PASS. Plan includes explicit automated test targets across app and API layers.
- **User Experience Comes First**: PASS. Permission flow is user-driven with non-blocking fallback.
- **Simplicity Over Cleverness**: PASS. Uses provider-native biasing instead of custom ranking logic.
- **Modularity Over Monolithic Design**: PASS. No cross-cutting architectural changes; only targeted component extensions.
- **Contract-First Interfaces**: PASS. Contract artifact added before implementation.
- **Observability & Reliability**: PASS. Existing logging path remains in place; bias/fallback behavior is explicit in design.
