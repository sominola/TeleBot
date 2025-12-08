using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class DeepSeekHandler : IMessageHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public DeepSeekHandler(IHttpClientFactory httpClientFactory, ILogger logger, IOptions<AppOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("Default");
        _httpClient.BaseAddress = new Uri("https://api.deepseek.com");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.DeepSeekApiKey);

        _logger = logger;
    }

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Sending message to DeepSeek: {Text}", message.Text);

            var request = new DeepSeekRequest
            {
                Model = "deepseek-chat",
                Messages = [new DeepSeekMessage { Role = "user", Content = message.Text! }],
                MaxTokens = 1024,
                Temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request, LambdaJsonContext.Default.DeepSeekRequest);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("/chat/completions", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            var deepSeekResponse =
                JsonSerializer.Deserialize(responseJson, LambdaJsonContext.Default.DeepSeekResponse);

            var answer = deepSeekResponse?.Choices.FirstOrDefault()?.Message.Content;
            if (string.IsNullOrEmpty(answer))
            {
                return;
            }

            await botClient.SendMessage(
                message.Chat.Id,
                answer,
                replyToMessageId: message.MessageId,
                ct: ct
            );

            _logger.LogInformation("DeepSeek response sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeekHandler exception");
        }
    }
}
