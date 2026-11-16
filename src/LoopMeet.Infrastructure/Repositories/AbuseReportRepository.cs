using System.Text.Json;
using System.Text.Json.Serialization;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using Supabase;
using Supabase.Postgrest.Exceptions;

namespace LoopMeet.Infrastructure.Repositories;

/// <summary>
/// Calls submit_abuse_report() as the caller (023 research D1). The function enforces membership,
/// the reported person, the details and the daily limit, and raises a code for each refusal.
/// </summary>
public sealed class AbuseReportRepository : IAbuseReportRepository
{
    // invalid_reported_user before invalid_report, which is its prefix.
    private static readonly (string Code, ReportSubmissionStatus Status)[] Refusals =
    [
        ("invalid_reported_user", ReportSubmissionStatus.InvalidReportedUser),
        ("invalid_report", ReportSubmissionStatus.Invalid),
        ("not_group_member", ReportSubmissionStatus.NotGroupMember),
        ("report_limit_reached", ReportSubmissionStatus.LimitReached)
    ];

    private readonly Client _client;

    public AbuseReportRepository(Client client)
    {
        _client = client;
    }

    public async Task<ReportSubmission> SubmitAsync(Guid reporterUserId, NewAbuseReport report, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_group_id"] = report.GroupId,
            ["p_reported_user_id"] = report.ReportedUserId,
            ["p_reason"] = ReportReasonCodes.ToCode(report.Reason),
            ["p_details"] = report.Details
        };

        try
        {
            // Rpc<T> ignores [Column] names, so the returned row is read with explicit snake_case names.
            var response = await _client.Rpc("submit_abuse_report", parameters);
            var row = JsonSerializer.Deserialize<SubmittedRow[]>(response.Content ?? string.Empty)?.SingleOrDefault()
                ?? throw new InvalidOperationException("submit_abuse_report returned no row.");
            var details = report.Details.Trim();
            var stored = new AbuseReport(row.Id, reporterUserId, report.GroupId, report.ReportedUserId, report.Reason, details, row.ReceivedAt);
            return new ReportSubmission(ReportSubmissionStatus.Submitted, stored);
        }
        catch (PostgrestException exception)
        {
            foreach (var (code, status) in Refusals)
            {
                if (exception.Message.Contains(code, StringComparison.Ordinal))
                {
                    return new ReportSubmission(status, null);
                }
            }

            throw;
        }
    }

    private sealed record SubmittedRow(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("received_at")] DateTimeOffset ReceivedAt);
}
