using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TeleBot.AwsLambdaAOT.Extensions;

public class CustomLogger(ILambdaLogger logger) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Information:
            {
                logger.LogInformation($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.Warning:
            {
                logger.LogWarning($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.Error:
            {
                logger.LogError($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.Critical:
            {
                logger.LogCritical($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.None:
            {
                logger.Log($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.Trace:
            {
                logger.LogTrace($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            case LogLevel.Debug:
            {
                logger.LogDebug($"[{logLevel}]: {formatter(state, exception)}");
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}
