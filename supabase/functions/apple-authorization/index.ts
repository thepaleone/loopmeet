// Called by the app right after Sign in with Apple, with the user's access token (research D7).
// The service-role client only writes apple_authorizations, which has no client policies.
import { createClient } from "@supabase/supabase-js";
import { exchangeAuthorizationCode, readAppleEnv } from "../_shared/apple-auth.ts";
import { handleAppleAuthorization } from "./handler.ts";
import { parseOrigins, withCors } from "./cors.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

const appleEnv = readAppleEnv();

// The website's origins (017): the Account page calls this after a web Sign in with Apple.
const siteOrigins = parseOrigins(Deno.env.get("SITE_ORIGINS") ?? "");

Deno.serve(withCors((request) =>
  handleAppleAuthorization(request, {
    async getUser(jwt) {
      const { data, error } = await supabase.auth.getUser(jwt);
      return error || !data.user ? null : data.user.id;
    },
    exchangeCode: (code) => exchangeAuthorizationCode(appleEnv, code),
    async upsertAuthorization(row) {
      const { error } = await supabase
        .from("apple_authorizations")
        .upsert({ ...row, updated_at: new Date().toISOString() }, { onConflict: "user_id,client_id" });
      if (error) throw error;
    },
    nativeClientId: appleEnv.clientId,
    log(level, message, fields) {
      console.log(JSON.stringify({ level, message, ...fields }));
    },
  }), siteOrigins));
