using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Linn.Common.Messaging.RabbitMQ;

public class RabbitMessageRouter
{
    private readonly IChannel channel;
    private readonly Dictionary<string, IMessageHandler> handlers;

    public RabbitMessageRouter(
        IChannel channel,
        IEnumerable<IMessageHandler> handlers)
    {
        this.channel = channel;

        this.handlers = handlers
            .GroupBy(h => h.RoutingKey)
            .ToDictionary(
                g => g.Key,
                g => g.Single());
    }

    public AsyncEventingBasicConsumer CreateConsumer(
        CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(this.channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            if (!this.handlers.TryGetValue(ea.RoutingKey, out var handler))
            {
                Console.WriteLine(
                    $"No handler registered for routing key '{ea.RoutingKey}'");

                await this.channel.BasicRejectAsync(
                    ea.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);

                return;
            }

            var message = new Message
            {
                RoutingKey = ea.RoutingKey,
                Body = ea.Body,
                Headers = ea.BasicProperties?.Headers != null
                    ? new Dictionary<string, object>(ea.BasicProperties.Headers!)
                    : new Dictionary<string, object>()
            };

            try
            {
                await handler.HandleAsync(message, stoppingToken);

                await this.channel.BasicAckAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Handler for '{ea.RoutingKey}' failed: {ex}");

                await this.channel.BasicRejectAsync(
                    ea.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        return consumer;
    }
}
