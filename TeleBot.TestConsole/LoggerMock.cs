using Amazon.Lambda.Core;

namespace TeleBot.TestConsole;

public class LoggerMock : ILambdaLogger
{
    public void Log(string message)
    {
    }

    public void LogLine(string message)
    {
    }
}
