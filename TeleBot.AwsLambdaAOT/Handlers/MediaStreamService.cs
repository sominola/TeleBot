namespace TeleBot.AwsLambdaAOT.Handlers;

public enum MediaType
{
    Photo,
    Video,
}

public sealed class MediaStreamResult(
    MediaType type,
    string extension,
    Stream stream,
    HttpResponseMessage response
) : IAsyncDisposable
{
    public MediaType Type { get; } = type;
    public string Extension { get; } = extension;
    public Stream Stream { get; } = stream;

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Stream.DisposeAsync();
        }
        finally
        {
            response.Dispose();
        }
    }
}

public sealed class MediaStreamService(IHttpClientFactory httpClientFactory)
{
    private const long MaxVideoSizeBytes = 50L * 1024 * 1024;
    private const string UserAgent = "Telegram/31192 CFNetwork/1492.0.1 Darwin/23.3.0";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Default");

    public async Task<MediaStreamResult> Get(
        string url,
        CancellationToken ct = default
    )
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Media URL must be an absolute HTTPS URL.", nameof(url));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        try
        {
            response.EnsureSuccessStatusCode();

            var media = DetectMedia(
                response.Content.Headers.ContentType?.MediaType,
                uri.AbsolutePath);

            if (media.Type == MediaType.Video)
            {
                var contentLength = response.Content.Headers.ContentLength
                                    ?? throw new InvalidDataException("Video Content-Length is missing.");

                if (contentLength > MaxVideoSizeBytes)
                {
                    throw new InvalidDataException(
                        $"Video exceeds the {MaxVideoSizeBytes} byte limit.");
                }
            }

            var sourceStream = await response.Content.ReadAsStreamAsync(ct);
            return new MediaStreamResult(media.Type, media.Extension, sourceStream, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static MediaDescriptor DetectMedia(string? contentType, string path)
    {
        var normalizedContentType = contentType?.ToLowerInvariant();
        var normalizedExtension = Path.GetExtension(path).ToLowerInvariant();

        return normalizedContentType switch
        {
            "video/mp4" => new(MediaType.Video, ".mp4"),
            "video/webm" => new(MediaType.Video, ".webm"),
            "video/quicktime" => new(MediaType.Video, ".mov"),
            "video/x-matroska" => new(MediaType.Video, ".mkv"),
            "video/x-msvideo" => new(MediaType.Video, ".avi"),
            "video/3gpp" => new(MediaType.Video, ".3gp"),
            "video/x-m4v" => new(MediaType.Video, ".m4v"),
            "image/jpeg" => new(MediaType.Photo, ".jpg"),
            "image/png" => new(MediaType.Photo, ".png"),
            "image/webp" => new(MediaType.Photo, ".webp"),
            "image/bmp" => new(MediaType.Photo, ".bmp"),
            "image/gif" => new(MediaType.Photo, ".gif"),
            "image/tiff" => new(MediaType.Photo, ".tiff"),
            "image/heic" => new(MediaType.Photo, ".heic"),
            _ => normalizedExtension switch
            {
                ".mp4" or ".webm" or ".mov" or ".mkv" or ".avi" or ".3gp" or ".m4v" =>
                    new(MediaType.Video, normalizedExtension),
                ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif" or ".tiff" or ".heic" =>
                    new(MediaType.Photo, normalizedExtension),
                _ => throw new NotSupportedException(
                    $"Unsupported media type: {contentType ?? "unknown"}."),
            },
        };
    }

    private readonly record struct MediaDescriptor(MediaType Type, string Extension);
}
