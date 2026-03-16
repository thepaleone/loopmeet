using Microsoft.Extensions.Configuration;

namespace LoopMeet.Api.Services.Configuration;

public class ConfigurationResolver
{
    public static string ResolveConfigValue(IConfiguration configuration, params string[] keys)
    {
        return ResolveConfigValue(configuration: configuration, defaultValue: string.Empty, keys: keys);
    }
    public static string ResolveConfigValue(IConfiguration configuration, string defaultValue, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return defaultValue;
    }
}