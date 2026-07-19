using System.Net;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleBot.AwsLambdaAOT.Extensions;
using TeleBot.AwsLambdaAOT.Handlers;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.AwsLambdaAOT.Options.Extensions;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Extensions;
using TeleBot.Lib.Models;
using TeleBot.Lib.Models.Enums;

namespace TeleBot.AwsLambdaAOT;

public class Function
{
    private const string TelegramApiKeyHeader = "X-Telegram-Bot-ApiKey";
    private static readonly Lazy<ServiceProvider> LambdaServices = new(InitLambda);

    private static async Task Main()
    {
        var handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaJsonContext>())
            .Build()
            .RunAsync();
    }


    public static async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var serviceProvider = LambdaServices.Value;
        var loggerProvider = serviceProvider.GetRequiredService<CustomLoggerProvider>();
        loggerProvider.SetLogger(context.Logger);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Function>>();
            var appOptions = scope.ServiceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

            if (!IsTelegramApiKeyValid(request.Headers, appOptions.TelegramApiKey))
            {
                logger.LogWarning("Invalid or missing Telegram API key");
                return Ok("Invalid or missing Telegram API key");
            }

            try
            {
                logger.LogInformation("Start processing request");
                await ProcessRequest(request.Body, scope, logger);
            }
            catch (Exception e)
            {
                logger.LogError(e, "EXCEPTION WHILE PROCESSING REQUEST");
                return Ok();
            }

            logger.LogInformation("Request success processed");
            return Ok();
        }
        finally
        {
            loggerProvider.ClearLogger();
        }
    }


    private static async Task ProcessRequest(
        string? body,
        IServiceScope scope,
        ILogger<Function> logger
    )
    {
        if (string.IsNullOrEmpty(body))
        {
            logger.LogWarning("Body is null or empty, return");
            return;
        }

        logger.LogInformation("Try deserialize body to <Message>: {Body}", body);

        var updateEvent = JsonSerializer.Deserialize(body, typeof(Update), TeleGenerationContext.Default) as Update;

        if (updateEvent?.Message is null)
        {
            logger.LogWarning("Message is null, return");
            return;
        }

        logger.LogInformation("Message successful deserialized");

        var message = updateEvent.Message;

        var botClient = scope.ServiceProvider.GetRequiredService<ITeleBot>();

        logger.LogInformation("Message type is {Type}", message.Type);
        if (message.Type == MessageType.Text)
        {
            var textHandler = scope.ServiceProvider.GetService<TextMessageHandler>()!;
            await textHandler.Handle(botClient, message);
        }
        else
        {
            logger.LogInformation("No handlers for type {Type}, return", message.Type);
        }
    }

    private static APIGatewayProxyResponse Ok(string? textBody = null)
    {
        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
        };

        if (!string.IsNullOrEmpty(textBody))
        {
            response.Body = textBody;
        }

        return response;
    }

    private static bool IsTelegramApiKeyValid(
        IDictionary<string, string>? headers,
        string expectedApiKey
    )
    {
        if (headers is null || string.IsNullOrEmpty(expectedApiKey))
            return false;

        var actualApiKey = headers
            .FirstOrDefault(header => string.Equals(
                header.Key,
                TelegramApiKeyHeader,
                StringComparison.OrdinalIgnoreCase))
            .Value;

        if (string.IsNullOrEmpty(actualApiKey))
            return false;

        return string.Equals(
            actualApiKey,
            expectedApiKey,
            StringComparison.Ordinal);
    }

    private static ServiceProvider InitLambda()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection()
            .AddHandlers()
            .AddHttpClients(config);

        var loggerProvider = new CustomLoggerProvider();
        services.AddSingleton(loggerProvider);
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(loggerProvider);
        });

        services.AddSingleton<ILogger>(serviceProvider =>
            serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TeleBot"));

        services.Configure<AppOptions>(config.GetSection(nameof(AppOptions)));

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(TikWmResponse))]
[JsonSerializable(typeof(TikWmData))]
[JsonSerializable(typeof(InstaResponse))]
[JsonSerializable(typeof(InstaGramMedia))]
[JsonSerializable(typeof(InstaMeta))]
[JsonSerializable(typeof(InstaComment))]
[JsonSerializable(typeof(DeepSeekRequest))]
[JsonSerializable(typeof(DeepSeekMessage))]
[JsonSerializable(typeof(DeepSeekResponse))]
[JsonSerializable(typeof(DeepSeekChoice))]
[JsonSerializable(typeof(DeepSeekUsage))]
public partial class LambdaJsonContext : JsonSerializerContext
{
}
