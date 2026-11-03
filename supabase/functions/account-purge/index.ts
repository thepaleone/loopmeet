// Permanent deletion, run hourly by the account-purge-hourly cron job (research D4). This is the
// only place the service-role key deletes user data (FR-019); see purge-runner.ts for the steps.
import { createClient } from "@supabase/supabase-js";
import { readAppleEnv, revokeRefreshToken } from "../_shared/apple-auth.ts";
import { type PurgePorts, PurgeStepError, runPurge } from "./purge-runner.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

const ONESIGNAL_APP_ID = Deno.env.get("ONESIGNAL_APP_ID") ?? "";
const ONESIGNAL_REST_API_KEY = Deno.env.get("ONESIGNAL_REST_API_KEY") ?? "";
// Must match the Api's Supabase:AvatarBucketName (research D14).
const AVATAR_BUCKET_NAME = Deno.env.get("AVATAR_BUCKET_NAME") ?? "";
const AVATAR_PAGE_SIZE = 100;

const appleEnv = readAppleEnv();

const avatarBucket = () => {
  if (AVATAR_BUCKET_NAME === "") throw new PurgeStepError("bucket_unset");
  return supabase.storage.from(AVATAR_BUCKET_NAME);
};

const ports: PurgePorts = {
  async claim(limit) {
    const { data, error } = await supabase.rpc("claim_due_account_purges", { p_limit: limit });
    if (error) throw error;
    return data ?? [];
  },

  async recordStep(userId, step, errorCode) {
    const { error } = await supabase.rpc("record_account_purge_step", {
      p_user_id: userId,
      p_step: step,
      p_error: errorCode,
    });
    if (error) throw error;
  },

  async deleteOneSignalUser(userId) {
    const response = await fetch(
      `https://api.onesignal.com/apps/${ONESIGNAL_APP_ID}/users/by/external_id/${userId}`,
      { method: "DELETE", headers: { Authorization: `Key ${ONESIGNAL_REST_API_KEY}` } },
    );
    await response.body?.cancel();
    return response.status;
  },

  async listAppleAuthorizations(userId) {
    const { data, error } = await supabase
      .from("apple_authorizations")
      .select("client_id, refresh_token")
      .eq("user_id", userId);
    if (error) throw error;
    return data ?? [];
  },

  revokeApple(clientId, refreshToken) {
    return revokeRefreshToken(appleEnv, clientId, refreshToken);
  },

  async deleteAppleAuthorization(userId, clientId) {
    const { error } = await supabase
      .from("apple_authorizations")
      .delete()
      .eq("user_id", userId)
      .eq("client_id", clientId);
    if (error) throw error;
  },

  async hasAppleIdentity(userId) {
    const { data, error } = await supabase.auth.admin.getUserById(userId);
    if (error) {
      if (error.status === 404) return false;
      throw error;
    }
    return data.user.identities?.some((identity) => identity.provider === "apple") ?? false;
  },

  async listAvatarObjects(userId) {
    const { data, error } = await avatarBucket().list(userId, { limit: AVATAR_PAGE_SIZE });
    if (error) throw error;
    return (data ?? []).map((object) => `${userId}/${object.name}`);
  },

  async removeAvatarObjects(paths) {
    const { error } = await avatarBucket().remove(paths);
    if (error) throw error;
  },

  async purgeDatabase(userId) {
    const { error } = await supabase.rpc("purge_account_data", { p_user_id: userId });
    if (error) throw error;
  },

  async deleteAuthUser(userId) {
    const { error } = await supabase.auth.admin.deleteUser(userId);
    // 404: an earlier run deleted the user but didn't get to record it.
    if (error && error.status !== 404) throw error;
  },

  log(level, message, fields) {
    console.log(JSON.stringify({ level, message, ...fields }));
  },
};

Deno.serve(async (request) => {
  if (request.method !== "POST") {
    return Response.json({ error: "method_not_allowed" }, { status: 405 });
  }

  try {
    return Response.json(await runPurge(ports));
  } catch (err) {
    const code = typeof err === "object" && err !== null && "code" in err ? String(err.code) : "exception";
    console.log(JSON.stringify({ level: "error", message: "account_purge_claim_failed", error_code: code }));
    return Response.json({ ok: false, error: "claim_failed" }, { status: 500 });
  }
});
