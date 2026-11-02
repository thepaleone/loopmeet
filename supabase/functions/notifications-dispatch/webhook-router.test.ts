import { assertEquals } from "jsr:@std/assert@1";
import {
  resolveNotificationType,
  resolveSuppression,
  type WebhookPayload,
} from "./webhook-router.ts";

// 2026-10-30 13:00 in Los Angeles.
const now = new Date("2026-10-30T20:00:00Z");
const la = "America/Los_Angeles";
const past = { scheduled_at: "2026-10-27T02:00:00+00:00", timezone: la };
const earlierPast = { scheduled_at: "2026-10-20T02:00:00+00:00", timezone: la };
const upcoming = { scheduled_at: "2026-11-02T02:00:00+00:00", timezone: la };
const startedToday = {
  scheduled_at: "2026-10-30T16:00:00+00:00",
  timezone: la,
};

const meetupUpdate = (
  oldRecord: Record<string, unknown> | null,
  record: Record<string, unknown>,
): WebhookPayload => ({
  type: "UPDATE",
  table: "meetups",
  record: { id: "m1", group_id: "g1", ...record },
  old_record: oldRecord === null
    ? null
    : { id: "m1", group_id: "g1", ...oldRecord },
});

Deno.test("past -> past edit is suppressed", () => {
  assertEquals(
    resolveSuppression(meetupUpdate(past, earlierPast), now),
    "past_meetup",
  );
});

Deno.test("past -> past cancel is suppressed before the status branch", () => {
  const payload = meetupUpdate(past, { ...past, status: "canceled" });
  assertEquals(resolveSuppression(payload, now), "past_meetup");
});

Deno.test("upcoming -> upcoming edit is not suppressed", () => {
  assertEquals(resolveSuppression(meetupUpdate(upcoming, upcoming), now), null);
});

Deno.test("upcoming -> past edit is not suppressed (old record was upcoming)", () => {
  assertEquals(resolveSuppression(meetupUpdate(upcoming, past), now), null);
});

Deno.test("meetup that started earlier today is still upcoming, so not suppressed", () => {
  assertEquals(
    resolveSuppression(meetupUpdate(startedToday, startedToday), now),
    null,
  );
});

Deno.test("past -> future edit is not suppressed and notifies as meetup.updated", () => {
  const payload = meetupUpdate(past, upcoming);
  assertEquals(resolveSuppression(payload, now), null);
  assertEquals(resolveNotificationType(payload), "meetup.updated");
});

Deno.test("update without old_record is not suppressed", () => {
  assertEquals(resolveSuppression(meetupUpdate(null, past), now), null);
});

Deno.test("inserts, deletes and other tables are never suppressed", () => {
  const insert: WebhookPayload = {
    type: "INSERT",
    table: "meetups",
    record: past,
  };
  const del: WebhookPayload = {
    type: "DELETE",
    table: "meetups",
    record: past,
    old_record: past,
  };
  const invitation: WebhookPayload = {
    type: "INSERT",
    table: "invitations",
    record: {},
  };
  assertEquals(resolveSuppression(insert, now), null);
  assertEquals(resolveSuppression(del, now), null);
  assertEquals(resolveSuppression(invitation, now), null);
});

Deno.test("deletes never notify", () => {
  const del: WebhookPayload = {
    type: "DELETE",
    table: "meetups",
    record: past,
    old_record: past,
  };
  assertEquals(resolveNotificationType(del), null);
});

Deno.test("plain meetup update still resolves to meetup.updated", () => {
  assertEquals(
    resolveNotificationType(meetupUpdate(upcoming, upcoming)),
    "meetup.updated",
  );
});
