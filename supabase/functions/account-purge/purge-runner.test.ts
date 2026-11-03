import { assert, assertEquals } from "jsr:@std/assert@1";
import {
  type AppleAuthorizationRow,
  type ClaimedDeletion,
  type LogLevel,
  PURGE_STEPS,
  type PurgePorts,
  type PurgeStep,
  runPurge,
} from "./purge-runner.ts";

interface LogLine {
  level: LogLevel;
  message: string;
  fields: Record<string, unknown>;
}

// An in-memory world: deletion rows plus what each external system still holds.
class FakeWorld implements PurgePorts {
  readonly rows = new Map<string, ClaimedDeletion>();
  readonly calls: string[] = [];
  readonly logs: LogLine[] = [];
  readonly failAt = new Map<string, PurgeStep>();
  oneSignalStatus = 202;
  readonly apple = new Map<string, AppleAuthorizationRow[]>();
  readonly appleIdentities = new Set<string>();
  readonly avatars = new Map<string, string[]>();
  avatarPageSize = 100;

  addUser(userId: string, completed: string[] = [], attempts = 1, lastError: string | null = null) {
    this.rows.set(userId, { user_id: userId, completed_steps: [...completed], attempts, last_error: lastError });
  }

  private maybeFail(userId: string, step: PurgeStep) {
    this.calls.push(`${step}:${userId}`);
    if (this.failAt.get(userId) === step) {
      this.failAt.delete(userId);
      throw Object.assign(new Error(`boom for ${userId}@example.com`), { status: 503 });
    }
  }

  claim(_limit: number) {
    return Promise.resolve([...this.rows.values()].map((row) => ({ ...row, completed_steps: [...row.completed_steps] })));
  }

  recordStep(userId: string, step: PurgeStep, errorCode: string | null) {
    const row = this.rows.get(userId);
    if (row) {
      if (errorCode === null && !row.completed_steps.includes(step)) row.completed_steps.push(step);
      if (errorCode !== null) row.last_error = errorCode;
    }
    return Promise.resolve();
  }

  deleteOneSignalUser(userId: string) {
    this.maybeFail(userId, "onesignal");
    return Promise.resolve(this.oneSignalStatus);
  }

  listAppleAuthorizations(userId: string) {
    this.maybeFail(userId, "apple");
    return Promise.resolve([...(this.apple.get(userId) ?? [])]);
  }

  revokeApple(clientId: string, refreshToken: string) {
    this.calls.push(`revoke:${clientId}:${refreshToken}`);
    return Promise.resolve();
  }

  deleteAppleAuthorization(userId: string, clientId: string) {
    this.apple.set(userId, (this.apple.get(userId) ?? []).filter((row) => row.client_id !== clientId));
    return Promise.resolve();
  }

  hasAppleIdentity(userId: string) {
    return Promise.resolve(this.appleIdentities.has(userId));
  }

  listAvatarObjects(userId: string) {
    this.maybeFail(userId, "storage");
    return Promise.resolve((this.avatars.get(userId) ?? []).slice(0, this.avatarPageSize));
  }

  removeAvatarObjects(paths: string[]) {
    for (const [userId, objects] of this.avatars) {
      this.avatars.set(userId, objects.filter((path) => !paths.includes(path)));
    }
    return Promise.resolve();
  }

  purgeDatabase(userId: string) {
    this.maybeFail(userId, "database");
    return Promise.resolve();
  }

  deleteAuthUser(userId: string) {
    this.maybeFail(userId, "auth");
    // The cascade from auth.users removes the deletion row.
    this.rows.delete(userId);
    return Promise.resolve();
  }

  log(level: LogLevel, message: string, fields: Record<string, unknown>) {
    this.logs.push({ level, message, fields });
  }

  stepCalls(userId: string) {
    return this.calls.filter((call) => call.endsWith(`:${userId}`)).map((call) => call.split(":")[0]);
  }
}

Deno.test("a fresh row runs every step in order and is gone afterwards", async () => {
  const world = new FakeWorld();
  world.addUser("u1");

  const summary = await runPurge(world);

  assertEquals(world.stepCalls("u1"), [...PURGE_STEPS]);
  assertEquals(world.rows.has("u1"), false);
  assertEquals(summary, { ok: true, claimed: 1, completed: 1, failed: 0 });
  assertEquals(
    world.logs.map((line) => line.message),
    [
      "account_purge_started",
      ...PURGE_STEPS.map(() => "account_purge_step_completed"),
      "account_purge_completed",
    ],
  );
});

