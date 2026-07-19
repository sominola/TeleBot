using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeleBot.AwsLambdaAOT.Options.Extensions;

public static class ConfigureHttpClients
{
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddHttpClient("Default", client => { client.Timeout = TimeSpan.FromSeconds(15); })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip,
                }
            );

        services.AddHttpClient("Telegram", client => { client.Timeout = TimeSpan.FromSeconds(15); });

        return services;
    }
}
