using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Account;

/// <summary>
/// Sends a signed-in user whose account is scheduled for deletion to the pending-deletion screen,
/// the only screen they can use until they restore or sign out. Concurrent Api calls all get the
/// 403, so only the first signal of an episode navigates; the screen calls Reset() on its way out.
/// </summary>
public sealed class AccountDeletionGate
{
    private readonly ILogger<AccountDeletionGate> _logger;
    private int _navigated;

    public AccountDeletionGate(AccountDeletionSignal signal, ILogger<AccountDeletionGate> logger)
    {
        _logger = logger;
        signal.PendingDeletion += OnPendingDeletion;
    }

    public void Reset() => Interlocked.Exchange(ref _navigated, 0);

    private void OnPendingDeletion(DateTimeOffset deletesAt)
    {
        if (Interlocked.Exchange(ref _navigated, 1) == 1)
        {
            return;
        }

        _ = NavigateAsync(deletesAt);
    }

    private async Task NavigateAsync(DateTimeOffset deletesAt)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//pending-deletion", new Dictionary<string, object>
            {
                ["deletesAt"] = deletesAt
            }));
        }
        catch (Exception ex)
        {
            // Let the next blocked call try again.
            Reset();
            _logger.LogWarning(ex, "Navigation to the pending-deletion screen failed.");
        }
    }
}
