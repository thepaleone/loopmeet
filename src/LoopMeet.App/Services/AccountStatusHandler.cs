using System.Net;
using System.Text.Json;
using LoopMeet.App.Features.Account;

namespace LoopMeet.App.Services;

/// <summary>
/// Spots the Api's 403 account_pending_deletion response and raises AccountDeletionSignal. Runs after
/// ApiAuthHandler, never navigates, and returns the response unchanged: the 403 isn't a sign-out.
/// </summary>
public sealed class AccountStatusHandler : DelegatingHandler
{
    private const string PendingDeletionCode = "account_pending_deletion";

    private readonly AccountDeletionSignal _signal;

    public AccountStatusHandler(AccountDeletionSignal signal)
    {
        _signal = signal;
    }

    /// <summary>For callers holding an ApiException: pass its Content.</summary>
    public static bool IsPendingDeletion(string? body) => TryReadDeletesAt(body, out _);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Forbidden)
        {
            return response;
        }

        // Buffered so Refit can still read the body for its ApiException.
        await response.Content.LoadIntoBufferAsync(cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (TryReadDeletesAt(body, out var deletesAt))
        {
            _signal.Raise(deletesAt);
        }

        return response;
    }

    private static bool TryReadDeletesAt(string? body, out DateTimeOffset deletesAt)
    {
        deletesAt = default;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.String
                && code.GetString() == PendingDeletionCode
                && root.TryGetProperty("deletesAt", out var date)
                && date.ValueKind == JsonValueKind.String
                && date.TryGetDateTimeOffset(out deletesAt);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
