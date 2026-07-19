using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;

namespace TeleBot.AwsLambdaAOT.Extensions;

public sealed class CustomLoggerProvider : ILoggerProvider
{
    private readonly AsyncLocal<ILambdaLogger?> _currentLogger = new();

    internal ILambdaLogger? CurrentLogger => _currentLogger.Value;

    public void SetLogger(ILambdaLogger logger) => _currentLogger.Value = logger;

    public void ClearLogger() => _currentLogger.Value = null;

    public ILogger CreateLogger(string categoryName) => new CustomLogger(this, categoryName);

    public void Dispose()
    {
    }
}
