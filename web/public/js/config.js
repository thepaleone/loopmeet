// Public settings for the Account page. Every value is safe to publish: the publishable key only
// identifies the project, and row-level security protects the data. The live site and every
// preview use this one backend, the one the app uses (017 research D1).
export const config = Object.freeze({
  supabaseUrl: "https://cswfsnikasaorexwhsas.supabase.co",
  supabasePublishableKey: "sb_publishable__0wAiCklh-5wV_AmK0GJdQ_VAC5dYE8",
  apiBaseUrl: "https://api-staging.loopmeet.io",
  // The Apple Services ID for web sign-in. It must differ from the app's bundle ID, and Supabase's
  // Apple Client IDs must list it first. Empty hides the Apple button.
  appleWebClientId: "io.loopmeet.web",
});
