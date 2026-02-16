using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LoopMeet.Api.Services.Auth;

public static class JwtValidationHandler
{
    public static void Configure(JwtBearerOptions options, string issuer)
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false
        };
    }
}
