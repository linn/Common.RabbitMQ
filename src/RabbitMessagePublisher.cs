using RabbitMQ.Client;

namespace Linn.Common.Messaging.RabbitMQ;

public class RabbitPublisher(IChannel channel, string exchange)
{
    public async Task PublishAsync(Message message, CancellationToken cancellationToken = default)
    {
        var props = new BasicProperties
        {
            // A copy: with publisher confirms the client adds its sequence-number header to this dictionary.
            Headers = message.Headers.Any()
                ? message.Headers.ToDictionary(h => h.Key, object? (h) => h.Value)
                : null
        };

        if (message.ContentType != null)
        {
            props.ContentType = message.ContentType;
        }

        if (message.DeliveryMode == 2)
        {
            props.DeliveryMode = DeliveryModes.Persistent;
        }
        else if (message.DeliveryMode == 1)
        {
            props.DeliveryMode = DeliveryModes.Transient;
        }

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: message.RoutingKey,
            mandatory: false,
            body: message.Body,
            cancellationToken: cancellationToken,
            basicProperties: props);
    }
}
