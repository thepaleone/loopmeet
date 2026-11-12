using System.Text.Json;
using System.Text.Json.Serialization;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Supabase.Postgrest.Exceptions;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class AccountDeletionRepository : IAccountDeletionRepository
{
    private readonly Client _client;

    public AccountDeletionRepository(Client client)
    {
        _client = client;
    }

    public async Task<AccountDeletion?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<AccountDeletionRecord>()
            .Filter("user_id", Operator.Equals, userId.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    public async Task<AccountDeletion> RequestAsync(CancellationToken cancellationToken = default)
    {
        // Rpc<T> ignores [Column] names, so the returned row is read with explicit snake_case names.
        var response = await _client.Rpc("request_account_deletion", null);
        var row = JsonSerializer.Deserialize<RequestedRow>(response.Content ?? string.Empty)
            ?? throw new InvalidOperationException("request_account_deletion returned no row.");
        return Map(new AccountDeletionRecord
        {
            UserId = row.UserId,
            RequestedAt = row.RequestedAt,
            PurgeAfter = row.PurgeAfter,
            Status = row.Status
        });
    }

    public async Task<AccountRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var restored = await _client.Rpc<bool>("restore_account_deletion", null);
            return restored ? AccountRestoreResult.Restored : AccountRestoreResult.NotPending;
        }
        catch (PostgrestException exception) when (exception.Message.Contains("deletion_in_progress", StringComparison.Ordinal))
        {
            return AccountRestoreResult.DeletionInProgress;
        }
    }

    private static AccountDeletion Map(AccountDeletionRecord record)
    {
        var status = record.Status == "purging" ? AccountDeletionStatus.Purging : AccountDeletionStatus.Pending;
        return new AccountDeletion(record.UserId, record.RequestedAt, record.PurgeAfter, status);
    }

    private sealed record RequestedRow(
        [property: JsonPropertyName("user_id")] Guid UserId,
        [property: JsonPropertyName("requested_at")] DateTimeOffset RequestedAt,
        [property: JsonPropertyName("purge_after")] DateTimeOffset PurgeAfter,
        [property: JsonPropertyName("status")] string Status);
}
