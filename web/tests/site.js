// Shared by the website tests: where the published files live, and small readers for them.
// Run from the repo root: deno test --allow-read=web,specs/016-account-deletion web/

export const publicRoot = new URL("../public/", import.meta.url);
export const repoRoot = new URL("../../", import.meta.url);

export const pages = [
  { name: "Home", path: "/", file: "index.html" },
  { name: "Privacy", path: "/privacy/", file: "privacy/index.html" },
  { name: "Support", path: "/support/", file: "support/index.html" },
  { name: "Account", path: "/account/", file: "account/index.html" },
];

export const readPublic = (file) => Deno.readTextFile(new URL(file, publicRoot));

export const readRepo = (file) => Deno.readTextFile(new URL(file, repoRoot));

export const fileExists = async (url) => {
  try {
    return (await Deno.stat(url)).isFile;
  } catch {
    return false;
  }
};

// The page's visible text: tags, scripts and styles removed, entities for spaces and arrows
// decoded, whitespace collapsed. Good enough for checking what a reader sees.
export const textOf = (html) =>
  html
    .replace(/<(script|style)[\s\S]*?<\/\1>/gi, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ")
    .replace(/&rarr;/g, "→")
    .replace(/&amp;/g, "&")
    .replace(/&copy;/g, "©")
    .replace(/&rsquo;/g, "'")
    .replace(/\s+/g, " ")
    .trim();

export const firstMatch = (html, pattern) => html.match(pattern)?.[0] ?? null;
