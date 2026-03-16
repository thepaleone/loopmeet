using System.Net.Http.Headers;
using LoopMeet.Api.Services.Configuration;
using Microsoft.Extensions.Options;
using Supabase;

namespace LoopMeet.Api.Services.Auth;

public sealed class AvatarStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentUserService _currentUser;
    private readonly string _supabaseUrl;
    private readonly string _anonOrPublishableKey;
    private readonly string _bucketName;
    private readonly ILogger<AvatarStorageService> _logger;

    public AvatarStorageService(IHttpClientFactory httpClientFactory, CurrentUserService currentUser, IOptions<SupabaseConfigOptions> options, ILogger<AvatarStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _currentUser = currentUser;
        var config = options.Value;
        _supabaseUrl = config.Url.TrimEnd('/');
        _anonOrPublishableKey = config.AnonOrPublishableKey;
        _bucketName = config.AvatarBucketName;
        _logger = logger;

        _logger.LogInformation(
            "AvatarStorageService initialized. Url configured: {HasUrl}, AnonKey length: {AnonKeyLength}, Bucket: {Bucket}",
            !string.IsNullOrWhiteSpace(_supabaseUrl),
            _anonOrPublishableKey.Length,
            _bucketName);
    }

    public async Task<string> UploadAsync(Guid userId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var accessToken = _currentUser.AccessToken;
        if(string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No access token available for current user. Avatar upload may fail if service key is not configured.");
            throw new InvalidOperationException("Current user does not have an access token. Avatar upload requires authentication.");
        }
        
        if (string.IsNullOrWhiteSpace(_supabaseUrl))
        {
            throw new InvalidOperationException("Supabase:Url is not configured.");
        }

        var ext = Path.GetExtension(fileName).TrimStart('.');
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = contentType.Contains("png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        }

        var storagePath = $"{userId}/{Guid.NewGuid()}.{ext}";
        var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{storagePath}";

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        _logger.LogInformation(
            "Uploading avatar for user {UserId} to {Url} ({Bytes} bytes)",
            userId, uploadUrl, bytes.Length);

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("apikey", _anonOrPublishableKey);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await httpClient.PostAsync(uploadUrl, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Avatar upload failed for user {UserId}. Status: {Status}, Body: {Body}",
                userId, (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Avatar upload failed ({(int)response.StatusCode}): {responseBody}");
        }

        var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{storagePath}";
        _logger.LogInformation("Avatar uploaded for user {UserId}. Public URL: {Url}", userId, publicUrl);
        return publicUrl;
    }
}
