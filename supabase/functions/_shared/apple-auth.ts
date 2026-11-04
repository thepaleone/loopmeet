// Sign in with Apple token exchange and revocation (research D7). Apple authenticates these calls
// with a short-lived ES256 JWT signed by the team's .p8 key.

export interface AppleEnv {
  teamId: string;
  keyId: string;
  privateKey: string; // PKCS#8 PEM, the contents of AuthKey_<KEY_ID>.p8
  clientId: string; // the native app's client, io.loopmeet.app
}

export type FetchLike = (input: string, init: RequestInit) => Promise<Response>;

// Carries Apple's HTTP status only. The response body can echo tokens, so it's never kept.
export class AppleAuthError extends Error {
  constructor(readonly operation: "token" | "revoke", readonly status: number) {
    super(`apple_${operation}_failed:${status}`);
  }
}

const APPLE_AUDIENCE = "https://appleid.apple.com";
const TOKEN_URL = "https://appleid.apple.com/auth/token";
const REVOKE_URL = "https://appleid.apple.com/auth/revoke";
const CLIENT_SECRET_LIFETIME_SECONDS = 300;

export const readAppleEnv = (): AppleEnv => ({
  teamId: Deno.env.get("APPLE_TEAM_ID") ?? "",
  keyId: Deno.env.get("APPLE_KEY_ID") ?? "",
  privateKey: Deno.env.get("APPLE_PRIVATE_KEY") ?? "",
  clientId: Deno.env.get("APPLE_CLIENT_ID") ?? "",
});

const base64UrlEncode = (bytes: Uint8Array): string => {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
};

const base64UrlEncodeJson = (value: unknown): string =>
  base64UrlEncode(new TextEncoder().encode(JSON.stringify(value)));

const pemToPkcs8 = (pem: string): Uint8Array<ArrayBuffer> => {
  // Secrets set from a one-line value carry literal "\n" sequences instead of newlines.
  const body = pem
    .replace(/\\n/g, "\n")
    .replace(/-----BEGIN PRIVATE KEY-----/, "")
    .replace(/-----END PRIVATE KEY-----/, "")
    .replace(/\s+/g, "");
  return Uint8Array.from(atob(body), (char) => char.charCodeAt(0));
};

// sub is the client the token belongs to: the native app, or the web Services ID for tokens
// stored by 017.
export const createClientSecret = async (
  env: AppleEnv,
  now: Date,
  clientId: string = env.clientId,
): Promise<string> => {
  const key = await crypto.subtle.importKey(
    "pkcs8",
    pemToPkcs8(env.privateKey),
    { name: "ECDSA", namedCurve: "P-256" },
    false,
    ["sign"],
  );

  const issuedAt = Math.floor(now.getTime() / 1000);
  const signingInput = `${base64UrlEncodeJson({ alg: "ES256", kid: env.keyId })}.${
    base64UrlEncodeJson({
      iss: env.teamId,
      iat: issuedAt,
      exp: issuedAt + CLIENT_SECRET_LIFETIME_SECONDS,
      aud: APPLE_AUDIENCE,
      sub: clientId,
    })
  }`;

  // WebCrypto returns the raw r||s signature, which is the encoding JWS ES256 expects.
  const signature = await crypto.subtle.sign(
    { name: "ECDSA", hash: "SHA-256" },
    key,
    new TextEncoder().encode(signingInput),
  );

  return `${signingInput}.${base64UrlEncode(new Uint8Array(signature))}`;
};

const postForm = (fetchImpl: FetchLike, url: string, fields: Record<string, string>) =>
  fetchImpl(url, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(fields).toString(),
  });

export const exchangeAuthorizationCode = async (
  env: AppleEnv,
  code: string,
  fetchImpl: FetchLike = fetch,
): Promise<string> => {
  const response = await postForm(fetchImpl, TOKEN_URL, {
    client_id: env.clientId,
    client_secret: await createClientSecret(env, new Date()),
    code,
    grant_type: "authorization_code",
  });

  if (!response.ok) {
    await response.body?.cancel();
    throw new AppleAuthError("token", response.status);
  }

  const payload = (await response.json()) as { refresh_token?: unknown };
  if (typeof payload.refresh_token !== "string" || payload.refresh_token === "") {
    throw new AppleAuthError("token", response.status);
  }
  return payload.refresh_token;
};

export const revokeRefreshToken = async (
  env: AppleEnv,
  clientId: string,
  token: string,
  fetchImpl: FetchLike = fetch,
): Promise<void> => {
  const response = await postForm(fetchImpl, REVOKE_URL, {
    client_id: clientId,
    client_secret: await createClientSecret(env, new Date(), clientId),
    token,
    token_type_hint: "refresh_token",
  });

  await response.body?.cancel();
  if (response.status !== 200) {
    throw new AppleAuthError("revoke", response.status);
  }
};
