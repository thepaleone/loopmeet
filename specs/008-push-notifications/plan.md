# Implementation Plan: Push Notification Flows

**Branch**: `[008-push-notifications]` | **Date**: 2026-04-30 | **Spec**: `specs/008-push-notifications/spec.md`
**Input**: Feature specification from `/specs/008-push-notifications/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Implement end-to-end push notifications for invitations and meetup lifecycle events, including tap-to-destination deep linking, permission UX, signed-out redirect handling, and scheduled local-morning reminders. The plan uses Supabase Postgres + webhooks + Edge Functions for orchestration, OneSignal for delivery keyed by `external_id = auth.users.id`, and a MAUI notification module for permission, login binding, and navigation routing.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# 13 / .NET 10 (MAUI client), TypeScript (Deno runtime in Supabase Edge Functions), SQL (PostgreSQL)  
**Primary Dependencies**: OneSignalSDK.DotNet, Supabase Auth + Postgres + Database Webhooks + Edge Functions, OneSignal REST API  
**Storage**: Supabase Postgres (`user_devices`, existing groups/meetups/invitations domain tables, notification audit tables)  
**Testing**: .NET unit/integration tests, API integration tests for webhook/edge flow, manual device validation for push open behavior  
**Target Platform**: iOS + Android MAUI clients, Supabase-hosted backend services
**Project Type**: Mobile app + backend orchestration + third-party push integration  
**Performance Goals**: 98% send-attempt generation within 2 minutes of trigger event; 95% opened pushes navigate correctly first attempt  
**Constraints**: Permission prompt after sign-in/relevant action, canonical payload schema keys fixed by spec, 8:00-10:00 local reminder window, signed-out redirect preserved  
**Scale/Scope**: Initial five notification types, multiple active devices per user, feature-level rollout for existing authenticated user base

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Gate 1 - Code Quality Is Non-Negotiable**: PASS. Plan keeps routing/payload contracts centralized and modular (`NotificationDestinationMapping`, `NotificationService`, Edge dispatcher).
- **Gate 2 - Tests Are a Required Deliverable**: PASS. Plan includes contract tests for payload mapping, integration tests for webhook-to-OneSignal dispatch, and client navigation/permission tests.
- **Gate 3 - User Experience Comes First**: PASS. Permission UX timing, denied-state settings recovery, fallback routing, and signed-out post-login redirect are explicitly defined.
- **Gate 4 - Simplicity Over Cleverness**: PASS. Uses straightforward DB webhooks + single dispatcher function + typed payload contract instead of premature event bus complexity.
- **Gate 5 - Modularity Over Monolithic Design**: PASS. Backend orchestration, payload mapping, and MAUI client handling are split into cohesive components with explicit interfaces.
- **Gate 6 - Contract-First Interfaces**: PASS. OneSignal payload schema, webhook event contracts, and navigation contract are defined in `contracts/` before implementation.
- **Gate 7 - Observability & Reliability**: PASS. Structured logs, idempotency keying, retry/failure capture, and stale-token cleanup are included.

## Project Structure

### Documentation (this feature)

```text
specs/008-push-notifications/
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
├── LoopMeet.Api/
├── LoopMeet.App/
├── LoopMeet.Core/
└── LoopMeet.Infrastructure/

tests/
├── LoopMeet.Api.Tests/
├── LoopMeet.App.Tests/
└── LoopMeet.Core.Tests/

supabase/
├── migrations/
├── functions/notifications-dispatch/
└── functions/reminders-scheduler/
```

**Structure Decision**: Use existing multi-project .NET structure plus a `supabase/` workspace for SQL migrations and Edge Functions. Keep push orchestration in backend modules and client navigation handling in `LoopMeet.App` notification services.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
