using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #pragma warning disable CS0618
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    #pragma warning restore CS0618
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault();
        var email = Request.Headers["X-Test-Email"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test user"));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
