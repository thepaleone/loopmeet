# Tasks: Push Notification Flows

**Input**: Design documents from `/specs/008-push-notifications/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Include automated tests per constitution and plan requirements.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize project scaffolding for Supabase functions, migrations, and MAUI notification module.

- [X] T001 Create Supabase notifications function folders and config in `supabase/functions/notifications-dispatch/deno.json` and `supabase/functions/reminders-scheduler/deno.json`
- [X] T002 [P] Add OneSignal and notification configuration placeholders in `src/LoopMeet.App/Services/AppConfig.cs`
- [X] T003 [P] Add push-notification environment variable documentation in `specs/008-push-notifications/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core schema, contracts, and shared services required before user story implementation.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create notification schema migration for `user_devices`, `notification_events`, `notification_delivery_attempts`, and `notification_open_events` in `supabase/migrations/20260430_notifications_schema.sql`
- [X] T005 [P] Add RLS policies and indexes for notification tables in `supabase/migrations/20260430_notifications_policies.sql`
- [X] T006 [P] Implement shared notification type constants and payload model in `supabase/functions/_shared/notification-contract.ts`
- [X] T007 [P] Implement OneSignal REST client wrapper with retry and error mapping in `supabase/functions/_shared/onesignal-client.ts`
- [X] T008 Create backend notification audit repository for send/open events in `src/LoopMeet.Api/Services/Notifications/NotificationAuditService.cs`
- [X] T009 Create MAUI pending notification intent store for signed-out redirects in `src/LoopMeet.App/Services/Notifications/PendingNotificationIntentStore.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Receive and Open Actionable Notifications (Priority: P1) 🎯 MVP

**Goal**: Deliver all five notification types with correct recipient targeting and tap-to-destination routing.

**Independent Test**: Trigger each notification type (invitation/new meetup/update/cancel/today reminder) and verify delivery plus correct navigation destination from notification tap.

### Tests for User Story 1

- [X] T010 [P] [US1] Add contract test for canonical `additional_data` keys in `tests/LoopMeet.Api.Tests/Contract/NotificationPayloadContractTests.cs`
- [X] T011 [P] [US1] Add integration tests for webhook event-to-notification-type mapping in `tests/LoopMeet.Api.Tests/Integration/WebhookNotificationMappingTests.cs`
- [X] T012 [P] [US1] Add MAUI navigation resolver tests for notification type destinations in `tests/LoopMeet.App.Tests/Services/Notifications/NotificationNavigatorTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Implement Supabase webhook dispatcher entrypoint for invitations and meetups in `supabase/functions/notifications-dispatch/index.ts`
- [X] T014 [P] [US1] Implement recipient resolution queries for invitation and group membership targeting in `supabase/functions/notifications-dispatch/recipient-resolver.ts`
- [X] T015 [P] [US1] Implement centralized notification destination mapping in `supabase/functions/notifications-dispatch/destination-map.ts`
- [X] T016 [US1] Implement payload builder using canonical contract keys in `supabase/functions/notifications-dispatch/payload-builder.ts`
- [X] T017 [US1] Implement send orchestration with idempotency guard and OneSignal dispatch in `supabase/functions/notifications-dispatch/dispatch-service.ts`
- [X] T018 [US1] Configure webhook handlers for `public.invitations` and `public.meetups` events in `supabase/functions/notifications-dispatch/webhook-router.ts`
- [X] T019 [US1] Add reminder scheduler query and queue logic for `meetup.today_reminder` in `supabase/functions/reminders-scheduler/index.ts`
- [X] T020 [US1] Implement MAUI `NotificationService` notification-open parser and route handoff in `src/LoopMeet.App/Services/Notifications/NotificationService.cs`
- [X] T021 [US1] Implement MAUI destination navigator for Pending Invitations, Group Detail, and Home routes in `src/LoopMeet.App/Services/Notifications/NotificationNavigator.cs`
- [X] T022 [US1] Wire OneSignal opened callback registration and service initialization in `src/LoopMeet.App/MauiProgram.cs`
- [X] T023 [US1] Add invalid destination fallback message flow in `src/LoopMeet.App/Services/Notifications/NotificationService.cs`

**Checkpoint**: User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Manage Notification Permission First Run (Priority: P2)

**Goal**: Request notification permission at the right moment, preserve app usability on deny, and provide settings recovery path.

**Independent Test**: Fresh install path validates permission prompt timing (post sign-in/relevant action), deny path shows status and settings link, granted path avoids repeated prompts.

### Tests for User Story 2

- [X] T024 [P] [US2] Add permission workflow tests for prompt timing and re-prompt prevention in `tests/LoopMeet.App.Tests/Services/Notifications/NotificationPermissionServiceTests.cs`
- [X] T025 [P] [US2] Add integration test for denied-permission settings recovery CTA in `tests/LoopMeet.App.Tests/Features/Settings/NotificationSettingsCtaTests.cs`

### Implementation for User Story 2

