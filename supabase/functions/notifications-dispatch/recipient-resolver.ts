import { createClient } from "@supabase/supabase-js";

export interface RecipientResolverInput {
  table: string;
  record: Record<string, unknown>;
}

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

export const resolveRecipients = async ({ table, record }: RecipientResolverInput): Promise<string[]> => {
  if (table === "invitations") {
    const invitedUserId = record.invited_user_id;
    return typeof invitedUserId === "string" ? [invitedUserId] : [];
  }

  if (table === "meetups") {
    const groupId = record.group_id;
    if (typeof groupId !== "string") {
      return [];
    }

    const { data, error } = await supabase
      .from("memberships")
      .select("member_user_id")
      .eq("group_id", groupId);

    if (error) {
      throw new Error(`resolve_recipients_failed:${error.message}`);
    }

    return (data ?? [])
      .map((row) => row.member_user_id)
      .filter((value): value is string => typeof value === "string");
  }

  return [];
};