for (const [index, step] of PURGE_STEPS.entries()) {
  Deno.test(`a failure at ${step} is recorded and the next run resumes there`, async () => {
    const world = new FakeWorld();
    world.addUser("u1");
    world.failAt.set("u1", step);

    const first = await runPurge(world);

    assertEquals(first, { ok: true, claimed: 1, completed: 0, failed: 1 });
    assertEquals(world.rows.get("u1")?.completed_steps, PURGE_STEPS.slice(0, index));
    assertEquals(world.rows.get("u1")?.last_error, `${step}_503`);
    assert(world.logs.some((line) =>
      line.message === "account_purge_step_failed" && line.fields.step === step && line.fields.error_code === `${step}_503`
    ));

    world.calls.length = 0;
    world.logs.length = 0;
    const second = await runPurge(world);

    assertEquals(world.stepCalls("u1"), PURGE_STEPS.slice(index));
    assertEquals(world.logs[0].fields.resume_from, step);
    assertEquals(second.completed, 1);
    assertEquals(world.rows.has("u1"), false);
  });
}

Deno.test("OneSignal 404 counts as done, other statuses fail with the status", async () => {
  const gone = new FakeWorld();
  gone.addUser("u1");
  gone.oneSignalStatus = 404;
  assertEquals((await runPurge(gone)).completed, 1);

  const down = new FakeWorld();
  down.addUser("u1");
  down.oneSignalStatus = 500;
  assertEquals((await runPurge(down)).failed, 1);
  assertEquals(down.rows.get("u1")?.last_error, "onesignal_500");
});

Deno.test("an avatar folder listed over two pages is emptied", async () => {
  const world = new FakeWorld();
  world.addUser("u1");
  world.avatarPageSize = 2;
  world.avatars.set("u1", ["u1/a.jpg", "u1/b.jpg", "u1/c.jpg"]);
  world.avatars.set("u2", ["u2/a.jpg"]);

  await runPurge(world);

  assertEquals(world.avatars.get("u1"), []);
  assertEquals(world.avatars.get("u2"), ["u2/a.jpg"]);
  assertEquals(world.stepCalls("u1").filter((step) => step === "storage").length, 3);
});

Deno.test("every stored Apple token is revoked and its row deleted", async () => {
  const world = new FakeWorld();
  world.addUser("u1");
  world.appleIdentities.add("u1");
  world.apple.set("u1", [
    { client_id: "io.loopmeet.app", refresh_token: "r-app" },
    { client_id: "io.loopmeet.web", refresh_token: "r-web" },
  ]);

  await runPurge(world);

  assert(world.calls.includes("revoke:io.loopmeet.app:r-app"));
  assert(world.calls.includes("revoke:io.loopmeet.web:r-web"));
  assertEquals(world.apple.get("u1"), []);
  assertEquals(world.logs.some((line) => line.message === "apple_token_unavailable"), false);
});

Deno.test("an Apple user with no stored token is logged and the purge completes", async () => {
  const world = new FakeWorld();
  world.addUser("u1");
  world.addUser("u2");
  world.appleIdentities.add("u1");

  const summary = await runPurge(world);

  assertEquals(summary.completed, 2);
  assertEquals(
    world.logs.filter((line) => line.message === "apple_token_unavailable").map((line) => line.fields),
    [{ user_id: "u1" }],
  );
});

Deno.test("account_purge_stuck is logged at error level from 24 attempts", async () => {
  const world = new FakeWorld();
  world.addUser("u1", ["onesignal"], 24, "apple_503");
  world.addUser("u2", [], 23);

  await runPurge(world);

  const stuck = world.logs.filter((line) => line.message === "account_purge_stuck");
  assertEquals(stuck.length, 1);
  assertEquals(stuck[0].level, "error");
  assertEquals(stuck[0].fields, { user_id: "u1", attempts: 24, last_error: "apple_503" });
});

Deno.test("one user's failure doesn't stop the others, and the summary counts both", async () => {
  const world = new FakeWorld();
  world.addUser("u1");
  world.addUser("u2");
  world.addUser("u3");
  world.failAt.set("u2", "database");

  const summary = await runPurge(world);

  assertEquals(summary, { ok: true, claimed: 3, completed: 2, failed: 1 });
  assertEquals(world.rows.has("u1"), false);
  assertEquals(world.rows.has("u2"), true);
  assertEquals(world.rows.has("u3"), false);
});

Deno.test("no log line contains an email, even when an error message does", async () => {
  const world = new FakeWorld();
  world.addUser("u1");
  world.addUser("u2", [], 30, "storage_503");
  world.failAt.set("u1", "auth");
  world.appleIdentities.add("u2");

  await runPurge(world);

  for (const line of world.logs) {
    assert(!JSON.stringify(line).includes("@"), `log line leaks an email: ${JSON.stringify(line)}`);
  }
});

Deno.test("nothing claimed means an empty summary", async () => {
  const world = new FakeWorld();

  assertEquals(await runPurge(world), { ok: true, claimed: 0, completed: 0, failed: 0 });
  assertEquals(world.logs, []);
});
