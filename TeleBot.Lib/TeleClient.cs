using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using TeleBot.Lib.Extensions;

namespace TeleBot.Lib;

public interface ITeleClient : IDisposable
{
    Task<TRes> Get<TRes>(string teleMethod, CancellationToken ct = default) where TRes : class;
    Task<TRes> Post<TReq, TRes>(string teleMethod, TReq body, CancellationToken ct = default) where TRes : class;

    Task<TRes> PostMultipartContent<TRes>(
        string teleMethod,
        IDictionary<string, string> keyValues,
        (Stream stream, string fileName, string key) file,
        CancellationToken ct = default);
}

public class TeleClient : ITeleClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseAddress;
    private bool _disposed;

    public TeleClient(string baseUrl, string token)
    {
        if (string.IsNullOrEmpty(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));

        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));

        _baseAddress = baseUrl + token + "/";

        _httpClient = new HttpClient();
    }

    public async Task<TRes> PostMultipartContent<TRes>(
        string teleMethod,
        IDictionary<string, string> keyValues,
        (Stream stream, string fileName, string key) file,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var (key, value) in keyValues)
        {
            content.Add(new StringContent(value), key);
        }

        content.Add(new StreamContent(file.stream), file.key, file.fileName);

        var response = await _httpClient.PostAsync(_baseAddress + teleMethod, content, ct);
        if (response.IsSuccessStatusCode)
        {
            var deserialized = await response.Content
                .ReadFromJsonAsync(TeleGenerationContext.Default.TeleResult, ct);

            if (deserialized is TRes result) return result;

            var message = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Failed parse body. Response message: {message}");
        }

        var responseMessage = await response.Content.ReadAsStringAsync(ct);
        throw new Exception($"Bad Request. Response message: {responseMessage}");
    }

    public async Task<TRes> Get<TRes>(string teleMethod, CancellationToken ct = default) where TRes : class
    {
        using var request = CreateRequest(HttpMethod.Get, teleMethod);
        var response = await Send<TRes>(request, ct);
        return response;
    }


    public async Task<TRes> Post<TReq, TRes>(string teleMethod, TReq body, CancellationToken ct = default)
        where TRes : class
    {
        using var request = CreateRequest(HttpMethod.Post, teleMethod, body);
        var response = await Send<TRes>(request, ct);
        return response;
    }

    private async Task<TRes> Send<TRes>(HttpRequestMessage request, CancellationToken ct = default) where TRes : class
    {
        using var response = await _httpClient.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            var deserialized = await response.Content
                .ReadFromJsonAsync(TeleGenerationContext.Default.TeleResult, ct);

            if (deserialized is TRes result) return result;

            var message = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Failed parse body. Response message: {message}");
        }

        var responseMessage = await response.Content.ReadAsStringAsync(ct);
        throw new Exception($"Bad Request. Response message: {responseMessage}");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string teleMethod)
    {
        return new HttpRequestMessage(method, _baseAddress + teleMethod);
    }

    private HttpRequestMessage CreateRequest<T>(HttpMethod method, string teleMethod, T? body)
    {
        var requestMessage = new HttpRequestMessage(method, _baseAddress + teleMethod);
        if (body is null) return requestMessage;

        var serialized = JsonSerializer.Serialize(body, typeof(T), TeleGenerationContext.Default);
        requestMessage.Content = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json);

        return requestMessage;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();

        _disposed = true;
    }
}
