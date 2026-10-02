using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

// The constructor signature is unchanged since 5.1 (binary compatibility for assemblies compiled
// against earlier versions); newer options are init properties.
public class JsonMessagePublisher<T>(
    RabbitPublisher rabbitPublisher,
    string routingKey,
    IReadOnlyDictionary<string, object>? headers = null,
    JsonSerializerOptions? serializerOptions = null)
    : IPublisher<T>
{
    /// <summary>Content type set on published messages (e.g. a vendor media type). Default: none.</summary>
    public string? ContentType { get; init; }

    /// <summary>Publish with delivery mode 2 (persistent). Default: false (broker default).</summary>
    public bool Persistent { get; init; }

    public async Task PublishAsync(T obj, CancellationToken cancellationToken = default)
    {
        var msg = new Message
        {
            RoutingKey = routingKey,
            Body = JsonSerializer.SerializeToUtf8Bytes(obj, serializerOptions),
            Headers = headers ?? new Dictionary<string, object>(),
            ContentType = this.ContentType,
            DeliveryMode = this.Persistent ? 2 : null
        };

        await rabbitPublisher.PublishAsync(msg, cancellationToken);
    }
}