- [X] T026 [US2] Implement MAUI permission-state manager (`unknown/granted/denied`) in `src/LoopMeet.App/Services/Notifications/NotificationPermissionService.cs`
- [X] T027 [US2] Trigger first permission request after sign-in or first notification-relevant action in `src/LoopMeet.App/Services/Auth/AuthSessionService.cs`
- [X] T028 [US2] Add in-app disabled-notifications status and settings deep link in `src/LoopMeet.App/Features/Profile/Views/SettingsPage.xaml`
- [X] T029 [US2] Implement OS settings launcher for notification recovery CTA in `src/LoopMeet.App/Services/Notifications/NotificationSettingsLauncher.cs`
- [X] T030 [US2] Persist permission state updates to `user_devices` sync endpoint in `src/LoopMeet.App/Services/Notifications/DeviceRegistrationService.cs`

**Checkpoint**: User Story 2 works independently and does not block core app flows.

---

## Phase 5: User Story 3 - Extensible Notification Catalog (Priority: P3)

**Goal**: Make notification type and route mapping easy to extend without breaking existing notification behavior.

**Independent Test**: Add a mock notification type in mapping, verify contract validation passes, and existing five types remain unchanged.

### Tests for User Story 3

- [X] T031 [P] [US3] Add contract parity test for Edge mapping vs MAUI mapping in `tests/LoopMeet.Api.Tests/Contract/NotificationMappingParityTests.cs`
- [X] T032 [P] [US3] Add regression test ensuring existing notification types resolve unchanged in `tests/LoopMeet.App.Tests/Services/Notifications/NotificationTypeRegressionTests.cs`

### Implementation for User Story 3

- [X] T033 [US3] Extract notification mapping registry with explicit versioning notes in `supabase/functions/_shared/notification-mapping-registry.ts`
- [X] T034 [US3] Refactor MAUI route resolution to consume central mapping abstraction in `src/LoopMeet.App/Services/Notifications/NotificationRouteMap.cs`
- [X] T035 [US3] Document process for adding new notification types in `specs/008-push-notifications/contracts/notification-payload-contract.md`

**Checkpoint**: User Story 3 is independently functional and extension-safe.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Reliability hardening, observability, and end-to-end validation across all stories.

- [X] T036 [P] Add structured logs and correlation IDs for dispatch and open events in `supabase/functions/notifications-dispatch/dispatch-service.ts`
- [X] T037 [P] Implement stale-device cleanup job and invalid-token handling in `supabase/functions/reminders-scheduler/stale-device-cleanup.ts`
- [X] T038 Add signed-out post-login redirect completion flow in `src/LoopMeet.App/Services/Notifications/PostLoginNotificationRedirectService.cs`
- [X] T039 Add end-to-end test checklist and manual validation matrix in `specs/008-push-notifications/quickstart.md`
- [X] T040 Run full notification validation for all five types and record results in `specs/008-push-notifications/research.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Stories (Phases 3-5)**: Depend on Foundational completion.
- **Polish (Phase 6)**: Depends on completion of desired user stories.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2 and is MVP.
- **US2 (P2)**: Starts after Phase 2; may reuse US1 notification service but remains independently testable.
- **US3 (P3)**: Starts after Phase 2; validates extension model and regression safety.

### Within Each User Story

- Tests first, then implementation.
- Contract and mapping tasks before dispatch/navigation wiring.
- Complete story-level validation before moving to next priority for release.

### Parallel Opportunities

- Phase 1: T002 and T003 in parallel.
- Phase 2: T005, T006, and T007 in parallel after T004 starts.
- US1: T010, T011, T012 parallel; T014 and T015 parallel; client tasks T020 and T021 parallel.
- US2: T024 and T025 parallel; T028 and T029 parallel.
- US3: T031 and T032 parallel.

---

## Parallel Example: User Story 1

```bash
# Run US1 tests in parallel:
Task: "T010 [US1] tests/LoopMeet.Api.Tests/Contract/NotificationPayloadContractTests.cs"
Task: "T011 [US1] tests/LoopMeet.Api.Tests/Integration/WebhookNotificationMappingTests.cs"
Task: "T012 [US1] tests/LoopMeet.App.Tests/Services/Notifications/NotificationNavigatorTests.cs"

# Build mapping and recipient components in parallel:
Task: "T014 [US1] supabase/functions/notifications-dispatch/recipient-resolver.ts"
Task: "T015 [US1] supabase/functions/notifications-dispatch/destination-map.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate all five notification types and destinations.
4. Demo MVP push flow.

### Incremental Delivery

1. Deliver US1 (core notifications + deep linking).
2. Deliver US2 (permission lifecycle + settings recovery).
3. Deliver US3 (extensibility and mapping safeguards).
4. Finish Phase 6 reliability and observability hardening.

### Parallel Team Strategy

1. Team A: Backend/Supabase tasks (T013-T019, T036-T037).
2. Team B: MAUI notification navigation and permission tasks (T020-T030, T038).
3. Team C: Test/contract parity tasks (T010-T012, T024-T025, T031-T032, T040).

---

## Notes

- [P] tasks are safe for parallel execution because they target different files.
- Story labels map directly to spec user stories for independent verification.
- Sensitive credential files must remain untracked and excluded from commits.
