using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TeleBot.AwsLambdaAOT.Extensions;

public sealed class CustomLogger(
    CustomLoggerProvider provider,
    string categoryName
) : ILogger
{
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel) || provider.CurrentLogger is not { } logger)
            return;

        var message = $"[{categoryName}] [{logLevel}]: {formatter(state, exception)}";

        if (exception is not null)
            message += $"{Environment.NewLine}{exception}";

        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(message);
                break;

            case LogLevel.Debug:
                logger.LogDebug(message);
                break;

            case LogLevel.Information:
                logger.LogInformation(message);
                break;

            case LogLevel.Warning:
                logger.LogWarning(message);
                break;

            case LogLevel.Error:
                logger.LogError(message);
                break;

            case LogLevel.Critical:
                logger.LogCritical(message);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && provider.CurrentLogger is not null;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;
}
