using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

public class JsonMessagePublisher<T>(
    RabbitPublisher rabbitPublisher,
    string routingKey,
    IReadOnlyDictionary<string, object>? headers = null)
    : IPublisher<T>
{
    public async Task PublishAsync(T obj, CancellationToken cancellationToken = default)
    {
        var msg = new Message
        {
            RoutingKey = routingKey,
            Body = JsonSerializer.SerializeToUtf8Bytes(obj),
            Headers = headers ?? new Dictionary<string, object>()
        };

        await rabbitPublisher.PublishAsync(msg, cancellationToken);
    }
}
