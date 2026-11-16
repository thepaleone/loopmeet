// Sends recorded reports to the safety inbox (specs/023-safety-controls/contracts/report-email.md).
// index.ts wires the real ports; claiming, rendering, recording and logging live here so they can
// be tested with fakes.

export const BATCH_LIMIT = 25;

const REASON_LABELS: Record<string, string> = {
  inappropriate_photo_or_name: "Inappropriate photo or name",
  harassment_or_threats: "Harassment or threats",
  child_safety: "Child safety",
  spam: "Spam",
  other: "Other",
};

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export interface ReportRow {
  id: string;
  reporter_user_id: string;
  group_id: string;
  reported_user_id: string | null;
  reason: string;
  details: string;
  group_name: string;
  reporter_display_name: string;
  reported_display_name: string | null;
  received_at: string;
  email_attempts: number;
}

export interface StuckReport {
  id: string;
  received_at: string;
}

export interface OutgoingEmail {
  from: string;
  to: string;
  replyTo: string | null;
  subject: string;
  text: string;
}

export interface Addresses {
  from: string;
  to: string;
}

export type LogLevel = "info" | "warn" | "error";

export interface MailerPorts {
  claim(reportId: string | null, limit: number): Promise<ReportRow[]>;
  reporterEmail(userId: string): Promise<string | null>;
  // Resolves to the provider's HTTP status. Rejects when the request never got an answer.
  send(email: OutgoingEmail): Promise<number>;
  mark(reportId: string, errorCode: string | null): Promise<void>;
  stuck(): Promise<StuckReport[]>;
  log(level: LogLevel, message: string, fields: Record<string, unknown>): void;
}

export type MailerRequest = { kind: "one"; reportId: string } | { kind: "sweep" };

export interface MailerSummary {
  sent: number;
  failed: number;
  stuck: number;
}

// { report_id } from the insert trigger, or {} from the sweep. Anything else is refused.
export const parseRequest = (body: unknown): MailerRequest | null => {
  if (typeof body !== "object" || body === null || Array.isArray(body)) return null;
  const keys = Object.keys(body);
  if (keys.length === 0) return { kind: "sweep" };
  const reportId = (body as Record<string, unknown>).report_id;
  if (keys.length === 1 && typeof reportId === "string" && UUID.test(reportId)) {
    return { kind: "one", reportId };
  }
  return null;
};

const label = (reason: string): string => REASON_LABELS[reason] ?? reason;

// Plain text only, so report text can't inject markup. The subject carries no user-written text,
// so a hostile group name can't spoof it in the safety inbox.
export const renderEmail = (row: ReportRow, reporterEmail: string | null, addresses: Addresses): OutgoingEmail => {
  const reported = row.reported_user_id === null
    ? "The group itself"
    : `${row.reported_display_name ?? ""} (${row.reported_user_id})`;

  const text = [
    `Report ${row.id}`,
    `Received: ${row.received_at}`,
    `Reason: ${label(row.reason)}`,
    "",
    `Group: ${row.group_name} (${row.group_id})`,
    `Reported: ${reported}`,
    `Reporter: ${row.reporter_display_name} (${row.reporter_user_id}), ${reporterEmail ?? "email unavailable"}`,
    "",
    "Details:",
    row.details,
  ].join("\n");

  return {
    from: addresses.from,
    to: addresses.to,
    replyTo: reporterEmail,
    subject: `LoopMeet report ${row.id.slice(0, 8)}: ${label(row.reason)}`,
    text,
  };
};

const sendOne = async (ports: MailerPorts, row: ReportRow, addresses: Addresses): Promise<boolean> => {
  let errorCode: string | null;
  try {
    const email = renderEmail(row, await ports.reporterEmail(row.reporter_user_id), addresses);
    const status = await ports.send(email);
    errorCode = status === 202 ? null : `http_${status}`;
  } catch {
    errorCode = "network";
  }

  await ports.mark(row.id, errorCode);
  if (errorCode === null) {
    ports.log("info", "report_email_sent", { report_id: row.id });
    return true;
  }
  ports.log("warn", "report_email_failed", { report_id: row.id, error_code: errorCode, attempts: row.email_attempts });
  return false;
};

export const runMailer = async (
  ports: MailerPorts,
  request: MailerRequest,
  addresses: Addresses,
): Promise<MailerSummary> => {
  const rows = request.kind === "one" ? await ports.claim(request.reportId, 1) : await ports.claim(null, BATCH_LIMIT);

  let sent = 0;
  let failed = 0;
  for (const row of rows) {
    if (await sendOne(ports, row, addresses)) sent++;
    else failed++;
  }

  let stuck = 0;
  if (request.kind === "sweep") {
    for (const report of await ports.stuck()) {
      ports.log("error", "report_email_stuck", { report_id: report.id, received_at: report.received_at });
      stuck++;
    }
  }

  return { sent, failed, stuck };
};
