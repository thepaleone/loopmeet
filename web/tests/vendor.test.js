import { assertEquals, assertMatch } from "jsr:@std/assert@1";
import { encodeHex } from "jsr:@std/encoding@1/hex";

// The vendored supabase-js must be exactly the file web/vendor.lock records (017 research D3).
// To update it: download the npm tarball, check its integrity against the registry, copy
// package/dist/umd/supabase.js, and record the new version, integrity and SHA-256 here.
const webRoot = new URL("../", import.meta.url);
const lock = JSON.parse(await Deno.readTextFile(new URL("vendor.lock", webRoot)));

for (const [name, entry] of Object.entries(lock)) {
  Deno.test(`${name}: the vendored file matches vendor.lock`, async () => {
    const bytes = await Deno.readFile(new URL(entry.file, webRoot));
    const digest = encodeHex(await crypto.subtle.digest("SHA-256", bytes));
    assertEquals(digest, entry.sha256, `${entry.file} changed; update vendor.lock only after checking the new file`);
  });

  Deno.test(`${name}: the lock pins an exact version from the npm registry, with its license`, async () => {
    assertMatch(entry.version, /^\d+\.\d+\.\d+$/);
    assertMatch(entry.source, new RegExp(`^https://registry\\.npmjs\\.org/.+-${entry.version.replaceAll(".", "\\.")}\\.tgz$`));
    assertMatch(entry.sourceIntegrity, /^sha512-/);
    assertMatch(await Deno.readTextFile(new URL(entry.license, webRoot)), /MIT License/);
  });
}
