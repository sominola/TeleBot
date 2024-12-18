using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using TeleBot.AwsLambdaAOT;
using TeleBot.TestConsole;

var response = await Function.FunctionHandler(new APIGatewayProxyRequest
{
    Body = File.ReadAllText("./teleMessage.json"),
}, new MockContext());

Console.WriteLine(JsonSerializer.Serialize(response));
