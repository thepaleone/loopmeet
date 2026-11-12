import { assert, assertMatch } from "jsr:@std/assert@1";
import { readPublic, readRepo, textOf } from "./site.js";

// The policy must cover what the app really does (017 FR-014–FR-018) and agree with 016's
// data-retention.md, which ci-scope.sh maps to this suite so a change there reruns these checks.
const html = await readPublic("privacy/index.html");
const policy = textOf(html);
const retention = await readRepo("specs/016-account-deletion/data-retention.md");

const assertSays = (pattern, what) => assertMatch(policy, pattern, `the policy doesn't cover: ${what}`);

Deno.test("covers every kind of data the app uses (FR-014)", () => {
  const categories = [
    [/email address/i, "account email"],
    [/password[^.]*(hash|never see)/i, "password, stored hashed"],
    [/display name/i, "display name"],
    [/phone number/i, "optional phone"],
    [/time zone/i, "time zone"],
    [/Google or Apple (account|identity)/i, "the sign-in method"],
    [/profile photo/i, "profile photo"],
    [/Google account picture/i, "photo taken from Google"],
    [/groups/i, "groups"],
    [/invitations?[^.]*email address of the person/i, "invitations, including the invitee's email"],
    [/meetups?[^.]*(place|address)/i, "meetups with their places"],
    [/coordinates/i, "place coordinates"],
    [/precise location/i, "precise location"],
    [/approximate location/i, "approximate location"],
    [/location isn't stored|never stores? your location/i, "location is not stored"],
    [/search text|what you type/i, "place-search text"],
    [/push[- ]notification/i, "push notifications"],
    [/device identifiers?/i, "push device identifiers"],
    [/delivery and open records|delivered and opened/i, "notification delivery and open records"],
    [/Sign in with Apple[^.]*token|token[^.]*Sign in with Apple/i, "the Sign in with Apple token"],
    [/logs/i, "operational logs"],
  ];
  for (const [pattern, what] of categories) assertSays(pattern, what);
});

Deno.test("names every company that processes data, and what each does (FR-015)", () => {
  for (const company of ["Supabase", "DigitalOcean", "Google", "Apple", "OneSignal", "Firebase Cloud Messaging", "Cloudflare"]) {
    assertMatch(html, new RegExp(`<td>${company}</td>`), `no processor row for ${company}`);
  }
  assertSays(/Places/, "Google Places");
});

Deno.test("states the deletion and retention rules as data-retention.md does (FR-016)", () => {
  // The values the policy repeats, checked in both documents so neither can drift alone.
  const shared = [
    [/\*\*30 days\*\*/, /30 days/, "the 30-day grace period"],
    [/within 24 hours/, /within 24 hours/, "deletion within 24 hours of the date"],
    [/Supabase database backups \| [^|]+\| 7 days/, /backups?[^.]*7 days/i, "7-day database backups"],
    [/Supabase logs[^|]*\| [^|]+\| 7 days/, /Supabase[^.]*logs?[^.]*7 days|logs?[^.]*Supabase[^.]*7 days/i, "7-day Supabase logs"],
    [/DigitalOcean[^|]*\| [^|]+\| Not stored/, /(not stored|aren't stored|doesn't store)/i, "Api logs not stored"],
    [/asynchronously/, /asynchronously/, "OneSignal's asynchronous deletion"],
    [/keeps no copy/, /keeps no copy/, "no copy kept after deletion"],
    [/Restore account/, /Restore account/, "restore during the 30 days"],
  ];
  for (const [inRetention, inPolicy, what] of shared) {
    assertMatch(retention, inRetention, `data-retention.md no longer says: ${what}`);
    assertSays(inPolicy, what);
  }
  assertSays(/Apple[^.]*(own records|its own record|Apple's privacy policy)/i, "Apple's own records");
  assertSays(/hidden from (everyone|other people)/i, "what's hidden during the 30 days");
});

Deno.test("says data isn't sold or used for ads, and there are no analytics or ad SDKs (FR-017)", () => {
  assertSays(/(don't|do not|never) sell/i, "no sale");
  assertSays(/advertising/i, "no advertising");
  assertSays(/no third-party analytics or advertising/i, "no analytics or ad SDKs");
});

Deno.test("explains how to exercise data rights, and has a last-updated date (FR-018)", () => {
  assertSays(/Profile → Delete Account/, "deletion in the app");
  assertMatch(html, /href="\/account\/"/, "a link to web deletion");
  assertMatch(html, /href="mailto:info@palehorse\.net"/, "the contact email");
  assertSays(/Palehorse, LLC/, "the operator");

  const updated = policy.match(/Last updated: ([A-Z][a-z]+ \d{1,2}, \d{4})/);
  assert(updated, "no 'Last updated: Month D, YYYY'");
  assert(!Number.isNaN(Date.parse(updated[1])), `unreadable date ${updated[1]}`);
});

Deno.test("says LoopMeet isn't for children under 13", () => {
  assertSays(/under 13/, "children under 13");
});

Deno.test("every section heading can be linked to, and the contents list links to each", () => {
  const ids = [...html.matchAll(/<h2 id="([a-z0-9-]+)">/g)].map((match) => match[1]);
  const headings = html.match(/<h2[\s>]/g) ?? [];
  assert(ids.length >= 10, `expected at least 10 sections, found ${ids.length}`);
  assert(ids.length === headings.length, "every <h2> needs an id");
  for (const id of ids) assertMatch(html, new RegExp(`href="#${id}"`), `contents list misses #${id}`);
});
