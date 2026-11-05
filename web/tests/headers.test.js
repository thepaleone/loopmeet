import { assert, assertEquals, assertMatch } from "jsr:@std/assert@1";
import { readPublic } from "./site.js";

// Cloudflare _headers (Workers static assets): a path line, then indented "Name: value" lines.
const parseHeaders = (text) => {
  const rules = new Map();
  let current = null;
  for (const line of text.split("\n")) {
    if (line.trim() === "" || line.trim().startsWith("#")) continue;
    if (!/^\s/.test(line)) {
      current = new Map();
      rules.set(line.trim(), current);
    } else {
      const index = line.indexOf(":");
      current.set(line.slice(0, index).trim().toLowerCase(), line.slice(index + 1).trim());
    }
  }
  return rules;
};

const rules = parseHeaders(await readPublic("_headers"));
const config = await readPublic("js/config.js");
const configValue = (key) => config.match(new RegExp(`${key}: "([^"]*)"`))?.[1];

Deno.test("every page gets the security headers", () => {
  const all = rules.get("/*");
  assert(all, "missing /* rule");
  assertEquals(all.get("x-content-type-options"), "nosniff");
  assertEquals(all.get("referrer-policy"), "strict-origin-when-cross-origin");
  assertMatch(all.get("permissions-policy"), /camera=\(\)/);
  assertMatch(all.get("permissions-policy"), /microphone=\(\)/);
  assertMatch(all.get("permissions-policy"), /geolocation=\(\)/);
});

Deno.test("the content security policy allows only our own scripts and our backend", () => {
  const csp = rules.get("/*").get("content-security-policy");
  const directives = new Map(
    csp.split(";").map((part) => part.trim()).filter(Boolean).map((part) => {
      const [name, ...values] = part.split(/\s+/);
      return [name, values];
    }),
  );
  assertEquals(directives.get("default-src"), ["'self'"]);
  assertEquals(directives.get("script-src"), ["'self'"]);
  assertEquals(directives.get("frame-ancestors"), ["'none'"]);
  assertEquals(directives.get("base-uri"), ["'self'"]);
  assertEquals(directives.get("form-action"), ["'self'"]);

  const connect = directives.get("connect-src");
  assert(connect.includes("'self'"));
  for (const key of ["supabaseUrl", "apiBaseUrl"]) {
    const value = configValue(key);
    assert(value?.startsWith("https://"), `config.js ${key} must be an https URL`);
    assert(connect.includes(value), `connect-src is missing ${key} (${value})`);
  }
});

Deno.test("the account page is never cached", () => {
  assertEquals(rules.get("/account/*")?.get("cache-control"), "no-store");
});

Deno.test("/delete-account is a permanent redirect to the account page", async () => {
  const redirects = (await readPublic("_redirects")).split("\n").map((line) => line.trim().split(/\s+/));
  const rule = redirects.find(([from]) => from === "/delete-account");
  assertEquals(rule, ["/delete-account", "/account/", "301"]);
});
