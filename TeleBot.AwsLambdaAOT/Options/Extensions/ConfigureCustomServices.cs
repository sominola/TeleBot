using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeleBot.AwsLambdaAOT.Handlers;
using TeleBot.AwsLambdaAOT.Handlers.TextHandlers;
using TeleBot.Lib;

namespace TeleBot.AwsLambdaAOT.Options.Extensions;

public static class ConfigureCustomServices
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<TextMessageHandler>();
        services.AddScoped<InstaReelsHandler>();
        services.AddScoped<TikTokHandler>();
        services.AddScoped<DeepSeekHandler>();
        services.AddScoped<MediaStreamService>();
        services.AddScoped<ITeleBot>(serviceProvider =>
        {
            var httpClient = serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient("Telegram");
            var options = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

            return new TeleBot.Lib.TeleBot(httpClient, options.TelegramBotApiKey);
        });

        return services;
    }
}
