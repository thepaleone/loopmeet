namespace LoopMeet.App.Features.Account;

/// <summary>
/// The delete button stays disabled until the user types exactly DELETE: case-sensitive and
/// untrimmed, matching the Api's own check. Kept MAUI-free so it is unit tested directly.
/// </summary>
public static class AccountDeletionConfirmation
{
    public const string RequiredText = "DELETE";

    public static bool IsConfirmed(string? text) => string.Equals(text, RequiredText, StringComparison.Ordinal);
}
