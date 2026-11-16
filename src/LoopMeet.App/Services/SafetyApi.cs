using LoopMeet.App.Features.Safety.Models;
using Refit;

namespace LoopMeet.App.Services;

public interface ISafetyApi
{
    [Post("/reports")]
    Task<ReportResult> SubmitReportAsync([Body] ReportRequest request);
}

public sealed class SafetyApi
{
    private readonly ISafetyApi _api;

    public SafetyApi(ISafetyApi api)
    {
        _api = api;
    }

    public Task<ReportResult> SubmitReportAsync(ReportRequest request) => _api.SubmitReportAsync(request);
}
