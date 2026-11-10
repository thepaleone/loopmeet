// Generates the Secret Key for the Supabase Apple provider, which web Sign in with Apple (017) needs.
// Apple caps it at 6 months, so rerun this before the last one expires and paste the output into
// Supabase → Authentication → Sign In / Providers → Apple → Secret Key (for OAuth).
//
//   deno run --allow-read=<path to AuthKey_XXXXXXXXXX.p8> supabase/functions/_tools/apple-client-secret.ts \
//     --team-id <TEAM_ID> --key-id <KEY_ID> --client-id <SERVICES_ID> --key-file <path to .p8>
//
// The secret goes to stdout and its expiry to stderr. Nothing is written to disk.
import { parseArgs } from "jsr:@std/cli@1/parse-args";
import { createClientSecret, MAX_CLIENT_SECRET_LIFETIME_SECONDS } from "../_shared/apple-auth.ts";

export interface Options {
  teamId: string;
  keyId: string;
  clientId: string;
  keyFile: string;
}

export const USAGE =
  "Usage: apple-client-secret.ts --team-id <TEAM_ID> --key-id <KEY_ID> --client-id <SERVICES_ID> --key-file <AuthKey.p8>";

// Apple team and key IDs are 10 uppercase letters and digits.
const APPLE_ID = /^[A-Z0-9]{10}$/;

export const parseOptions = (args: string[]): Options => {
  const parsed = parseArgs(args, { string: ["team-id", "key-id", "client-id", "key-file"] });
  const options = {
    teamId: parsed["team-id"]?.trim() ?? "",
    keyId: parsed["key-id"]?.trim() ?? "",
    clientId: parsed["client-id"]?.trim() ?? "",
    keyFile: parsed["key-file"]?.trim() ?? "",
  };

  const missing = Object.entries({
    "--team-id": options.teamId,
    "--key-id": options.keyId,
    "--client-id": options.clientId,
    "--key-file": options.keyFile,
  }).filter(([, value]) => value === "").map(([flag]) => flag);
  if (missing.length > 0) throw new Error(`Missing ${missing.join(", ")}.\n${USAGE}`);

  if (!APPLE_ID.test(options.teamId)) throw new Error("--team-id must be 10 letters and digits.");
  if (!APPLE_ID.test(options.keyId)) throw new Error("--key-id must be 10 letters and digits.");
  return options;
};

export const generate = async (
  options: Options,
  readTextFile: (path: string) => Promise<string>,
  now: Date,
): Promise<{ secret: string; expiresAt: Date }> => {
  const privateKey = await readTextFile(options.keyFile);
  if (!privateKey.includes("-----BEGIN PRIVATE KEY-----")) {
    throw new Error(`${options.keyFile} isn't a .p8 private key.`);
  }

  const env = { teamId: options.teamId, keyId: options.keyId, privateKey, clientId: options.clientId };
  const secret = await createClientSecret(env, now, options.clientId, MAX_CLIENT_SECRET_LIFETIME_SECONDS);
  const issuedAt = Math.floor(now.getTime() / 1000);
  return { secret, expiresAt: new Date((issuedAt + MAX_CLIENT_SECRET_LIFETIME_SECONDS) * 1000) };
};

if (import.meta.main) {
  try {
    const { secret, expiresAt } = await generate(parseOptions(Deno.args), Deno.readTextFile, new Date());
    console.log(secret);
    console.error(`Expires ${expiresAt.toISOString()}. Generate a new one before then.`);
  } catch (error) {
    console.error(error instanceof Error ? error.message : String(error));
    Deno.exit(1);
  }
}
