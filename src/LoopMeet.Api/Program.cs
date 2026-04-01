using LoopMeet.Api.Contracts;
using LoopMeet.Api.Services;
using LoopMeet.Api.Services.Auth;
using LoopMeet.Api.Services.Cache;
using LoopMeet.Api.Services.Configuration;
using LoopMeet.Api.Endpoints;
using LoopMeet.Api.Services.Groups;
using LoopMeet.Api.Services.Invitations;
using LoopMeet.Api.Services.Meetups;
using LoopMeet.Api.Services.Places;
using LoopMeet.Core.Interfaces;
using LoopMeet.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Supabase;
using Microsoft.Extensions.Options;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

var isDevelopment = builder.Environment.IsDevelopment();
if(isDevelopment)
{
    Log.Logger.Warning("Running in Development environment");
}


builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.Configure<SupabaseConfigOptions>(c => SupabaseConfigOptions.FromConfiguration(builder.Configuration, c));
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

var postgrestDebugHandlerAdded = 0;
builder.Services.AddScoped(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
    var bearerToken = authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
        ? authHeader["Bearer ".Length..].Trim()
        : null;

    if (string.IsNullOrWhiteSpace(bearerToken))
    {
        throw new InvalidOperationException("Missing bearer token for Supabase request.");
    }

    var sbOptions = provider.GetRequiredService<IOptions<SupabaseConfigOptions>>().Value;
    var supabaseClient = new Client(sbOptions.Url, sbOptions.AnonOrPublishableKey, new SupabaseOptions
    {
        AutoConnectRealtime = false,
        Headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {bearerToken}"
        }
    });

    // Register debug handler once — goes to singleton Debugger.Instance, fires for all future clients too
    if (Interlocked.CompareExchange(ref postgrestDebugHandlerAdded, 1, 0) == 0)
    {
        supabaseClient.Postgrest.AddDebugHandler((_, msg, ex) =>
        {
            Log.Debug("[Postgrest] {Message}", msg);
            if (ex is not null)
                Log.Warning("[Postgrest Error] {Error}", ex.Message);
        });
    }

    return supabaseClient;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.AddScoped<AvatarStorageService>();
builder.Services.AddScoped<UserProfileProjectionService>();
builder.Services.AddSingleton<ProfileAvatarResolver>();
builder.Services.AddScoped<IPasswordChangeService, SupabasePasswordChangeService>();
builder.Services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));
builder.Services.AddSingleton<PasswordPolicyValidator>();
builder.Services.AddScoped<GroupQueryService>();
builder.Services.AddScoped<GroupCommandService>();
builder.Services.AddScoped<InvitationQueryService>();
builder.Services.AddScoped<InvitationCommandService>();
builder.Services.AddScoped<MeetupQueryService>();
builder.Services.AddScoped<MeetupCommandService>();

builder.Services.Configure<PlacesOptions>(options =>
{
    options.ApiKey = builder.Configuration["Google:PlacesApiKey"] ?? string.Empty;
});
builder.Services.AddHttpClient<PlacesProxyService>();
builder.Services.AddScoped<PlacesProxyService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<IAuthIdentityRepository, AuthIdentityRepository>();
builder.Services.AddScoped<IMeetupRepository, MeetupRepository>();

var jwtIssuer = builder.Configuration["Supabase:JwtIssuer"] ?? string.Empty;
var jwtAudience = builder.Configuration["Supabase:JwtAudience"] ?? "loopmeet-api";
if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("Supabase:JwtIssuer is required for JWT validation.");
}

Log.Information("Supabase JWT validation configured. Issuer: {Issuer}, Audience: {Audience}",
    jwtIssuer,
    jwtAudience);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Audience = jwtAudience;
        options.Authority = jwtIssuer;
        options.RequireHttpsMetadata = !isDevelopment;
        options.TokenValidationParameters = new()
        {
            IncludeTokenOnFailedValidation = true,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // PUT A BREAKPOINT HERE
                Log.Error("Auth failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Information("Token validated!");
                return Task.CompletedTask;
            }
        };
        // JwtValidationHandler.Configure(options, jwtIssuer, jwtAudience);
    });
builder.Services.AddAuthorization();
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;


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
app.MapMeetupsEndpoints();
app.MapPlacesEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "LoopMeet API" }));

// #if DEBUG
var sbOptions = app.Services.GetRequiredService<IOptions<SupabaseConfigOptions>>().Value;
Log.Information("Supabase Config - Url: {Url}, AnonOrPublishableKey: {AnonOrPublishableKey}, JwtIssuer: {JwtIssuer}, JwtAudience: {JwtAudience}, AvatarBucketName: {AvatarBucketName}",
    sbOptions.Url,
    sbOptions.AnonOrPublishableKey,
    sbOptions.JwtIssuer,
    sbOptions.JwtAudience,
    sbOptions.AvatarBucketName);
// #endif

app.Run();

public partial class Program
{
}
