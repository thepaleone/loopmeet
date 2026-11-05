// The Account page's calls to the Api and to apple-authorization (017 contracts/site.md).
// No globals: fetch, the settings and the access token are passed in, so tests can fake them.

export class AccountApiError extends Error {
  constructor(status, code) {
    // Only the status and code: the message must never carry a response body (FR-013).
    super(`Account API ${status} ${code}`);
    this.name = "AccountApiError";
    this.status = status;
    this.code = code;
  }
}

const readCode = async (response) => {
  try {
    const body = await response.json();
    return typeof body?.code === "string" ? body.code : "http_error";
  } catch {
    return "http_error";
  }
};

export const createAccountApi = ({ fetch, apiBaseUrl, supabaseUrl, supabasePublishableKey }) => {
  const call = async (method, path, accessToken, body) => {
    const headers = { Authorization: `Bearer ${accessToken}` };
    if (body !== undefined) headers["Content-Type"] = "application/json";

    let response;
    try {
      response = await fetch(`${apiBaseUrl}${path}`, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    } catch {
      throw new AccountApiError(0, "network");
    }

    if (!response.ok) throw new AccountApiError(response.status, await readCode(response));
    return await response.json();
  };

  return {
    status: (accessToken) => call("GET", "/users/account-deletion", accessToken),
    preview: (accessToken) => call("GET", "/users/account-deletion/preview", accessToken),
    request: (accessToken) => call("POST", "/users/account-deletion", accessToken, { confirmation: "DELETE" }),
    restore: (accessToken) => call("DELETE", "/users/account-deletion", accessToken),

    // Lets a later deletion revoke the web Sign in with Apple (016 research D7, 017 research D5).
    // Resolves to whether it was stored, and never throws: a failure mustn't block sign-in (FR-011).
    async registerAppleToken(accessToken, refreshToken, clientId) {
      try {
        const response = await fetch(`${supabaseUrl}/functions/v1/apple-authorization`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${accessToken}`,
            apikey: supabasePublishableKey,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ refreshToken, clientId }),
        });
        await response.body?.cancel();
        return response.ok;
      } catch {
        return false;
      }
    },
  };
};
