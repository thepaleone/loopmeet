// CORS for the website's Account page (017 research D6). Only allowed origins get CORS headers;
// requests without an Origin header, like the app's, pass through unchanged.
// An entry is an exact origin, or holds one "*" in its first host label that matches one or more
// letters, digits or dashes and never a dot. src/LoopMeet.Api/Cors/SiteOrigins.cs holds the same
// rule; both test suites read site-origins.cases.json.

type Handler = (request: Request) => Promise<Response>;

const ALLOWED_HEADERS = "authorization, content-type, apikey, x-client-info";

export const parseOrigins = (csv: string): string[] =>
  csv.split(",").map((entry) => entry.trim()).filter(Boolean);

const wildcardIsInFirstHostLabel = (pattern: string, star: number): boolean => {
  if (pattern.indexOf("*", star + 1) >= 0) return false;
  const schemeEnd = pattern.indexOf("://");
  if (schemeEnd < 0 || star < schemeEnd + 3) return false;
  return !/[.:/]/.test(pattern.slice(schemeEnd + 3, star));
};

const matches = (origin: string, pattern: string): boolean => {
  const star = pattern.indexOf("*");
  if (star < 0) return origin.toLowerCase() === pattern.toLowerCase();
  if (!wildcardIsInFirstHostLabel(pattern, star)) return false;

  const value = origin.toLowerCase();
  const prefix = pattern.slice(0, star).toLowerCase();
  const suffix = pattern.slice(star + 1).toLowerCase();
  if (value.length <= prefix.length + suffix.length) return false;
  if (!value.startsWith(prefix) || !value.endsWith(suffix)) return false;
  return /^[a-z0-9-]+$/.test(value.slice(prefix.length, value.length - suffix.length));
};

export const isAllowedOrigin = (origin: string | null, patterns: string[]): boolean =>
  !!origin && patterns.some((pattern) => matches(origin, pattern.trim()));

export const corsHeadersFor = (origin: string | null, allowed: string[]): Record<string, string> | null =>
  origin && isAllowedOrigin(origin, allowed)
    ? {
      "Access-Control-Allow-Origin": origin,
      "Access-Control-Allow-Methods": "POST",
      "Access-Control-Allow-Headers": ALLOWED_HEADERS,
      "Vary": "Origin",
    }
    : null;

export const withCors = (handler: Handler, allowed: string[]): Handler => async (request) => {
  const cors = corsHeadersFor(request.headers.get("Origin"), allowed);
  if (!cors) return handler(request);
  if (request.method === "OPTIONS") return new Response(null, { status: 204, headers: cors });

  const response = await handler(request);
  const headers = new Headers(response.headers);
  headers.set("Access-Control-Allow-Origin", cors["Access-Control-Allow-Origin"]);
  headers.set("Vary", "Origin");
  return new Response(response.body, { status: response.status, statusText: response.statusText, headers });
};
