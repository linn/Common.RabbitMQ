using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

public abstract class JsonMessageHandler<T>(string routingKey) : IMessageHandler
{
    public string RoutingKey { get; } = routingKey;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Deserialize<T>(message.Body.Span);
        await this.HandleAsync(body, cancellationToken);
    }

    protected abstract Task HandleAsync(T? body, CancellationToken cancellationToken);
}
