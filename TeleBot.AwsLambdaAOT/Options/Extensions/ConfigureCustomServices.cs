using Microsoft.Extensions.DependencyInjection;
using TeleBot.AwsLambdaAOT.Handlers;
using TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

namespace TeleBot.AwsLambdaAOT.Options.Extensions;

public static class ConfigureCustomServices
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<TextMessageHandler>();
        services.AddScoped<InstaReelsHandler>();
        services.AddScoped<TikTokHandler>();
        services.AddScoped<DeepSeekHandler>();

        return services;
    }
}
