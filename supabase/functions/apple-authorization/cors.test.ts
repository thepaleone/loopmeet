import { assert, assertEquals } from "jsr:@std/assert@1";
import cases from "./site-origins.cases.json" with { type: "json" };
import { corsHeadersFor, isAllowedOrigin, parseOrigins, withCors } from "./cors.ts";

const allowed = ["https://account.loopmeet.io", "https://*-loopmeet-account.joelgray.workers.dev"];

for (const c of cases) {
  Deno.test(`isAllowedOrigin: ${c.name}`, () => {
    assertEquals(isAllowedOrigin(c.origin, c.patterns), c.expectedAllowed);
  });
}

Deno.test("the shared cases keep covering the rule", () => {
  assert(cases.length >= 20, "site-origins.cases.json must keep at least 20 cases");
});

Deno.test("parseOrigins splits on commas, trims and drops empty entries", () => {
  assertEquals(parseOrigins(" https://a.test, ,https://*-b.test ,"), ["https://a.test", "https://*-b.test"]);
  assertEquals(parseOrigins(""), []);
});

Deno.test("corsHeadersFor echoes an allowed origin with the headers supabase-js sends", () => {
  const headers = corsHeadersFor("https://main-loopmeet-account.joelgray.workers.dev", allowed);

  assertEquals(headers?.["Access-Control-Allow-Origin"], "https://main-loopmeet-account.joelgray.workers.dev");
  assertEquals(headers?.["Access-Control-Allow-Methods"], "POST");
  assertEquals(headers?.["Vary"], "Origin");
  const allowedHeaders = headers?.["Access-Control-Allow-Headers"].split(", ") ?? [];
  for (const name of ["authorization", "content-type", "apikey", "x-client-info"]) {
    assert(allowedHeaders.includes(name), `missing ${name}`);
  }
});

Deno.test("corsHeadersFor gives null for a disallowed or missing origin, or an empty list", () => {
  assertEquals(corsHeadersFor("https://evil.test", allowed), null);
  assertEquals(corsHeadersFor(null, allowed), null);
  assertEquals(corsHeadersFor("https://account.loopmeet.io", []), null);
});

const handled: Request[] = [];
const handler = (request: Request) => {
  handled.push(request);
  return Promise.resolve(Response.json({ code: "stored" }, { status: 200, headers: { "X-Handler": "yes" } }));
};
const wrapped = withCors(handler, allowed);
const request = (method: string, origin: string | null) =>
  new Request("http://localhost/apple-authorization", {
    method,
    headers: origin === null ? {} : { Origin: origin },
    body: method === "POST" ? "{}" : undefined,
  });

Deno.test("withCors answers a preflight from an allowed origin without calling the handler", async () => {
  handled.length = 0;
  const response = await wrapped(request("OPTIONS", "https://account.loopmeet.io"));

  assertEquals(response.status, 204);
  assertEquals(response.headers.get("Access-Control-Allow-Origin"), "https://account.loopmeet.io");
  assertEquals(response.headers.get("Access-Control-Allow-Methods"), "POST");
  assert(response.headers.get("Access-Control-Allow-Headers")?.includes("authorization"));
  assertEquals(response.headers.get("Access-Control-Allow-Credentials"), null);
  assertEquals(handled.length, 0);
});

Deno.test("withCors adds the origin to the handler's response for an allowed origin", async () => {
  handled.length = 0;
  const response = await wrapped(request("POST", "https://account.loopmeet.io"));

  assertEquals(response.status, 200);
  assertEquals(await response.json(), { code: "stored" });
  assertEquals(response.headers.get("X-Handler"), "yes");
  assertEquals(response.headers.get("Access-Control-Allow-Origin"), "https://account.loopmeet.io");
  assertEquals(response.headers.get("Vary"), "Origin");
  assertEquals(handled.length, 1);
});

Deno.test("withCors leaves a disallowed origin's preflight to the handler, with no CORS headers", async () => {
  handled.length = 0;
  const response = await wrapped(request("OPTIONS", "https://evil.test"));

  assertEquals(handled.length, 1);
  assertEquals(response.headers.get("Access-Control-Allow-Origin"), null);
  await response.body?.cancel();
});

Deno.test("withCors passes a request without Origin, like the app's, through unchanged", async () => {
  handled.length = 0;
  const response = await wrapped(request("POST", null));

  assertEquals(handled.length, 1);
  assertEquals(response.headers.get("X-Handler"), "yes");
  assertEquals(response.headers.get("Access-Control-Allow-Origin"), null);
  assertEquals(response.headers.get("Vary"), null);
  await response.body?.cancel();
});
