using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers;

public interface IMessageHandler
{
    Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default);
}
