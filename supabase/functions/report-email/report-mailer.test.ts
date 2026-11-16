import { assert, assertEquals, assertFalse, assertStringIncludes } from "jsr:@std/assert@1";
import {
  BATCH_LIMIT,
  type LogLevel,
  type MailerPorts,
  type OutgoingEmail,
  parseRequest,
  renderEmail,
  type ReportRow,
  runMailer,
} from "./report-mailer.ts";

const ADDRESSES = { from: "noreply@loopmeet.io", to: "abuse@loopmeet.io" };

const row = (overrides: Partial<ReportRow> = {}): ReportRow => ({
  id: "3f2a9c41-0000-4000-8000-000000000001",
  reporter_user_id: "00000000-0000-0000-0000-00000000000b",
  group_id: "00000000-0000-0000-0000-0000000000a1",
  reported_user_id: "00000000-0000-0000-0000-00000000000a",
  reason: "harassment_or_threats",
  details: "He keeps sending threats.",
  group_name: "Book club",
  reporter_display_name: "Ben",
  reported_display_name: "Ann",
  received_at: "2026-11-16T10:00:00.000Z",
  email_attempts: 1,
  ...overrides,
});

interface LogLine {
  level: LogLevel;
  message: string;
  fields: Record<string, unknown>;
}

class FakePorts implements MailerPorts {
  rows: ReportRow[] = [];
  readonly emails = new Map<string, string>();
  readonly sent: OutgoingEmail[] = [];
  readonly marks: Array<{ id: string; error: string | null }> = [];
  readonly claims: Array<{ reportId: string | null; limit: number }> = [];
  readonly logs: LogLine[] = [];
  statusFor: (email: OutgoingEmail) => number = () => 202;
  networkFailFor = new Set<string>();
  stuckRows: Array<{ id: string; received_at: string }> = [];
  stuckCalls = 0;

  claim(reportId: string | null, limit: number) {
    this.claims.push({ reportId, limit });
    return Promise.resolve(this.rows.filter((r) => reportId === null || r.id === reportId));
  }

  reporterEmail(userId: string) {
    return Promise.resolve(this.emails.get(userId) ?? null);
  }

  send(email: OutgoingEmail) {
    this.sent.push(email);
    if ([...this.networkFailFor].some((id) => email.text.includes(id))) {
      return Promise.reject(new TypeError("network down for b@example.com"));
    }
    return Promise.resolve(this.statusFor(email));
  }

  mark(id: string, error: string | null) {
    this.marks.push({ id, error });
    return Promise.resolve();
  }

  stuck() {
    this.stuckCalls++;
    return Promise.resolve(this.stuckRows);
  }

  log(level: LogLevel, message: string, fields: Record<string, unknown>) {
    this.logs.push({ level, message, fields });
  }
}

Deno.test("the email carries everything needed to act on the report", () => {
  const email = renderEmail(row(), "b@example.com", ADDRESSES);

  assertEquals(email.from, "noreply@loopmeet.io");
  assertEquals(email.to, "abuse@loopmeet.io");
  assertEquals(email.replyTo, "b@example.com");
  for (
    const expected of [
      "Report 3f2a9c41-0000-4000-8000-000000000001",
      "Received: 2026-11-16T10:00:00.000Z",
      "Reason: Harassment or threats",
      "Group: Book club (00000000-0000-0000-0000-0000000000a1)",
      "Reported: Ann (00000000-0000-0000-0000-00000000000a)",
      "Reporter: Ben (00000000-0000-0000-0000-00000000000b), b@example.com",
      "Details:\nHe keeps sending threats.",
    ]
  ) {
    assertStringIncludes(email.text, expected);
  }
});

Deno.test("the subject holds the short id and reason, never user-written text", () => {
  const email = renderEmail(row({ group_name: "Spoofed: urgent", details: "Click here" }), null, ADDRESSES);

  assertEquals(email.subject, "LoopMeet report 3f2a9c41: Harassment or threats");
  assertFalse(email.subject.includes("Spoofed"));
  assertFalse(email.subject.includes("Click"));
});

Deno.test("a report about the group itself, from a reporter with no email", () => {
  const email = renderEmail(row({ reported_user_id: null, reported_display_name: null }), null, ADDRESSES);

  assertStringIncludes(email.text, "Reported: The group itself");
  assertStringIncludes(email.text, "Reporter: Ben (00000000-0000-0000-0000-00000000000b), email unavailable");
  assertEquals(email.replyTo, null);
});

