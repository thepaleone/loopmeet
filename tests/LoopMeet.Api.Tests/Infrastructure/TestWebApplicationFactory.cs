using System.Collections.Generic;
using LoopMeet.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryStore _store;

    public TestWebApplicationFactory(InMemoryStore store)
    {
        _store = store;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "http://localhost",
                ["Supabase:ServiceKey"] = "test-key",
                ["Supabase:JwtIssuer"] = "http://localhost"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IGroupRepository>();
            services.RemoveAll<IMembershipRepository>();
            services.RemoveAll<IInvitationRepository>();
            services.RemoveAll<IAuthIdentityRepository>();

            services.AddSingleton(_store);
            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IGroupRepository, InMemoryGroupRepository>();
            services.AddScoped<IMembershipRepository, InMemoryMembershipRepository>();
            services.AddScoped<IInvitationRepository, InMemoryInvitationRepository>();
            services.AddScoped<IAuthIdentityRepository, InMemoryAuthIdentityRepository>();

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            services.AddAuthorization();
        });
    }
}
