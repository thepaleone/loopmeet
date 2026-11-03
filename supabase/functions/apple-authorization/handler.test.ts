import { assert, assertEquals } from "jsr:@std/assert@1";
import { AppleAuthError } from "../_shared/apple-auth.ts";
import { type AppleAuthorizationPorts, handleAppleAuthorization } from "./handler.ts";

const USER_ID = "00000000-0000-0000-0000-00000000000a";

const fakePorts = (overrides: Partial<AppleAuthorizationPorts> = {}) => {
  const stored: { user_id: string; client_id: string; refresh_token: string }[] = [];
  const exchanged: string[] = [];
  const logs: { level: string; message: string; fields: Record<string, unknown> }[] = [];
  const ports: AppleAuthorizationPorts = {
    getUser: (jwt) => Promise.resolve(jwt === "valid-jwt" ? USER_ID : null),
    exchangeCode: (code) => {
      exchanged.push(code);
      return Promise.resolve("r-from-apple");
    },
    upsertAuthorization: (row) => {
      stored.push(row);
      return Promise.resolve();
    },
    nativeClientId: "io.loopmeet.app",
    log: (level, message, fields) => logs.push({ level, message, fields }),
    ...overrides,
  };
  return { ports, stored, exchanged, logs };
};

const post = (body: unknown, token: string | null = "valid-jwt") =>
  new Request("http://localhost/apple-authorization", {
    method: "POST",
    headers: token === null ? {} : { Authorization: `Bearer ${token}` },
    body: typeof body === "string" ? body : JSON.stringify(body),
  });

Deno.test("a missing or invalid bearer token is rejected with 401", async () => {
  const { ports, stored } = fakePorts();

  assertEquals((await handleAppleAuthorization(post({ authorizationCode: "c" }, null), ports)).status, 401);
  assertEquals((await handleAppleAuthorization(post({ authorizationCode: "c" }, "expired"), ports)).status, 401);
  assertEquals(stored, []);
});

Deno.test("a body with neither a code nor a refresh token and client id is rejected with 400", async () => {
  const { ports, stored } = fakePorts();

  for (const body of [{}, { refreshToken: "r" }, { clientId: "io.loopmeet.web" }, { authorizationCode: "" }, "not json", "null"]) {
    const response = await handleAppleAuthorization(post(body), ports);
    assertEquals(response.status, 400, `body ${JSON.stringify(body)}`);
    assertEquals(await response.json(), { code: "invalid_request" });
  }
  assertEquals(stored, []);
});

Deno.test("an authorization code is exchanged and stored under the native client id", async () => {
  const { ports, stored, exchanged, logs } = fakePorts();

  const response = await handleAppleAuthorization(post({ authorizationCode: "code-1" }), ports);

  assertEquals(response.status, 204);
  assertEquals(exchanged, ["code-1"]);
  assertEquals(stored, [{ user_id: USER_ID, client_id: "io.loopmeet.app", refresh_token: "r-from-apple" }]);
  assertEquals(logs, [{
    level: "info",
    message: "apple_authorization_stored",
    fields: { user_id: USER_ID, client_id: "io.loopmeet.app" },
  }]);
});

Deno.test("a refresh token is stored as is with its client id", async () => {
  const { ports, stored, exchanged } = fakePorts();

  const response = await handleAppleAuthorization(post({ refreshToken: "r-web", clientId: "io.loopmeet.web" }), ports);

  assertEquals(response.status, 204);
  assertEquals(exchanged, []);
  assertEquals(stored, [{ user_id: USER_ID, client_id: "io.loopmeet.web", refresh_token: "r-web" }]);
});

Deno.test("an Apple failure returns 502 and logs only the status", async () => {
  const { ports, stored, logs } = fakePorts({
    exchangeCode: () => Promise.reject(new AppleAuthError("token", 400)),
  });

  const response = await handleAppleAuthorization(post({ authorizationCode: "code-1" }), ports);

  assertEquals(response.status, 502);
  assertEquals(await response.json(), { code: "apple_exchange_failed" });
  assertEquals(stored, []);
  assertEquals(logs, [{ level: "warn", message: "apple_authorization_failed", fields: { user_id: USER_ID, status: 400 } }]);
});

Deno.test("no log line contains a token, a code or an email", async () => {
  const { ports, logs } = fakePorts({
    getUser: () => Promise.resolve(USER_ID),
  });
  const failing = fakePorts({ exchangeCode: () => Promise.reject(new Error("a@example.com r-secret")) });

  await handleAppleAuthorization(post({ authorizationCode: "code-secret" }), ports);
  await handleAppleAuthorization(post({ refreshToken: "r-secret", clientId: "io.loopmeet.web" }), ports);
  await handleAppleAuthorization(post({ authorizationCode: "code-secret" }), failing.ports);

  for (const line of [...logs, ...failing.logs]) {
    const text = JSON.stringify(line);
    for (const secret of ["valid-jwt", "code-secret", "r-secret", "r-from-apple", "@"]) {
      assert(!text.includes(secret), `log line leaks ${secret}: ${text}`);
    }
  }
});