Deno.test("every reason has a label", () => {
  const labels = ["inappropriate_photo_or_name", "harassment_or_threats", "child_safety", "spam", "other"]
    .map((reason) => renderEmail(row({ reason }), null, ADDRESSES).subject);

  assertEquals(labels, [
    "LoopMeet report 3f2a9c41: Inappropriate photo or name",
    "LoopMeet report 3f2a9c41: Harassment or threats",
    "LoopMeet report 3f2a9c41: Child safety",
    "LoopMeet report 3f2a9c41: Spam",
    "LoopMeet report 3f2a9c41: Other",
  ]);
});

Deno.test("parseRequest accepts one report id or an empty sweep body, and nothing else", () => {
  assertEquals(parseRequest({ report_id: "3f2a9c41-0000-4000-8000-000000000001" }), {
    kind: "one",
    reportId: "3f2a9c41-0000-4000-8000-000000000001",
  });
  assertEquals(parseRequest({}), { kind: "sweep" });
  assertEquals(parseRequest({ report_id: "not-a-uuid" }), null);
  assertEquals(parseRequest({ report_id: "3f2a9c41-0000-4000-8000-000000000001", extra: 1 }), null);
  assertEquals(parseRequest(null), null);
  assertEquals(parseRequest([]), null);
});

Deno.test("a 202 marks the report sent", async () => {
  const ports = new FakePorts();
  ports.rows = [row()];
  ports.emails.set(row().reporter_user_id, "b@example.com");

  const summary = await runMailer(ports, { kind: "one", reportId: row().id }, ADDRESSES);

  assertEquals(summary, { sent: 1, failed: 0, stuck: 0 });
  assertEquals(ports.claims, [{ reportId: row().id, limit: 1 }]);
  assertEquals(ports.marks, [{ id: row().id, error: null }]);
  assertEquals(ports.stuckCalls, 0, "a single-report call does not check for stuck reports");
});

Deno.test("a failed send is recorded and the next report still goes", async () => {
  const ports = new FakePorts();
  const first = row({ id: "3f2a9c41-0000-4000-8000-000000000001" });
  const second = row({ id: "3f2a9c41-0000-4000-8000-000000000002" });
  const third = row({ id: "3f2a9c41-0000-4000-8000-000000000003" });
  ports.rows = [first, second, third];
  ports.statusFor = (email) => email.text.includes(first.id) ? 401 : 202;
  ports.networkFailFor.add(second.id);

  const summary = await runMailer(ports, { kind: "sweep" }, ADDRESSES);

  assertEquals(summary, { sent: 1, failed: 2, stuck: 0 });
  assertEquals(ports.claims, [{ reportId: null, limit: BATCH_LIMIT }]);
  const failures = ports.logs.filter((line) => line.message === "report_email_failed").map((line) => line.fields);
  assertEquals(failures, [
    { report_id: first.id, error_code: "http_401", attempts: 1 },
    { report_id: second.id, error_code: "network", attempts: 1 },
  ]);
  assertEquals(ports.marks, [
    { id: first.id, error: "http_401" },
    { id: second.id, error: "network" },
    { id: third.id, error: null },
  ]);
});

Deno.test("a sweep logs each stuck report once", async () => {
  const ports = new FakePorts();
  ports.stuckRows = [{ id: row().id, received_at: row().received_at }];

  const summary = await runMailer(ports, { kind: "sweep" }, ADDRESSES);

  assertEquals(summary, { sent: 0, failed: 0, stuck: 1 });
  const stuck = ports.logs.filter((line) => line.message === "report_email_stuck");
  assertEquals(stuck.length, 1);
  assertEquals(stuck[0].level, "error");
  assertEquals(stuck[0].fields, { report_id: row().id, received_at: row().received_at });
});

Deno.test("logs carry ids and codes only: no email, name or details", async () => {
  const ports = new FakePorts();
  const first = row({ id: "3f2a9c41-0000-4000-8000-000000000001" });
  const second = row({ id: "3f2a9c41-0000-4000-8000-000000000002" });
  ports.rows = [first, second];
  ports.emails.set(first.reporter_user_id, "b@example.com");
  ports.networkFailFor.add(second.id);
  ports.stuckRows = [{ id: first.id, received_at: first.received_at }];

  await runMailer(ports, { kind: "sweep" }, ADDRESSES);

  assert(ports.logs.length >= 3);
  const logged = JSON.stringify(ports.logs);
  for (const forbidden of ["b@example.com", "Ben", "Ann", "Book club", "threats", "@loopmeet.io", "network down"]) {
    assertFalse(logged.includes(forbidden), `logs contain ${forbidden}`);
  }
  assertEquals(
    ports.logs.map((line) => line.message),
    ["report_email_sent", "report_email_failed", "report_email_stuck"],
  );
});
