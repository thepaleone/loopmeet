import type { SupabaseClient } from "npm:@supabase/supabase-js@2";
import { isAccountActive } from "../_shared/account-status.ts";

export interface RecipientResolverInput {
  table: string;
  record: Record<string, unknown>;
}

export const resolveRecipients = async (
  client: SupabaseClient,
  { table, record }: RecipientResolverInput,
): Promise<string[]> => {
  if (table === "invitations") {
    const invitedUserId = record.invited_user_id;
    if (typeof invitedUserId !== "string") {
      return [];
    }

    // A user pending deletion gets no pushes (FR-008).
    return await isAccountActive(client, invitedUserId) ? [invitedUserId] : [];
  }

  if (table === "meetups") {
    const groupId = record.group_id;
    if (typeof groupId !== "string") {
      return [];
    }

    // active_memberships leaves out pending members and every group with a pending owner.
    const { data, error } = await client
      .from("active_memberships")
      .select("member_user_id")
      .eq("group_id", groupId);

    if (error) {
      throw new Error(`resolve_recipients_failed:${error.message}`);
    }

    return ((data ?? []) as { member_user_id: unknown }[])
      .map((row) => row.member_user_id)
      .filter((value): value is string => typeof value === "string");
  }

  return [];
};
