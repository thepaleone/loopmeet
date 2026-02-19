using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LoopMeet.Api.Services.Auth;

public static class JwtValidationHandler
{
    public static void Configure(JwtBearerOptions options, string issuer, string audience)
    {
        options.Authority = issuer;
        options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = issuer.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true
        };
    }
}
