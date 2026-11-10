import { assert, assertEquals, assertRejects, assertThrows } from "jsr:@std/assert@1";
import { generate, type Options, parseOptions } from "./apple-client-secret.ts";
import { createClientSecret, MAX_CLIENT_SECRET_LIFETIME_SECONDS } from "../_shared/apple-auth.ts";

const base64UrlDecode = (value: string): Uint8Array<ArrayBuffer> => {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  return Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));
};

const decodeJson = (value: string) => JSON.parse(new TextDecoder().decode(base64UrlDecode(value)));

const generateKey = async () => {
  const pair = await crypto.subtle.generateKey({ name: "ECDSA", namedCurve: "P-256" }, true, ["sign", "verify"]);
  const pkcs8 = new Uint8Array(await crypto.subtle.exportKey("pkcs8", pair.privateKey));
  let binary = "";
  for (const byte of pkcs8) binary += String.fromCharCode(byte);
  const body = btoa(binary).match(/.{1,64}/g)!.join("\n");
  return { pem: `-----BEGIN PRIVATE KEY-----\n${body}\n-----END PRIVATE KEY-----\n`, publicKey: pair.publicKey };
};

const args = ["--team-id", "TEAM123456", "--key-id", "KEY1234567", "--client-id", "io.loopmeet.web", "--key-file", "AuthKey.p8"];
const options: Options = { teamId: "TEAM123456", keyId: "KEY1234567", clientId: "io.loopmeet.web", keyFile: "AuthKey.p8" };

Deno.test("parseOptions reads the four flags", () => {
  assertEquals(parseOptions(args), options);
  assertEquals(parseOptions(["--team-id=TEAM123456", "--key-id=KEY1234567", "--client-id=io.loopmeet.web", "--key-file=AuthKey.p8"]), options);
});

Deno.test("parseOptions names every missing flag", () => {
  const error = assertThrows(() => parseOptions(["--team-id", "TEAM123456"]), Error);
  assert(error.message.startsWith("Missing --key-id, --client-id, --key-file."), error.message);
});

Deno.test("parseOptions rejects team and key IDs that aren't 10 letters and digits", () => {
  assertThrows(() => parseOptions(args.with(1, "team12345")), Error, "--team-id");
  assertThrows(() => parseOptions(args.with(3, "KEY-123456")), Error, "--key-id");
});

Deno.test("generate signs a 6-month secret for the Services ID that verifies with the key", async () => {
  const { pem, publicKey } = await generateKey();
  const now = new Date("2026-11-10T18:00:00Z");
  const reads: string[] = [];

  const { secret, expiresAt } = await generate(options, (path) => {
    reads.push(path);
    return Promise.resolve(pem);
  }, now);

  assertEquals(reads, ["AuthKey.p8"]);
  const [header, payload, signature] = secret.split(".");
  assertEquals(decodeJson(header), { alg: "ES256", kid: "KEY1234567" });
  const iat = Math.floor(now.getTime() / 1000);
  assertEquals(decodeJson(payload), {
    iss: "TEAM123456",
    iat,
    exp: iat + MAX_CLIENT_SECRET_LIFETIME_SECONDS,
    aud: "https://appleid.apple.com",
    sub: "io.loopmeet.web",
  });
  assertEquals(expiresAt.toISOString(), "2027-05-12T08:30:00.000Z");
  assert(
    await crypto.subtle.verify(
      { name: "ECDSA", hash: "SHA-256" },
      publicKey,
      base64UrlDecode(signature),
      new TextEncoder().encode(`${header}.${payload}`),
    ),
  );
});

Deno.test("generate refuses a file that isn't a private key", async () => {
  await assertRejects(() => generate(options, () => Promise.resolve("not a key"), new Date()), Error, "isn't a .p8 private key");
});

Deno.test("createClientSecret refuses a lifetime past Apple's 6-month limit", async () => {
  const { pem } = await generateKey();
  const env = { teamId: "TEAM123456", keyId: "KEY1234567", privateKey: pem, clientId: "io.loopmeet.web" };
  for (const lifetime of [0, -1, 1.5, MAX_CLIENT_SECRET_LIFETIME_SECONDS + 1]) {
    await assertRejects(() => createClientSecret(env, new Date(), env.clientId, lifetime), RangeError);
  }
});
