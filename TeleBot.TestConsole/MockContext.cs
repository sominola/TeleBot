using Amazon.Lambda.Core;

namespace TeleBot.TestConsole;

public class MockContext : ILambdaContext
{
    public string AwsRequestId => null!;
    public IClientContext ClientContext => null!;
    public string FunctionName => null!;
    public string FunctionVersion => null!;
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn => null!;
    public ILambdaLogger Logger => new LoggerMock();
    public string LogGroupName => null!;
    public string LogStreamName => null!;
    public int MemoryLimitInMB => 0;
    public TimeSpan RemainingTime => TimeSpan.Zero;
}
