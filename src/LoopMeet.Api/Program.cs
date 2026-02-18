using LoopMeet.Api.Services;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Api.Endpoints;
using LoopMeet.Api.Services.Groups;
using LoopMeet.Api.Services.Invitations;
using LoopMeet.Core.Interfaces;
using LoopMeet.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}
builder.Services.AddSingleton<ICacheService>(provider =>
    new CacheService(provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        provider.GetService<IDistributedCache>()));

var supabaseUrl = builder.Configuration["Supabase:Url"] ?? string.Empty;
var supabaseServiceKey = builder.Configuration["Supabase:ServiceKey"]
    ?? builder.Configuration["Supabase:AnonKey"]
    ?? string.Empty;
builder.Services.AddSingleton(_ => new Client(supabaseUrl, supabaseServiceKey, new SupabaseOptions
{
    AutoConnectRealtime = false
}));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));
builder.Services.AddSingleton<PasswordPolicyValidator>();
builder.Services.AddScoped<GroupQueryService>();
builder.Services.AddScoped<GroupCommandService>();
builder.Services.AddScoped<InvitationQueryService>();
builder.Services.AddScoped<InvitationCommandService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<IAuthIdentityRepository, AuthIdentityRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Supabase:JwtIssuer"] ?? string.Empty;
        JwtValidationHandler.Configure(options, issuer);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapUserEndpoints();
app.MapGroupsEndpoints();
app.MapInvitationEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "LoopMeet API" }));

app.Run();

public partial class Program
{
}
