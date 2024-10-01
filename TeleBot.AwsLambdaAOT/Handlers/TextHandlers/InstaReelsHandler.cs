using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public partial class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    IOptions<AppOptions> options
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");
        using var requestMessage = BuildMessage(message.Text!);

        using var response = await _defaultHttpClient.SendAsync(requestMessage, ct);
        if (response.IsSuccessStatusCode)
        {
            var instaJobResponse = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.InstaJobResponse, ct);

            if (instaJobResponse is null)
                throw new Exception("InstaResponse is null, return");

            var url = string.Empty;
            var retry = 15;
            var currentRetry = 0;
            while (true)
            {
                if (currentRetry == retry)
                    throw new Exception("InstaResponse url timeout");
                
                var jobStatus = await _defaultHttpClient.GetFromJsonAsync(
                    $"https://app.publer.io/api/v1/job_status/{instaJobResponse.JobId}", 
                    LambdaJsonContext.Default.InstaResponse, 
                    cancellationToken: ct);

                if (jobStatus!.Status == "complete")
                {
                    url = jobStatus.Payload?.FirstOrDefault()?.Path;
                    if (string.IsNullOrEmpty(url))
                        throw new Exception("InstaUrl is null or empty");
                    break;
                }

                currentRetry++;
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            // logger.LogInformation("URL: {Url}", url);

            using var contentResponse = await _defaultHttpClient.GetAsync(url, ct);
            var fileName = contentResponse.Content.Headers.ContentDisposition!.FileName;
            var ext = Path.GetExtension(fileName);
            var isVideo = ext!.Contains("mp4");
            await using var stream = await contentResponse.Content.ReadAsStreamAsync(ct);

            if (isVideo)
            {
                await botClient.SendVideo(
                    message.Chat.Id,
                    stream,
                    $"{Guid.NewGuid()}{ext}",
                    hasSpoiler: false,
                    disableNotification: true,
                    replyToMessageId: message.MessageId,
                    ct: ct);
            }
            else
            {
                await botClient.SendPhoto(
                    message.Chat.Id,
                    stream,
                    $"{Guid.NewGuid()}{ext}",
                    hasSpoiler: false,
                    disableNotification: true,
                    replyToMessageId: message.MessageId,
                    ct: ct
                );
            }
        }
    }

    private HttpRequestMessage BuildMessage(string url)
    {
        var obj = new InstaRequest
        {
            Iphone = false,
            Url = url,
        };

        var json = JsonSerializer.Serialize(obj, LambdaJsonContext.Default.InstaRequest);
        var req = new HttpRequestMessage(HttpMethod.Post, options.Value.InstagramApiUrl + "/hooks/media")
        {
            Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json),
        };

        _defaultHttpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _defaultHttpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        _defaultHttpClient.DefaultRequestHeaders.Add("Accept-Language",
            "uk-UA,uk;q=0.9,ru-UA;q=0.8,ru;q=0.7,en-US;q=0.6,en;q=0.5");
        _defaultHttpClient.DefaultRequestHeaders.Add("If-None-Match", "W/\"71786219b00b25d7225fe65316a84acf\"");
        _defaultHttpClient.DefaultRequestHeaders.Add("Origin", "https://publer.io");
        _defaultHttpClient.DefaultRequestHeaders.Add("Referer", "https://publer.io/");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-CH-UA",
            "\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Google Chrome\";v=\"128\"");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        _defaultHttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
        _defaultHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");


        return req;
    }
}
