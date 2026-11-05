import { assert, assertEquals, assertInstanceOf, assertRejects } from "jsr:@std/assert@1";
import { AccountApiError, createAccountApi } from "../public/js/account-api.js";

const settings = {
  apiBaseUrl: "https://api.test",
  supabaseUrl: "https://project.supabase.test",
  supabasePublishableKey: "sb_publishable_test",
};

// A fake fetch that records each call and answers with the next queued response (or throws it).
const fakeFetch = (...responses) => {
  const calls = [];
  const fetch = (url, init = {}) => {
    calls.push({ url, method: init.method, headers: { ...init.headers }, body: init.body });
    const next = responses.shift();
    return next instanceof Error ? Promise.reject(next) : Promise.resolve(next);
  };
  return { fetch, calls };
};

const json = (status, body) => new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });

Deno.test("status: GETs the deletion status with the bearer token", async () => {
  const { fetch, calls } = fakeFetch(json(200, { status: "none" }));
  const api = createAccountApi({ ...settings, fetch });

  assertEquals(await api.status("token-1"), { status: "none" });
  assertEquals(calls, [{
    url: "https://api.test/users/account-deletion",
    method: "GET",
    headers: { Authorization: "Bearer token-1" },
    body: undefined,
  }]);
});

Deno.test("preview: GETs the preview", async () => {
  const preview = { deletesAt: "2026-12-03T18:00:00Z", ownedGroups: [{ id: "g", name: "Book Club", memberCount: 1 }] };
  const { fetch, calls } = fakeFetch(json(200, preview));
  const api = createAccountApi({ ...settings, fetch });

  assertEquals(await api.preview("token-1"), preview);
  assertEquals(calls[0].url, "https://api.test/users/account-deletion/preview");
  assertEquals(calls[0].method, "GET");
  assertEquals(calls[0].headers.Authorization, "Bearer token-1");
});

Deno.test("request: POSTs the DELETE confirmation as JSON and returns the 202 body", async () => {
  const pending = { status: "pending", requestedAt: "2026-11-03T18:00:00Z", deletesAt: "2026-12-03T18:00:00Z" };
  const { fetch, calls } = fakeFetch(json(202, pending));
  const api = createAccountApi({ ...settings, fetch });

  assertEquals(await api.request("token-1"), pending);
  assertEquals(calls, [{
    url: "https://api.test/users/account-deletion",
    method: "POST",
    headers: { Authorization: "Bearer token-1", "Content-Type": "application/json" },
    body: '{"confirmation":"DELETE"}',
  }]);
});

Deno.test("restore: DELETEs the deletion request and returns the 200 body", async () => {
  const { fetch, calls } = fakeFetch(json(200, { status: "none" }));
  const api = createAccountApi({ ...settings, fetch });

  assertEquals(await api.restore("token-1"), { status: "none" });
  assertEquals(calls[0].url, "https://api.test/users/account-deletion");
  assertEquals(calls[0].method, "DELETE");
  assertEquals(calls[0].headers, { Authorization: "Bearer token-1" });
});

Deno.test("failures throw AccountApiError with the status and the body's code", async () => {
  const cases = [
    [json(400, { code: "invalid_confirmation", message: "Type DELETE to confirm." }), 400, "invalid_confirmation"],
    [json(409, { code: "deletion_in_progress", message: "This account is already being deleted." }), 409, "deletion_in_progress"],
    [new Response(null, { status: 401 }), 401, "http_error"],
    [new Response("<html>Bad gateway</html>", { status: 502 }), 502, "http_error"],
  ];
  for (const [response, status, code] of cases) {
    const api = createAccountApi({ ...settings, fetch: fakeFetch(response).fetch });
    const error = await assertRejects(() => api.request("token-1"), AccountApiError);
    assertEquals([error.status, error.code], [status, code]);
  }
});

Deno.test("a fetch that rejects becomes a network AccountApiError", async () => {
  const api = createAccountApi({ ...settings, fetch: fakeFetch(new TypeError("Failed to fetch")).fetch });

  const error = await assertRejects(() => api.status("token-1"), AccountApiError);
  assertEquals([error.status, error.code], [0, "network"]);
});

Deno.test("AccountApiError's message carries only the status and code", () => {
  const error = new AccountApiError(409, "deletion_in_progress");
  assertInstanceOf(error, Error);
  assertEquals(error.message, "Account API 409 deletion_in_progress");
});

Deno.test("registerAppleToken: posts the refresh token and client id to apple-authorization", async () => {
  const { fetch, calls } = fakeFetch(new Response(null, { status: 204 }));
  const api = createAccountApi({ ...settings, fetch });

  assertEquals(await api.registerAppleToken("token-1", "r-apple", "io.loopmeet.web"), true);
  assertEquals(calls, [{
    url: "https://project.supabase.test/functions/v1/apple-authorization",
    method: "POST",
    headers: {
      Authorization: "Bearer token-1",
      apikey: "sb_publishable_test",
      "Content-Type": "application/json",
    },
    body: '{"refreshToken":"r-apple","clientId":"io.loopmeet.web"}',
  }]);
});

Deno.test("registerAppleToken: never throws, so a failure can't block sign-in (FR-011)", async () => {
  for (const outcome of [new Response("{}", { status: 500 }), new Response(null, { status: 401 }), new TypeError("offline")]) {
    const api = createAccountApi({ ...settings, fetch: fakeFetch(outcome).fetch });
    const result = await api.registerAppleToken("token-1", "r-apple", "io.loopmeet.web");
    assert(result === false);
  }
});
