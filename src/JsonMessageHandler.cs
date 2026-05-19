using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

public abstract class JsonMessageHandler<T>(JsonSerializerOptions? serializerOptions = null) : IMessageHandler
{
    public abstract string RoutingKey { get; }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<T>(message.Body.Span, serializerOptions)
            ?? throw new JsonException($"Failed to deserialize message body to {typeof(T).Name}");

        await HandleAsync(payload, message.Headers, cancellationToken);
    }

    public abstract Task HandleAsync(
        T payload,
        IReadOnlyDictionary<string, object> headers,
        CancellationToken cancellationToken);
}
