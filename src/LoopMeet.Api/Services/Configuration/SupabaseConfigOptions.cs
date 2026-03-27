using System;

namespace LoopMeet.Api.Services.Configuration;

public class SupabaseConfigOptions
{
        public string Url { get; set; } = string.Empty;
        public string AnonOrPublishableKey { get; set; } = string.Empty;
        public string ServiceOrSecretKey { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = "authenticated";
        public string AvatarBucketName { get; set; } = "avatars";

        public static void FromConfiguration(IConfiguration configuration, SupabaseConfigOptions options)
        {
                options.Url = configuration["Supabase:Url"] ?? string.Empty;
                options.AnonOrPublishableKey = configuration["Supabase:AnonOrPublishableKey"] ?? string.Empty;
                options.ServiceOrSecretKey = configuration["Supabase:ServiceOrSecretKey"] ?? string.Empty;
                options.JwtIssuer = configuration["Supabase:JwtIssuer"] ?? string.Empty;
                options.JwtAudience = configuration["Supabase:JwtAudience"] ?? "authenticated";
                options.AvatarBucketName = configuration["Supabase:AvatarBucketName"] ?? "avatars";
        }
}
