// Orchestrates permanent deletion (research D5). index.ts wires the real ports; everything that
// decides order, resumption and recording lives here so it can be tested with fakes.

export const PURGE_STEPS = ["onesignal", "apple", "storage", "database", "auth"] as const;
export type PurgeStep = typeof PURGE_STEPS[number];

export const CLAIM_LIMIT = 10;
// About a day of hourly failures.
export const STUCK_ATTEMPTS = 24;
// A storage folder that is still non-empty after this many list/remove rounds fails the step.
const MAX_STORAGE_ROUNDS = 50;

export interface ClaimedDeletion {
  user_id: string;
  completed_steps: string[];
  attempts: number;
  last_error: string | null;
}

export interface AppleAuthorizationRow {
  client_id: string;
  refresh_token: string;
}

export type LogLevel = "info" | "warn" | "error";

export interface PurgePorts {
  claim(limit: number): Promise<ClaimedDeletion[]>;
  recordStep(userId: string, step: PurgeStep, errorCode: string | null): Promise<void>;
  // Returns OneSignal's HTTP status.
  deleteOneSignalUser(userId: string): Promise<number>;
  listAppleAuthorizations(userId: string): Promise<AppleAuthorizationRow[]>;
  revokeApple(clientId: string, refreshToken: string): Promise<void>;
  deleteAppleAuthorization(userId: string, clientId: string): Promise<void>;
  hasAppleIdentity(userId: string): Promise<boolean>;
  // One page of object paths under the user's avatar folder.
  listAvatarObjects(userId: string): Promise<string[]>;
  removeAvatarObjects(paths: string[]): Promise<void>;
  purgeDatabase(userId: string): Promise<void>;
  // Resolves when the user is deleted or already gone.
  deleteAuthUser(userId: string): Promise<void>;
  log(level: LogLevel, message: string, fields: Record<string, unknown>): void;
}

export interface PurgeSummary {
  ok: true;
  claimed: number;
  completed: number;
  failed: number;
}

// Thrown by a step whose outcome wasn't an exception, such as an unexpected HTTP status.
export class PurgeStepError extends Error {
  constructor(readonly code: string) {
    super(code);
  }
}

// An error *code* only: messages and provider bodies can carry emails or tokens.
const errorCode = (err: unknown): string => {
  if (err instanceof PurgeStepError) return err.code;
  if (typeof err === "object" && err !== null) {
    const { status, code } = err as { status?: unknown; code?: unknown };
    if (typeof status === "number") return String(status);
    if (typeof code === "string" && /^[A-Za-z0-9_]{1,40}$/.test(code)) return code;
  }
  return "exception";
};

const runStep = async (ports: PurgePorts, userId: string, step: PurgeStep): Promise<void> => {
  switch (step) {
    case "onesignal": {
      const status = await ports.deleteOneSignalUser(userId);
      // 404: OneSignal never had this user, or already deleted them.
      if (!(status >= 200 && status < 300) && status !== 404) throw new PurgeStepError(String(status));
      return;
    }
    case "apple": {
      const authorizations = await ports.listAppleAuthorizations(userId);
      if (authorizations.length === 0) {
        if (await ports.hasAppleIdentity(userId)) {
          ports.log("warn", "apple_token_unavailable", { user_id: userId });
        }
        return;
      }
      for (const authorization of authorizations) {
        await ports.revokeApple(authorization.client_id, authorization.refresh_token);
        await ports.deleteAppleAuthorization(userId, authorization.client_id);
      }
      return;
    }
    case "storage": {
      for (let round = 0; round < MAX_STORAGE_ROUNDS; round++) {
        const paths = await ports.listAvatarObjects(userId);
        if (paths.length === 0) return;
        await ports.removeAvatarObjects(paths);
      }
      throw new PurgeStepError("incomplete");
    }
    case "database":
      await ports.purgeDatabase(userId);
      return;
    case "auth":
      await ports.deleteAuthUser(userId);
      return;
  }
};

const purgeUser = async (ports: PurgePorts, deletion: ClaimedDeletion): Promise<boolean> => {
  const userId = deletion.user_id;
  const remaining = PURGE_STEPS.filter((step) => !deletion.completed_steps.includes(step));

  if (deletion.attempts >= STUCK_ATTEMPTS) {
    ports.log("error", "account_purge_stuck", {
      user_id: userId,
      attempts: deletion.attempts,
      last_error: deletion.last_error,
    });
  }
  ports.log("info", "account_purge_started", {
    user_id: userId,
    attempt: deletion.attempts,
    resume_from: remaining[0] ?? null,
  });

  for (const step of remaining) {
    try {
      await runStep(ports, userId, step);
    } catch (err) {
      const code = `${step}_${errorCode(err)}`;
      ports.log("warn", "account_purge_step_failed", { user_id: userId, step, error_code: code });
      await ports.recordStep(userId, step, code);
      return false;
    }
    // After the auth step the row has cascaded away, so this record is a no-op there.
    await ports.recordStep(userId, step, null);
    ports.log("info", "account_purge_step_completed", { user_id: userId, step });
  }

  ports.log("info", "account_purge_completed", { user_id: userId, attempts: deletion.attempts });
  return true;
};

export const runPurge = async (ports: PurgePorts): Promise<PurgeSummary> => {
  const claimed = await ports.claim(CLAIM_LIMIT);
  let completed = 0;
  let failed = 0;

  for (const deletion of claimed) {
    let succeeded = false;
    try {
      succeeded = await purgeUser(ports, deletion);
    } catch (err) {
      // Recording itself failed. The lease expires in 15 minutes and a later run resumes.
      ports.log("error", "account_purge_record_failed", {
        user_id: deletion.user_id,
        error_code: errorCode(err),
      });
    }
    if (succeeded) completed++;
    else failed++;
  }

  return { ok: true, claimed: claimed.length, completed, failed };
};
