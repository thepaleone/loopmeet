using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace LoopMeet.App.Services;

public static class ApiClient
{
    public static IHttpClientBuilder AddLoopMeetApi<TClient>(this IServiceCollection services, AppConfig config)
        where TClient : class
    {
        return services
            .AddRefitClient<TClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(config.ApiBaseUrl);
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry)));
    }
}
