namespace Linn.Common.Messaging.RabbitMQ;

public interface IPublisher<in TMessage>
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
}
