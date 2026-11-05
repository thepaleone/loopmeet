import { assert, assertEquals } from "jsr:@std/assert@1";
import { AccountApiError } from "../public/js/account-api.js";
import { canConfirm, formatDeletionDate, MESSAGES, messageFor, showApple, viewFor } from "../public/js/account-state.js";

const session = { access_token: "t", user: { email: "t1@example.test" } };

Deno.test("viewFor: no session is signed-out, whatever the status", () => {
  assertEquals(viewFor(null, null), "signed-out");
  assertEquals(viewFor(null, { status: "pending" }), "signed-out");
});

Deno.test("viewFor: a session without a status yet is loading", () => {
  assertEquals(viewFor(session, null), "loading");
});

Deno.test("viewFor: maps each deletion status to its view", () => {
  assertEquals(viewFor(session, { status: "none" }), "active");
  assertEquals(viewFor(session, { status: "pending", deletesAt: "2026-12-03T18:00:00Z" }), "scheduled");
  assertEquals(viewFor(session, { status: "purging" }), "purging");
});

Deno.test("viewFor: a 409 deletion_in_progress is purging, other failures and unknown statuses are error", () => {
  assertEquals(viewFor(session, new AccountApiError(409, "deletion_in_progress")), "purging");
  assertEquals(viewFor(session, new AccountApiError(0, "network")), "error");
  assertEquals(viewFor(session, new AccountApiError(500, "http_error")), "error");
  assertEquals(viewFor(session, { status: "something-new" }), "error");
});

Deno.test("canConfirm: only the exact text DELETE confirms, as the server checks", () => {
  assert(canConfirm("DELETE"));
  for (const text of ["delete", " DELETE", "DELETE ", "", "DELET", undefined, null]) {
    assert(!canConfirm(text), JSON.stringify(text));
  }
});

Deno.test("messageFor: maps each failure to the contract's fixed message", () => {
  assertEquals(messageFor(new AccountApiError(0, "network")), MESSAGES.network);
  assertEquals(messageFor(new TypeError("Failed to fetch")), MESSAGES.network);
  assertEquals(messageFor({ name: "AuthRetryableFetchError", status: 0, message: "Failed to fetch" }), MESSAGES.network);
  assertEquals(messageFor(new AccountApiError(401, "http_error")), MESSAGES.sessionEnded);
  assertEquals(messageFor({ name: "AuthApiError", status: 400, code: "invalid_credentials" }), MESSAGES.invalidLogin);
  assertEquals(messageFor({ code: "access_denied" }), MESSAGES.cancelled);
  assertEquals(messageFor(new Error("boom")), MESSAGES.generic);
  assertEquals(messageFor(undefined), MESSAGES.generic);

  assertEquals(MESSAGES.network, "Couldn't reach LoopMeet. Check your connection and try again.");
  assertEquals(MESSAGES.sessionEnded, "Your session ended. Sign in again.");
  assertEquals(MESSAGES.invalidLogin, "That email and password don't match a LoopMeet account.");
  assertEquals(MESSAGES.cancelled, "Sign-in was cancelled.");
  assertEquals(MESSAGES.generic, "Something went wrong. Try again, or email info@palehorse.net.");
});

Deno.test("messageFor: never shows the raw error text (FR-013)", () => {
  const raw = "duplicate key value violates unique constraint secret_table_pkey";
  for (const error of [new Error(raw), { message: raw, code: "weird" }, new AccountApiError(500, raw)]) {
    assert(!Object.values(MESSAGES).includes(raw));
    assert(!messageFor(error).includes("secret_table"), messageFor(error));
  }
});

Deno.test("showApple: only when a Services ID is configured", () => {
  assertEquals(showApple({ appleWebClientId: "" }), false);
  assertEquals(showApple({}), false);
  assertEquals(showApple({ appleWebClientId: "io.loopmeet.web" }), true);
});

Deno.test("formatDeletionDate: formats in the given locale and time zone", () => {
  // 05:00 UTC on December 3 is still December 2 in Los Angeles.
  assertEquals(formatDeletionDate("2026-12-03T05:00:00Z", "en-US", "America/Los_Angeles"), "December 2, 2026");
  assertEquals(formatDeletionDate("2026-12-03T05:00:00Z", "en-US", "UTC"), "December 3, 2026");
  assertEquals(formatDeletionDate("2026-12-03T05:00:00Z", "de-DE", "UTC"), "3. Dezember 2026");
});

Deno.test("formatDeletionDate: defaults to the visitor's locale and time zone", () => {
  const expected = new Intl.DateTimeFormat(undefined, { dateStyle: "long" }).format(new Date("2026-12-03T18:00:00Z"));
  assertEquals(formatDeletionDate("2026-12-03T18:00:00Z"), expected);
});
