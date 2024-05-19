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
        services.AddHttpClient("Tikwm", client =>
        {
            var baseUrl = configuration.GetSection($"{nameof(AppOptions)}:{nameof(AppOptions.TikWmApiUrl)}").Value;

            client.BaseAddress = new Uri(baseUrl!);
        });

        services.AddHttpClient("Prostoy")
            .ConfigurePrimaryHttpMessageHandler(x => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip,
        });

        return services;
    }
}
