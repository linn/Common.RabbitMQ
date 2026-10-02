using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

public class JsonMessagePublisher<T>(
    RabbitPublisher rabbitPublisher,
    string routingKey,
    IReadOnlyDictionary<string, object>? headers = null,
    JsonSerializerOptions? serializerOptions = null,
    string? contentType = null,
    bool persistent = false)
    : IPublisher<T>
{
    public async Task PublishAsync(T obj, CancellationToken cancellationToken = default)
    {
        var msg = new Message
        {
            RoutingKey = routingKey,
            Body = JsonSerializer.SerializeToUtf8Bytes(obj, serializerOptions),
            Headers = headers ?? new Dictionary<string, object>(),
            ContentType = contentType,
            DeliveryMode = persistent ? 2 : null
        };

        await rabbitPublisher.PublishAsync(msg, cancellationToken);
    }
}
