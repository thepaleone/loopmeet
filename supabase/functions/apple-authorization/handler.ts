// Stores the Apple refresh token for a signed-in user so permanent deletion can revoke it
// (research D7). index.ts wires the real ports.
import { AppleAuthError } from "../_shared/apple-auth.ts";

export interface AppleAuthorizationPorts {
  // The caller's user id, or null when the token is missing, expired or invalid.
  getUser(jwt: string): Promise<string | null>;
  // Exchanges an authorization code for a refresh token. Throws AppleAuthError.
  exchangeCode(code: string): Promise<string>;
  upsertAuthorization(row: { user_id: string; client_id: string; refresh_token: string }): Promise<void>;
  // The native app's Apple client id (APPLE_CLIENT_ID), which issued authorization codes.
  nativeClientId: string;
  log(level: "info" | "warn" | "error", message: string, fields: Record<string, unknown>): void;
}

const nonEmpty = (value: unknown): value is string => typeof value === "string" && value.trim() !== "";

const invalidRequest = () => Response.json({ code: "invalid_request" }, { status: 400 });

export const handleAppleAuthorization = async (
  request: Request,
  ports: AppleAuthorizationPorts,
): Promise<Response> => {
  if (request.method !== "POST") {
    return Response.json({ error: "method_not_allowed" }, { status: 405 });
  }

  const authorization = request.headers.get("Authorization") ?? "";
  const jwt = authorization.toLowerCase().startsWith("bearer ") ? authorization.slice(7).trim() : "";
  const userId = jwt === "" ? null : await ports.getUser(jwt);
  if (userId === null) {
    return new Response(null, { status: 401 });
  }

  let body: Record<string, unknown>;
  try {
    const parsed = await request.json();
    if (typeof parsed !== "object" || parsed === null) return invalidRequest();
    body = parsed as Record<string, unknown>;
  } catch {
    return invalidRequest();
  }

  let clientId: string;
  let refreshToken: string;
  if (nonEmpty(body.authorizationCode)) {
    clientId = ports.nativeClientId;
    try {
      refreshToken = await ports.exchangeCode(body.authorizationCode);
    } catch (err) {
      const status = err instanceof AppleAuthError ? err.status : "exception";
      ports.log("warn", "apple_authorization_failed", { user_id: userId, status });
      return Response.json({ code: "apple_exchange_failed" }, { status: 502 });
    }
  } else if (nonEmpty(body.refreshToken) && nonEmpty(body.clientId)) {
    // 017 web: Supabase already exchanged the code and hands the client its provider refresh token.
    clientId = body.clientId;
    refreshToken = body.refreshToken;
  } else {
    return invalidRequest();
  }

  try {
    await ports.upsertAuthorization({ user_id: userId, client_id: clientId, refresh_token: refreshToken });
  } catch {
    ports.log("error", "apple_authorization_failed", { user_id: userId, status: "store_failed" });
    return Response.json({ code: "store_failed" }, { status: 500 });
  }

  ports.log("info", "apple_authorization_stored", { user_id: userId, client_id: clientId });
  return new Response(null, { status: 204 });
};
