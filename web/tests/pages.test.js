import { assert, assertEquals, assertMatch, assertNotMatch } from "jsr:@std/assert@1";
import { fileExists, firstMatch, pages, publicRoot, readPublic, textOf } from "./site.js";

const loaded = await Promise.all(pages.map(async (page) => ({ ...page, html: await readPublic(page.file) })));
const byName = Object.fromEntries(loaded.map((page) => [page.name, page]));

for (const page of loaded) {
  Deno.test(`${page.name}: has a language, title, viewport and exactly one h1`, () => {
    assertMatch(page.html, /<html lang="en">/);
    assertMatch(page.html, /<title>[^<]+<\/title>/);
    assertMatch(page.html, /<meta name="viewport" content="width=device-width, initial-scale=1">/);
    assertEquals(page.html.match(/<h1[\s>]/g)?.length, 1);
  });

  Deno.test(`${page.name}: header links to every page, footer names the operator and contact`, () => {
    const header = firstMatch(page.html, /<header[\s\S]*?<\/header>/);
    const footer = firstMatch(page.html, /<footer[\s\S]*?<\/footer>/);
    assert(header, "missing <header>");
    assert(footer, "missing <footer>");
    for (const { path } of pages) {
      assertMatch(header, new RegExp(`href="${path}"`));
    }
    assertMatch(footer, /Palehorse, LLC/);
    assertMatch(footer, /href="mailto:info@palehorse\.net"/);
  });

  Deno.test(`${page.name}: every site-relative link and asset exists`, async () => {
    const targets = [...page.html.matchAll(/(?:href|src)="(\/[^"#?]*)"/g)].map((match) => match[1]);
    for (const target of targets) {
      const relative = target.slice(1);
      const file = relative === "" || relative.endsWith("/") ? `${relative}index.html` : relative;
      assert(await fileExists(new URL(file, publicRoot)), `${page.file} links to missing ${target}`);
    }
  });

  Deno.test(`${page.name}: every image has alt text`, () => {
    for (const img of page.html.match(/<img\b[^>]*>/g) ?? []) {
      assertMatch(img, /\salt="[^"]*"/, img);
    }
  });
}

Deno.test("every page shares the same header and footer", () => {
  const chrome = (html) =>
    `${firstMatch(html, /<header[\s\S]*?<\/header>/)}${firstMatch(html, /<footer[\s\S]*?<\/footer>/)}`
      // The current page is marked for screen readers; everything else must match.
      .replace(/ aria-current="page"/g, "");
  const [first, ...rest] = loaded;
  for (const page of rest) {
    assertEquals(chrome(page.html), chrome(first.html), `${page.file} differs from ${first.file}`);
  }
});

Deno.test("each page marks itself as the current page in the header", () => {
  for (const page of loaded) {
    const header = firstMatch(page.html, /<header[\s\S]*?<\/header>/);
    assertMatch(header, new RegExp(`href="${page.path}" aria-current="page"`), page.file);
    assertEquals(header.match(/aria-current="page"/g)?.length, 1, page.file);
  }
});

for (const name of ["Home", "Privacy", "Support"]) {
  Deno.test(`${name}: reads fully without JavaScript (FR-002)`, () => {
    assertNotMatch(byName[name].html, /<script\b/i);
  });
}

Deno.test("Home: describes LoopMeet and links to the Google Play listing only", () => {
  const { html } = byName.Home;
  assertMatch(html, /href="https:\/\/play\.google\.com\/store\/apps\/details\?id=io\.loopmeet\.app"/);
  // The App Store listing arrives with 013.
  assertNotMatch(html, /apps\.apple\.com/);
  assertMatch(textOf(html), /meetups/i);
});

Deno.test("Support: explains both ways to delete an account, the 30 days, and the contact", () => {
  const { html } = byName.Support;
  const text = textOf(html);
  assertMatch(text, /Profile → Delete Account/);
  assertMatch(html, /href="\/account\/"/);
  assertMatch(text, /30 days/);
  assertMatch(text, /Restore account/);
  assertMatch(html, /href="mailto:info@palehorse\.net"/);
});
