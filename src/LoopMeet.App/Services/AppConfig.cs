namespace LoopMeet.App.Services;

public sealed class AppConfig
{
    public string ApiBaseUrl { get; init; } = "";
    public string SupabaseUrl { get; init; } = "";
    public string SupabaseAnonOrPublisableKey { get; init; } = "";
    public string OneSignalAppId { get; init; } = "";
    public string OneSignalRestApiKey { get; init; } = "";
}
