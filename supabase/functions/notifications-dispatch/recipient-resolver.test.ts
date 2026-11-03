import { assertEquals } from "jsr:@std/assert@1";
import { fakeClient } from "../_shared/fake-query-client.ts";
import { resolveRecipients } from "./recipient-resolver.ts";

Deno.test("meetup recipients come from active_memberships", async () => {
  const { client, queries } = fakeClient(() => ({
    data: [{ member_user_id: "member-1" }, { member_user_id: "member-2" }],
    error: null,
  }));

  const recipients = await resolveRecipients(client, { table: "meetups", record: { group_id: "group-1" } });

  assertEquals(recipients, ["member-1", "member-2"]);
  assertEquals(queries, [{ table: "active_memberships", columns: "member_user_id", filters: [["group_id", "group-1"]] }]);
});

Deno.test("an invitation to a pending user has no recipients", async () => {
  const { client } = fakeClient(() => ({ data: { user_id: "invited-1" }, error: null }));

  const recipients = await resolveRecipients(client, { table: "invitations", record: { invited_user_id: "invited-1" } });

  assertEquals(recipients, []);
});

Deno.test("an invitation to an active user goes to that user", async () => {
  const { client, queries } = fakeClient(() => ({ data: null, error: null }));

  const recipients = await resolveRecipients(client, { table: "invitations", record: { invited_user_id: "invited-1" } });

  assertEquals(recipients, ["invited-1"]);
  assertEquals(queries.map((query) => query.table), ["account_deletions"]);
});

Deno.test("an invitation to an email without an account has no recipients and no lookup", async () => {
  const { client, queries } = fakeClient(() => ({ data: null, error: null }));

  const recipients = await resolveRecipients(client, { table: "invitations", record: { invited_user_id: null } });

  assertEquals(recipients, []);
  assertEquals(queries, []);
});
