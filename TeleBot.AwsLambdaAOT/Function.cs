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
using TeleBot.Lib.Extensions;
using TeleBot.Lib.Models;
using TeleBot.Lib.Models.Enums;

namespace TeleBot.AwsLambdaAOT;

public class Function
{
    private static async Task Main()
    {
        var handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler,
                new SourceGeneratorLambdaJsonSerializer<LambdaJsonContext>())
            .Build()
            .RunAsync();
    }


    public static async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        var serviceProvider = InitLambda(context);
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger>()!;

        try
        {
            logger.LogInformation("Start processing request");
            await ProcessRequest(request.Body, scope, logger);
        }
        catch (Exception e)
        {
            logger.LogError("EXCEPTION WHILE PROCESSING REQUEST {E}", e);
            return Ok();
        }

        logger.LogInformation("Request success processed");
        return Ok();
    }

    private static async Task ProcessRequest(string? body, IServiceScope scope, ILogger logger)
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

        var appOptions = scope.ServiceProvider.GetService<IOptions<AppOptions>>()!;
        var botClient = new TeleBot.Lib.TeleBot(appOptions.Value.TelegramApiKey);

        logger.LogInformation("Message type is {Type}", message.Type.ToString());
        if (message.Type == MessageType.Text)
        {
            var textHandler = scope.ServiceProvider.GetService<TextMessageHandler>()!;
            await textHandler.Handle(botClient, message);
        }
        else
        {
            logger.LogInformation("No handlers for type {Type}, return", message.Type.ToString());
        }
    }

    private static APIGatewayProxyResponse Ok() => new()
    {
        StatusCode = (int)HttpStatusCode.OK,
    };

    private static IServiceProvider InitLambda(ILambdaContext context)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection()
            .AddHandlers()
            .AddHttpClients(config);

        var customLogger = new CustomLogger(context.Logger);
        services.AddSingleton<ILogger>(customLogger);

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
[JsonSerializable(typeof(InstagramMediaResponse))]
[JsonSerializable(typeof(InstagramData))]
[JsonSerializable(typeof(InstagramXdt))]
public partial class LambdaJsonContext : JsonSerializerContext
{
}
