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
        //         options.Url = ConfigurationResolver.ResolveConfigValue(
        //                 configuration,
        //                 string.Empty,
        //                 "Supabase:Url",
        //                 "SUPABASE__URL",
        //                 "SUPABASE_URL");
        //         options.AnonOrPublishableKey = ConfigurationResolver.ResolveConfigValue(
        //                 configuration,
        //                 string.Empty,
        //                 "Supabase:AnonOrPublishableKey",
        //                 "SUPABASE__ANON_OR_PUBLISHABLE_KEY",
        //                 "SUPABASE_ANON_OR_PUBLISHABLE_KEY",
        //                 "Supabase:AnonKey",
        //                 "SUPABASE__ANONKEY",
        //                 "SUPABASE_ANONKEY",
        //                 "SUPABASE_ANON_KEY");
        //         options.ServiceOrSecretKey = ConfigurationResolver.ResolveConfigValue(
        //                 configuration,
        //                 string.Empty,
        //                 "Supabase:ServiceOrSecretKey",
        //                 "SUPABASE__SERVICE_OR_SECRET_KEY",
        //                 "SUPABASE_SERVICE_OR_SECRET_KEY");
        //         options.JwtIssuer = ConfigurationResolver.ResolveConfigValue(
        //                 configuration,
        //                 string.Empty,
        //                 "Supabase:JwtIssuer",
        //                 "SUPABASE__JWT_ISSUER",
        //                 "SUPABASE_JWT_ISSUER");
        //         options.AvatarBucketName = ConfigurationResolver.ResolveConfigValue(
        //                 configuration,
        //                 defaultValue: "avatars",
        //                 "Supabase:AvatarBucketName",
        //                 "SUPABASE__AVATAR_BUCKET_NAME",
        //                 "SUPABASE_AVATAR_BUCKET_NAME"
        //                 );
        // }
}
