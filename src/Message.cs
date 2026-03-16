namespace Linn.Common.Messaging.RabbitMQ;

public class Message
{
    public string RoutingKey { get; init; } = string.Empty;

    public ReadOnlyMemory<byte> Body { get; init; }

    public IReadOnlyDictionary<string, object> Headers { get; init; } = new Dictionary<string, object>();

    public string? ContentType { get; init; }

    public int? DeliveryMode { get; init; }
}
