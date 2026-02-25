# Implementation Plan: Auth & Groups MVP

**Branch**: `001-auth-groups-mvp` | **Date**: 2026-02-16 | **Spec**: [specs/001-auth-groups-mvp/spec.md](specs/001-auth-groups-mvp/spec.md)
**Input**: Feature specification from `/specs/001-auth-groups-mvp/spec.md`

## Summary

Deliver a .NET 10 MAUI mobile app and ASP.NET Core Web API that supports sign-in, group listing, group details, group creation/editing, and email-based invitations. Persist data in Supabase Postgres for production and use local Postgres for testing. Include a basic MAUI splash screen for iOS and Android that displays the text "LoopMeet".

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: .NET MAUI, ASP.NET Core Web API, EF Core + Npgsql, Supabase.Client, CommunityToolkit.Mvvm, CommunityToolkit.Maui, Refit, Polly, Serilog  
**Storage**: Postgres (local for tests/dev), Supabase Postgres (production)  
**Testing**: xUnit, FluentAssertions, Testcontainers for Postgres, ASP.NET Core integration testing  
**Target Platform**: iOS and Android (MAUI) + web API  
**Project Type**: mobile + API  
**Performance Goals**: p95 API responses under 200ms for list/detail; groups screen renders in under 1s for 100 groups  
**Constraints**: Online required for auth and group data; invitation-only joining; duplicate group names per owner blocked  
**Scale/Scope**: MVP with 4-6 screens and under 10 API endpoints

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Privacy & Safety by Default: data collection minimized; member visibility limited to group members
- [x] Independent, Testable User Stories: acceptance scenarios defined; testing strategy documented in this plan
- [x] Contract-First Interfaces: OpenAPI contract planned in Phase 1 with versioned endpoints
- [x] Observability & Reliability: structured logging and error handling included for API and app flows
- [x] Simplicity & Incremental Delivery: scope limited to auth, groups, and invitations with no offline support

Re-check after Phase 1 design: all gates pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-auth-groups-mvp/
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
│   ├── Features/
│   │   ├── Auth/
│   │   ├── Groups/
│   │   └── Invitations/
│   ├── Views/
│   ├── ViewModels/
│   └── Resources/
│       └── Splash/
├── LoopMeet.Api/
│   ├── Contracts/
│   ├── Endpoints/
│   ├── Services/
│   └── Program.cs
├── LoopMeet.Core/
│   ├── Models/
│   ├── Interfaces/
│   └── Validators/
└── LoopMeet.Infrastructure/
    ├── Data/
    ├── Repositories/
    └── Supabase/

tests/
├── LoopMeet.Api.Tests/
├── LoopMeet.Core.Tests/
└── LoopMeet.Infrastructure.Tests/
```

**Structure Decision**: Mobile + API with shared core models and infrastructure to keep domain logic consistent across app and API.

## Phase 0: Outline & Research

- Confirm .NET 10 MAUI capabilities for splash screen text-only branding on iOS/Android.
- Validate Supabase Auth + JWT flow for ASP.NET Core API authorization.
- Choose minimal, popular NuGet packages that reduce boilerplate without adding complexity.

Output: [specs/001-auth-groups-mvp/research.md](specs/001-auth-groups-mvp/research.md)

## Phase 1: Design & Contracts

- Define the data model for users, groups, memberships, invitations, and auth identities with validation rules.
- Produce OpenAPI contract for group listing, detail, create/edit, invitations list, and invitation acceptance.
- Define app UI flows for auth, groups list, group detail, group creation/edit, invitation list, and empty state.
- Add the MAUI splash screen asset and platform configuration showing "LoopMeet" on iOS and Android.

Outputs:
- [specs/001-auth-groups-mvp/data-model.md](specs/001-auth-groups-mvp/data-model.md)
- [specs/001-auth-groups-mvp/contracts/loopmeet-api.yaml](specs/001-auth-groups-mvp/contracts/loopmeet-api.yaml)
- [specs/001-auth-groups-mvp/quickstart.md](specs/001-auth-groups-mvp/quickstart.md)

## UX Review Checkpoint

- Review login, create account, groups list, group detail, and invitation flows for clarity and visual consistency.
- Record UX review outcomes and action items in this plan before implementation starts.

## Testing Strategy

- Unit tests for domain rules: group name uniqueness per owner, invitation acceptance, and role checks.
- API integration tests using Testcontainers Postgres to validate sorting, invitation flow, and error messages.
- Smoke tests for MAUI views to ensure splash screen and basic navigation render on both platforms.

## Observability & Reliability

- API logs: structured logs with request correlation IDs, auth errors, and invitation failures.
- App logs: capture auth failures and API error responses with user-friendly messages.
- Standard error responses in API contracts to keep client behavior deterministic.

## Complexity Tracking

No constitution violations requiring justification.
