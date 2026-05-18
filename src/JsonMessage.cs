using System.Text;
using System.Text.Json;

namespace Linn.Common.Messaging.RabbitMQ;

public class JsonMessage<T>(Message message) where T : class
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string RoutingKey { get; } = message.RoutingKey;

    public IReadOnlyDictionary<string, object> Headers { get; } = message.Headers;

    public T? Body { get; } = JsonSerializer.Deserialize<T>(
        Encoding.UTF8.GetString(message.Body.ToArray()),
        JsonOptions);
}
