// The Account page's DOM wiring (017 plan): the Supabase client, events and rendering. Decisions
// live in account-state.js and requests in account-api.js, which the tests cover.
import { config } from "./config.js";
import { createAccountApi } from "./account-api.js";
import {
  canConfirm,
  formatDeletionDate,
  isDeletionInProgress,
  MESSAGES,
  messageFor,
  showApple,
  viewFor,
} from "./account-state.js";

// The OAuth provider a sign-in was started with, so the return trip knows an Apple sign-in happened.
const PROVIDER_KEY = "loopmeet.account.oauth-provider";

const client = globalThis.supabase.createClient(config.supabaseUrl, config.supabasePublishableKey, {
  auth: {
    flowType: "pkce",
    storage: globalThis.sessionStorage,
    persistSession: true,
    autoRefreshToken: true,
    detectSessionInUrl: true,
  },
});
const api = createAccountApi({ ...config, fetch: (...args) => globalThis.fetch(...args) });

const main = document.getElementById("main");
const message = document.getElementById("account-message");
const sections = new Map([...main.querySelectorAll("[data-view]")].map((section) => [section.dataset.view, section]));
const confirmText = document.getElementById("confirm-text");
const confirmButton = main.querySelector('[data-action="confirm"]');

// ready turns true at the first auth event, so a saved session never flashes the sign-in form.
const state = { ready: false, session: null, status: null, preview: null, loadedFor: null };
let shownView = null;

const say = (text) => {
  message.textContent = text ?? "";
};

const fields = (name) => main.querySelectorAll(`[data-field="${name}"]`);

const fill = (view) => {
  for (const el of fields("email")) el.textContent = state.session?.user?.email ?? "";

  const deletesAt = view === "confirm" ? state.preview?.deletesAt : state.status?.deletesAt;
  for (const el of fields("deletes-at")) el.textContent = deletesAt ? formatDeletionDate(deletesAt) : "";

  if (view === "confirm") {
    const groups = state.preview.ownedGroups ?? [];
    const list = fields("owned-groups")[0];
    list.replaceChildren(...groups.map((group) => {
      const item = document.createElement("li");
      const members = group.memberCount === 1 ? "1 member" : `${group.memberCount} members`;
      item.textContent = `${group.name} (${members})`;
      return item;
    }));
    fields("owned-groups-block")[0].hidden = groups.length === 0;
    fields("no-owned-groups")[0].hidden = groups.length > 0;
  }

  if (view === "error") fields("error-message")[0].textContent = messageFor(state.status);
};

const render = () => {
  const view = !state.ready ? "loading" : state.preview ? "confirm" : viewFor(state.session, state.status);
  fill(view);
  if (view === shownView) return;

  for (const [name, section] of sections) section.hidden = name !== view;
  // Move focus to the new view's heading so screen readers announce it, but not on first load.
  if (shownView !== null) sections.get(view)?.querySelector("h2")?.focus();
  shownView = view;
};

const setBusy = (busy) => {
  main.setAttribute("aria-busy", String(busy));
  for (const el of main.querySelectorAll("button, input")) el.disabled = busy;
  if (!busy) confirmButton.disabled = !canConfirm(confirmText.value);
};

// A 401 means the session is gone: sign out, then say so. A 409 deletion_in_progress shows the
// purging view. Anything else becomes a fixed message (FR-013).
const handleFailure = async (error) => {
  if (error?.status === 401) {
    await client.auth.signOut();
    say(MESSAGES.sessionEnded);
  } else if (isDeletionInProgress(error)) {
    state.preview = null;
    state.status = error;
    render();
  } else {
    say(messageFor(error));
  }
};

const run = async (action) => {
  say("");
  setBusy(true);
  try {
    await action();
  } catch (error) {
    await handleFailure(error);
  } finally {
    setBusy(false);
  }
};

const loadStatus = async () => {
  state.status = null;
  state.preview = null;
  render();
  try {
    state.status = await api.status(state.session.access_token);
  } catch (error) {
    if (error?.status === 401) return handleFailure(error);
    state.status = error;
  }
  render();
};

const registerAppleIfJustSignedIn = (session) => {
  const provider = globalThis.sessionStorage.getItem(PROVIDER_KEY);
  globalThis.sessionStorage.removeItem(PROVIDER_KEY);
  if (provider === "apple" && session.provider_refresh_token && showApple(config)) {
    // Not awaited: a failure here must never block the page (FR-011).
    void api.registerAppleToken(session.access_token, session.provider_refresh_token, config.appleWebClientId);
  }
};

client.auth.onAuthStateChange((_event, session) => {
  state.ready = true;
  state.session = session;
  if (!session) {
    Object.assign(state, { status: null, preview: null, loadedFor: null });
    render();
    return;
  }
  if (state.loadedFor === session.user.id) return;

  state.loadedFor = session.user.id;
  registerAppleIfJustSignedIn(session);
  // supabase-js asks callers not to await its own calls inside this callback.
  setTimeout(loadStatus, 0);
});

// A provider that sends the visitor back with an error (for example, they cancelled) puts it in the URL.
const returned = new URLSearchParams(globalThis.location.search);
if (returned.has("error")) {
  globalThis.sessionStorage.removeItem(PROVIDER_KEY);
  say(messageFor({ code: returned.get("error") }));
  globalThis.history.replaceState(null, "", globalThis.location.pathname);
}

main.querySelector('[data-provider="apple"]').hidden = !showApple(config);
for (const button of main.querySelectorAll("[data-provider]")) {
  button.addEventListener("click", () =>
    run(async () => {
      const provider = button.dataset.provider;
      globalThis.sessionStorage.setItem(PROVIDER_KEY, provider);
      const { error } = await client.auth.signInWithOAuth({
        provider,
        options: { redirectTo: `${globalThis.location.origin}/account/` },
      });
      if (error) throw error;
    }));
}

document.getElementById("sign-in-form").addEventListener("submit", (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  run(async () => {
    const { error } = await client.auth.signInWithPassword({
      email: form.elements.email.value.trim(),
      password: form.elements.password.value,
    });
    if (error) throw error;
    form.reset();
  });
});

const actions = {
  delete: () =>
    run(async () => {
      state.preview = await api.preview(state.session.access_token);
      confirmText.value = "";
      render();
    }),
  cancel: () => {
    state.preview = null;
    render();
  },
  restore: () =>
    run(async () => {
      await api.restore(state.session.access_token);
      await loadStatus();
      say("Your account has been restored.");
    }),
  retry: () => run(loadStatus),
  "sign-out": () => run(() => client.auth.signOut()),
};

for (const button of main.querySelectorAll("[data-action]")) {
  const action = actions[button.dataset.action];
  if (action) button.addEventListener("click", action);
}

confirmText.addEventListener("input", () => {
  confirmButton.disabled = !canConfirm(confirmText.value);
});

document.getElementById("confirm-form").addEventListener("submit", (event) => {
  event.preventDefault();
  if (!canConfirm(confirmText.value)) return;
  run(async () => {
    state.status = await api.request(state.session.access_token);
    state.preview = null;
    render();
  });
});

render();
