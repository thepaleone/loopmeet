using Microsoft.Extensions.DependencyInjection;
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
            });
    }
}
