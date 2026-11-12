// The Account page's decisions as pure functions (017 contracts/site.md): which view to show,
// when Delete may be confirmed, and the fixed message for each failure. No DOM access.
import { AccountApiError } from "./account-api.js";

export const MESSAGES = Object.freeze({
  network: "Couldn't reach LoopMeet. Check your connection and try again.",
  sessionEnded: "Your session ended. Sign in again.",
  invalidLogin: "That email and password don't match a LoopMeet account.",
  cancelled: "Sign-in was cancelled.",
  generic: "Something went wrong. Try again, or email info@palehorse.net.",
});

const VIEW_BY_STATUS = { none: "active", pending: "scheduled", purging: "purging" };

export const isDeletionInProgress = (error) =>
  error instanceof AccountApiError && error.status === 409 && error.code === "deletion_in_progress";

// statusOrError is null while loading, the status response once loaded, or the error that stopped it.
export const viewFor = (session, statusOrError) => {
  if (!session) return "signed-out";
  if (statusOrError == null) return "loading";
  if (statusOrError instanceof Error) return isDeletionInProgress(statusOrError) ? "purging" : "error";
  return VIEW_BY_STATUS[statusOrError.status] ?? "error";
};

// Exactly as the server compares it: no trimming, no case folding.
export const canConfirm = (text) => text === "DELETE";

export const messageFor = (error) => {
  if (error instanceof AccountApiError) {
    if (error.code === "network") return MESSAGES.network;
    if (error.status === 401) return MESSAGES.sessionEnded;
    return MESSAGES.generic;
  }
  // fetch rejects with a TypeError when the network is down; supabase-js wraps it in AuthRetryableFetchError.
  if (error instanceof TypeError || error?.name === "AuthRetryableFetchError") return MESSAGES.network;
  if (error?.code === "invalid_credentials") return MESSAGES.invalidLogin;
  // The provider redirects back with error=access_denied when the visitor closes or cancels its window.
  if (error?.code === "access_denied") return MESSAGES.cancelled;
  return MESSAGES.generic;
};

export const showApple = (config) => Boolean(config?.appleWebClientId);

export const formatDeletionDate = (iso, locale = undefined, timeZone = undefined) =>
  new Intl.DateTimeFormat(locale, { dateStyle: "long", timeZone }).format(new Date(iso));
