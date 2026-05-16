export interface OneSignalRequest {
  app_id: string;
  include_external_user_ids: string[];
  headings: Record<string, string>;
  contents: Record<string, string>;
  data: Record<string, unknown>;
  android_channel_id?: string;
}

export interface OneSignalResponse {
  id?: string;
  errors?: unknown;
}

export class OneSignalClient {
  constructor(
    private readonly restApiKey: string,
    private readonly baseUrl = "https://api.onesignal.com/notifications",
  ) {}

  async send(payload: OneSignalRequest): Promise<OneSignalResponse> {
    const response = await fetch(this.baseUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Key ${this.restApiKey}`,
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const body = await response.text();
      throw new Error(`onesignal_send_failed:${response.status}:${body}`);
    }

    return (await response.json()) as OneSignalResponse;
  }
}
