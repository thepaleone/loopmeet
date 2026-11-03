import { assertEquals, assertRejects } from "jsr:@std/assert@1";
import { isAccountActive } from "./account-status.ts";
import { fakeClient } from "./fake-query-client.ts";

Deno.test("an account with no deletion row is active", async () => {
  const { client, queries } = fakeClient(() => ({ data: null, error: null }));

  assertEquals(await isAccountActive(client, "user-1"), true);
  assertEquals(queries, [{ table: "account_deletions", columns: "user_id", filters: [["user_id", "user-1"]] }]);
});

Deno.test("an account with a deletion row is not active", async () => {
  const { client } = fakeClient(() => ({ data: { user_id: "user-1" }, error: null }));

  assertEquals(await isAccountActive(client, "user-1"), false);
});

Deno.test("a failed lookup throws instead of treating the account as active", async () => {
  const { client } = fakeClient(() => ({ data: null, error: { message: "boom" } }));

  await assertRejects(() => isAccountActive(client, "user-1"), Error, "account_status_failed:boom");
});
