import { createClient, type SupabaseClient } from "@supabase/supabase-js";
import { OneSignalClient } from "./onesignal-client.ts";
import { notificationMappingRegistry } from "./notification-mapping-registry.ts";
import type { NotificationPayload, NotificationType } from "./notification-contract.ts";

const destinationMap = notificationMappingRegistry;

export interface SendOptions {
  notificationType: NotificationType;
  eventId: string;
  externalUserId: string;
  title: string;
  body: string;
  targetId: string | null;
  sourceTable: string;
  sourceRowId: string;
  occurredAt?: string;
}

export interface SendResult {
  status: "sent" | "skipped" | "failed";
  reason?: string;
  providerMessageId?: string;
}

const buildPayload = (
  notificationType: NotificationType,
  targetId: string | null,
  eventId: string,
  sentAt: string,
): NotificationPayload => {
  const destination = destinationMap[notificationType];
  return {
    notification_type: notificationType,
    target_kind: destination.targetKind,
    target_id: targetId,
    fallback_route: destination.fallbackRoute,
    event_id: eventId,
    sent_at: sentAt,
  };
};

export class NotificationDispatcher {
  private readonly supabase: SupabaseClient;
  private readonly oneSignal: OneSignalClient;
  private readonly appId: string;

  constructor() {
    this.supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
    );
    this.oneSignal = new OneSignalClient(Deno.env.get("ONESIGNAL_REST_API_KEY") ?? "");
    this.appId = Deno.env.get("ONESIGNAL_APP_ID") ?? "";
  }

  /**
   * Sends a notification to a single recipient with per-recipient idempotency.
   * If an active delivery_attempt for (eventId, userId) already exists, the send is skipped.
   */
  async sendForRecipient(options: SendOptions): Promise<SendResult> {
    const sentAt = options.occurredAt ?? new Date().toISOString();
    const correlationId = crypto.randomUUID();

    const destination = destinationMap[options.notificationType];

    // Ensure the notification_events row exists.
    const eventRow = await this.upsertEvent({
      eventId: options.eventId,
      notificationType: options.notificationType,
      targetKind: destination.targetKind,
      targetId: options.targetId,
      fallbackRoute: destination.fallbackRoute,
      sourceTable: options.sourceTable,
      sourceRowId: options.sourceRowId,
      occurredAt: sentAt,
      payload: buildPayload(options.notificationType, options.targetId, options.eventId, sentAt),
    });

    if (!eventRow) {
      return { status: "failed", reason: "event_upsert_failed" };
    }

    // Idempotency: skip if we have already sent for this (event_id, user_id).
    const existing = await this.supabase
      .from("notification_delivery_attempts")
      .select("id,status")
      .eq("notification_event_id", eventRow.id)
      .eq("user_id", options.externalUserId)
      .maybeSingle();

    if (existing.data) {
      return { status: "skipped", reason: "already_attempted" };
    }

    const payload = buildPayload(options.notificationType, options.targetId, options.eventId, sentAt);

    console.log(JSON.stringify({
      level: "info",
      message: "reminder_dispatch_start",
      correlation_id: correlationId,
      event_id: options.eventId,
      notification_type: options.notificationType,
      external_user_id: options.externalUserId,
    }));

    try {
      const response = await this.oneSignal.send({
        app_id: this.appId,
        include_external_user_ids: [options.externalUserId],
        headings: { en: options.title },
        contents: { en: options.body },
        data: payload,
      });

      await this.supabase.from("notification_delivery_attempts").insert({
        notification_event_id: eventRow.id,
        user_id: options.externalUserId,
        onesignal_message_id: response.id ?? null,
        status: response.errors ? "failed" : "sent",
        provider_response: response as unknown as Record<string, unknown>,
        attempted_at: new Date().toISOString(),
      });

      if (response.errors) {
        return { status: "failed", reason: "provider_errors", providerMessageId: response.id };
      }
      return { status: "sent", providerMessageId: response.id };
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      await this.supabase.from("notification_delivery_attempts").insert({
        notification_event_id: eventRow.id,
        user_id: options.externalUserId,
        status: "failed",
        error_code: message.slice(0, 200),
        attempted_at: new Date().toISOString(),
      });
      console.log(JSON.stringify({
        level: "error",
        message: "reminder_dispatch_failed",
        correlation_id: correlationId,
        event_id: options.eventId,
        error: message,
      }));
      return { status: "failed", reason: message };
    }
  }

  private async upsertEvent(params: {
    eventId: string;
    notificationType: NotificationType;
    targetKind: string;
    targetId: string | null;
    fallbackRoute: string;
    sourceTable: string;
    sourceRowId: string;
    occurredAt: string;
    payload: NotificationPayload;
  }): Promise<{ id: string } | null> {
    const existing = await this.supabase
      .from("notification_events")
      .select("id")
      .eq("event_id", params.eventId)
      .maybeSingle();

    if (existing.data) {
      return { id: existing.data.id as string };
    }

    const inserted = await this.supabase
      .from("notification_events")
      .insert({
        event_id: params.eventId,
        notification_type: params.notificationType,
        target_kind: params.targetKind,
        target_id: params.targetId,
        fallback_route: params.fallbackRoute,
        source_table: params.sourceTable,
        source_row_id: params.sourceRowId,
        occurred_at: params.occurredAt,
        payload_json: params.payload,
      })
      .select("id")
      .maybeSingle();

    if (inserted.error) {
      // Race: another invocation inserted the same event_id.
      const retry = await this.supabase
        .from("notification_events")
        .select("id")
        .eq("event_id", params.eventId)
        .maybeSingle();
      return retry.data ? { id: retry.data.id as string } : null;
    }

    return inserted.data ? { id: inserted.data.id as string } : null;
  }
}
