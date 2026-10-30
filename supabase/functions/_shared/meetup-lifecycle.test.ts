import { assertEquals } from "jsr:@std/assert@1";
import cases from "./meetup-lifecycle.cases.json" with { type: "json" };
import { isMeetupPast } from "./meetup-lifecycle.ts";

interface LifecycleCase {
  name: string;
  scheduledAt: string;
  timezone: string | null;
  now: string;
  expectedIsPast: boolean;
}

for (const c of cases as LifecycleCase[]) {
  Deno.test(`isMeetupPast: ${c.name}`, () => {
    assertEquals(
      isMeetupPast(c.scheduledAt, c.timezone, new Date(c.now)),
      c.expectedIsPast,
    );
  });
}

Deno.test("shared case file is not emptied", () => {
  assertEquals((cases as LifecycleCase[]).length >= 15, true);
});
