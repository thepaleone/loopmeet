# Research: Push Notification Flows

## Decision 1: Device registration model
- Decision: Maintain `user_devices` in Supabase keyed to `auth.users.id`, with one row per installation/player and multiple active devices allowed.
- Rationale: Matches clarified FR-018 and enables fan-out to all active devices without forcing single-device revocation.
- Alternatives considered: Single active device per user (rejected: conflicts with clarified behavior), no local table (rejected: weak preference/control and poor observability).

## Decision 2: OneSignal identity strategy
- Decision: Call `OneSignal.Default.Login(userId)` after Supabase auth success; use canonical user id string as OneSignal `external_id`.
- Rationale: Stable identity unifies delivery across installations and supports backend targeting by user.
- Alternatives considered: Anonymous per-device only (rejected: weak user-level targeting), custom alias mapping service (rejected: unnecessary complexity).

## Decision 3: Notification payload contract
- Decision: Enforce canonical `additional_data` keys: `notification_type`, `target_kind`, `target_id`, `fallback_route`, `event_id`, `sent_at`.
- Rationale: Contract-first routing with deterministic parsing and easy extension.
- Alternatives considered: Per-type schemas (rejected: brittle), minimal keys (rejected: more implicit app logic).

## Decision 4: Destination mapping location
- Decision: Keep base mapping in Edge Function config (typed object) and mirror map in MAUI route resolver; mapping values must be contract-tested.
- Rationale: Fast lookup, versioned with code, simple rollout.
- Alternatives considered: DB lookup table for mapping (deferred for future admin-editable routing feature).

## Decision 5: Event trigger orchestration
- Decision: Use Supabase Database Webhooks on invitations and meetups tables to invoke `notifications-dispatch` Edge Function; use scheduled Edge Function for daily reminders.
- Rationale: Direct event coupling, low-latency dispatch, centralized behavior in one orchestration layer.
- Alternatives considered: API-layer direct push calls (rejected: scatters logic), pure SQL trigger HTTP logic (rejected: harder maintainability).

## Decision 6: Reminder scheduling
- Decision: Run a Supabase scheduled job every 15 minutes that invokes `reminders-scheduler`, selecting same-day meetups and queuing sends for users currently in 8:00-10:00 local window.
- Rationale: Satisfies FR-016 local timing while avoiding per-timezone cron explosion.
- Alternatives considered: Single UTC blast (rejected: violates local window), per-user cron entries (rejected: unscalable).

## Decision 7: Permission UX implementation
- Decision: Prompt after first successful sign-in or first notification-relevant action; when denied, show in-app CTA to open OS settings.
- Rationale: Aligns with platform guidance and clarified FR-014/FR-015.
- Alternatives considered: Prompt at cold start (rejected: poor opt-in), no in-app recovery path (rejected: UX gap).

## Decision 8: Signed-out tap behavior
- Decision: Persist tapped notification intent in transient secure local state and execute post-login redirect once auth is restored.
- Rationale: Required by FR-017 and keeps user intent intact.
- Alternatives considered: Drop intent and route home (rejected: breaks expected flow).

## Decision 9: Reliability and observability
- Decision: Add idempotency key (`event_id` + recipient), delivery attempt records, provider-response capture, and stale-token cleanup workflow.
- Rationale: Supports FR-010/FR-011/FR-019 and reduces duplicate sends.
- Alternatives considered: Best-effort fire-and-forget (rejected: poor supportability).
