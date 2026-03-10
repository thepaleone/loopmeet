using System.Net.Http.Headers;

namespace LoopMeet.Api.Services.Auth;

public sealed class AvatarStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _supabaseUrl;
    private readonly string _serviceKey;
    private readonly string _anonKey;
    private readonly string _bucketName;
    private readonly ILogger<AvatarStorageService> _logger;

    public AvatarStorageService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AvatarStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _supabaseUrl = (configuration["Supabase:Url"] ?? string.Empty).TrimEnd('/');
        _serviceKey = configuration["Supabase:ServiceKey"] ?? string.Empty;
        _anonKey = configuration["Supabase:AnonKey"] ?? string.Empty;
        _bucketName = configuration["Supabase:AvatarBucketName"] ?? "avatars";
        _logger = logger;

        _logger.LogInformation(
            "AvatarStorageService initialized. Url configured: {HasUrl}, ServiceKey length: {KeyLength}, AnonKey length: {AnonKeyLength}, Bucket: {Bucket}",
            !string.IsNullOrWhiteSpace(_supabaseUrl),
            _serviceKey.Length,
            _anonKey.Length,
            _bucketName);
    }

    public async Task<string> UploadAsync(Guid userId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serviceKey))
        {
            throw new InvalidOperationException("Supabase:ServiceKey is not configured. Avatar uploads require the service_role key.");
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
        httpClient.DefaultRequestHeaders.Add("apikey", _serviceKey);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);

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
