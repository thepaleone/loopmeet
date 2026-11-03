import { assert, assertStringIncludes } from "jsr:@std/assert@1";

// The scheduler's handler builds its own client at module load, so the recipient query is
// checked in the source (the repo's source-inspection pattern for code that can't be injected).
const source = await Deno.readTextFile(new URL("./index.ts", import.meta.url));

Deno.test("reminder recipients are read from active_memberships", () => {
  assertStringIncludes(source, `.from("active_memberships")`);
});

Deno.test("reminder recipients are never read from raw memberships", () => {
  assert(!source.includes(`.from("memberships")`), "index.ts still reads memberships directly");
});
