import { assert, assertEquals, assertRejects, assertStringIncludes } from "jsr:@std/assert@1";
import {
  AppleAuthError,
  type AppleEnv,
  createClientSecret,
  exchangeAuthorizationCode,
  revokeRefreshToken,
} from "./apple-auth.ts";

const base64UrlDecode = (value: string): Uint8Array<ArrayBuffer> => {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  return Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));
};

const decodeJson = (value: string) => JSON.parse(new TextDecoder().decode(base64UrlDecode(value)));

const generateEnv = async (): Promise<{ env: AppleEnv; publicKey: CryptoKey }> => {
  const pair = await crypto.subtle.generateKey({ name: "ECDSA", namedCurve: "P-256" }, true, ["sign", "verify"]);
  const pkcs8 = new Uint8Array(await crypto.subtle.exportKey("pkcs8", pair.privateKey));
  let binary = "";
  for (const byte of pkcs8) binary += String.fromCharCode(byte);
  const body = btoa(binary).match(/.{1,64}/g)!.join("\n");
  return {
    env: {
      teamId: "TEAM123456",
      keyId: "KEY1234567",
      privateKey: `-----BEGIN PRIVATE KEY-----\n${body}\n-----END PRIVATE KEY-----\n`,
      clientId: "io.loopmeet.app",
    },
    publicKey: pair.publicKey,
  };
};

interface RecordedRequest {
  url: string;
  form: URLSearchParams;
}

const fakeFetch = (status: number, body: unknown) => {
  const requests: RecordedRequest[] = [];
  const fetchImpl = (url: string, init: RequestInit) => {
    requests.push({ url, form: new URLSearchParams(String(init.body)) });
    return Promise.resolve(new Response(JSON.stringify(body), { status }));
  };
  return { fetchImpl, requests };
};

Deno.test("createClientSecret builds an ES256 JWT that verifies with the team key", async () => {
  const { env, publicKey } = await generateEnv();
  const now = new Date("2026-11-03T18:00:00Z");

  const jwt = await createClientSecret(env, now);
  const [header, payload, signature] = jwt.split(".");

  assertEquals(decodeJson(header), { alg: "ES256", kid: "KEY1234567" });
  const iat = Math.floor(now.getTime() / 1000);
  assertEquals(decodeJson(payload), {
    iss: "TEAM123456",
    iat,
    exp: iat + 300,
    aud: "https://appleid.apple.com",
    sub: "io.loopmeet.app",
  });

  const verified = await crypto.subtle.verify(
    { name: "ECDSA", hash: "SHA-256" },
    publicKey,
    base64UrlDecode(signature),
    new TextEncoder().encode(`${header}.${payload}`),
  );
  assert(verified);
});

Deno.test("createClientSecret accepts a key whose newlines arrived as \\n", async () => {
  const { env } = await generateEnv();
  const oneLine = { ...env, privateKey: env.privateKey.replace(/\n/g, "\\n") };

  const jwt = await createClientSecret(oneLine, new Date(), "io.loopmeet.web");

  assertEquals(decodeJson(jwt.split(".")[1]).sub, "io.loopmeet.web");
});

Deno.test("exchangeAuthorizationCode posts the code and returns the refresh token", async () => {
  const { env } = await generateEnv();
  const { fetchImpl, requests } = fakeFetch(200, { refresh_token: "r-123", access_token: "a" });

  const token = await exchangeAuthorizationCode(env, "code-1", fetchImpl);

  assertEquals(token, "r-123");
  assertEquals(requests.length, 1);
  assertEquals(requests[0].url, "https://appleid.apple.com/auth/token");
  assertEquals(requests[0].form.get("client_id"), "io.loopmeet.app");
  assertEquals(requests[0].form.get("code"), "code-1");
  assertEquals(requests[0].form.get("grant_type"), "authorization_code");
  assertEquals(requests[0].form.get("client_secret")?.split(".").length, 3);
});

Deno.test("revokeRefreshToken posts the token for the client that issued it", async () => {
  const { env } = await generateEnv();
  const { fetchImpl, requests } = fakeFetch(200, {});

  await revokeRefreshToken(env, "io.loopmeet.web", "r-456", fetchImpl);

  assertEquals(requests[0].url, "https://appleid.apple.com/auth/revoke");
  assertEquals(requests[0].form.get("client_id"), "io.loopmeet.web");
  assertEquals(requests[0].form.get("token"), "r-456");
  assertEquals(requests[0].form.get("token_type_hint"), "refresh_token");
  const secret = requests[0].form.get("client_secret")!;
  assertEquals(decodeJson(secret.split(".")[1]).sub, "io.loopmeet.web");
});

Deno.test("Apple errors carry the status, not the body", async () => {
  const { env } = await generateEnv();
  const { fetchImpl } = fakeFetch(400, { error: "invalid_grant", echoed: "r-secret" });

  const exchange = await assertRejects(() => exchangeAuthorizationCode(env, "code-1", fetchImpl), AppleAuthError);
  assertEquals(exchange.status, 400);
  assertEquals(exchange.message, "apple_token_failed:400");

  const revoke = await assertRejects(() => revokeRefreshToken(env, "io.loopmeet.app", "r-secret", fetchImpl), AppleAuthError);
  assertEquals(revoke.status, 400);
  assert(!revoke.message.includes("r-secret"));
  assertStringIncludes(revoke.message, "apple_revoke_failed");
});
