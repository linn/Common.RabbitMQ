namespace Linn.Common.Messaging.RabbitMQ;

public interface IMessageHandler
{
    string RoutingKey { get; }

    Task HandleAsync(
            Message message,
            CancellationToken cancellationToken);
}
