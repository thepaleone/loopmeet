import { createClient } from "@supabase/supabase-js";
import { type MailerPorts, parseRequest, type ReportRow, runMailer } from "./report-mailer.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

const SENDGRID_API_KEY = Deno.env.get("SENDGRID_API_KEY") ?? "";
const addresses = {
  from: Deno.env.get("REPORT_EMAIL_FROM") ?? "",
  to: Deno.env.get("REPORT_EMAIL_TO") ?? "",
};

const ports: MailerPorts = {
  async claim(reportId, limit) {
    const { data, error } = await supabase.rpc("claim_report_emails", { p_report_id: reportId, p_limit: limit });
    if (error) throw error;
    return (data ?? []) as ReportRow[];
  },

  async reporterEmail(userId) {
    const { data, error } = await supabase.from("user_profiles").select("email").eq("id", userId).maybeSingle();
    if (error) throw error;
    return typeof data?.email === "string" && data.email.length > 0 ? data.email : null;
  },

  async send(email) {
    const response = await fetch("https://api.sendgrid.com/v3/mail/send", {
      method: "POST",
      headers: { Authorization: `Bearer ${SENDGRID_API_KEY}`, "Content-Type": "application/json" },
      body: JSON.stringify({
        personalizations: [{ to: [{ email: email.to }] }],
        from: { email: email.from },
        ...(email.replyTo ? { reply_to: { email: email.replyTo } } : {}),
        subject: email.subject,
        content: [{ type: "text/plain", value: email.text }],
      }),
    });
    // The body may echo addresses, so it's discarded unread.
    await response.body?.cancel();
    return response.status;
  },

  async mark(reportId, errorCode) {
    const { error } = await supabase.rpc("mark_report_email", { p_report_id: reportId, p_error: errorCode });
    if (error) throw error;
  },

  async stuck() {
    const { data, error } = await supabase.rpc("stuck_report_emails");
    if (error) throw error;
    return (data ?? []).map((row: { id: string; received_at: string }) => ({ id: row.id, received_at: row.received_at }));
  },

  log(level, message, fields) {
    console.log(JSON.stringify({ level, message, ...fields }));
  },
};

Deno.serve(async (request) => {
  if (request.method !== "POST") {
    return Response.json({ error: "method_not_allowed" }, { status: 405 });
  }

  const mailerRequest = parseRequest(await request.json().catch(() => null));
  if (mailerRequest === null) {
    return Response.json({ error: "invalid_request" }, { status: 400 });
  }

  try {
    return Response.json(await runMailer(ports, mailerRequest, addresses));
  } catch (err) {
    const code = typeof err === "object" && err !== null && "code" in err ? String(err.code) : "exception";
    console.log(JSON.stringify({ level: "error", message: "report_email_claim_failed", error_code: code }));
    return Response.json({ error: "claim_failed" }, { status: 500 });
  }
});
