using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Safety;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Endpoints;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        // Reports are never read back through the Api: there is no GET (FR-008).
        app.MapPost("/reports", async (
                ReportRequest request,
                CurrentUserService currentUser,
                ReportCommandService reportService,
                CancellationToken cancellationToken) =>
            {
                var userId = currentUser.UserId;
                if (userId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await reportService.SubmitAsync(userId.Value, request, cancellationToken);
                return result.Status switch
                {
                    ReportSubmissionStatus.Submitted => Results.Created(
                        $"/reports/{result.Report!.Id}",
                        new ReportResponse { Id = result.Report.Id, ReceivedAt = result.Report.ReceivedAt }),
                    ReportSubmissionStatus.NotGroupMember => Error(StatusCodes.Status403Forbidden, "not_group_member",
                        "You can only report groups you belong to."),
                    ReportSubmissionStatus.InvalidReportedUser => Error(StatusCodes.Status422UnprocessableEntity, "invalid_reported_user",
                        "Choose someone in this group other than yourself."),
                    ReportSubmissionStatus.LimitReached => Error(StatusCodes.Status429TooManyRequests, "report_limit_reached",
                        "You've sent 10 reports today. You can send more later, or email abuse@loopmeet.io."),
                    _ => Error(StatusCodes.Status400BadRequest, "invalid_report",
                        "Choose a reason and describe what happened (up to 1,000 characters).")
                };
            })
            .RequireAuthorization();

        return app;
    }

    private static IResult Error(int status, string code, string message) =>
        Results.Json(new ErrorResponse { Code = code, Message = message }, statusCode: status);
}
