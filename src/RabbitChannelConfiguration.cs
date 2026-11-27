using Linn.Common.Configuration;
using RabbitMQ.Client;

namespace Linn.Common.Messaging.RabbitMQ;

// The constructor signature is unchanged since 5.0 (binary compatibility); newer options are init properties.
public class RabbitChannelConfiguration(
    string queueName,
    string[] routingKeys,
    string exchangeName,
    bool durableExchange = true,
    string? dlqName = null,
    string? dlxName = null,
    bool createProducerChannel = true,
    bool createConsumerChannel = true,
    string exchangeType = ExchangeType.Direct,
    ExchangeBinding[]? additionalBindings = null)
    : IAsyncDisposable
{
    private readonly string dlqName = string.IsNullOrEmpty(dlqName) ? queueName : dlqName;

    private readonly string dlxName = string.IsNullOrEmpty(dlxName) ? exchangeName : dlxName;

    private readonly TimeSpan? requestedHeartbeat;

    // Heartbeat to request from the broker; null keeps the client's 60s.
    public TimeSpan? RequestedHeartbeat
    {
        get => this.requestedHeartbeat;
        init
        {
            // The client negotiates whole seconds, so anything under a second would turn heartbeats off.
            if (value is { } v && (v < TimeSpan.FromSeconds(1) || v > TimeSpan.FromSeconds(ushort.MaxValue)))
            {
                throw new ArgumentOutOfRangeException(nameof(this.RequestedHeartbeat), v, "Must be between 1 and 65535 seconds.");
            }

            this.requestedHeartbeat = value;
        }
    }

    // Publishes on the producer channel wait for the broker's confirm and throw on a nack.
    public bool PublisherConfirms { get; init; }

    public string? Exchange { get; private set; }

    public string QueueName => $"{queueName}.q";

    public IConnection? Connection { get; private set; }

    public IChannel? ConsumerChannel { get; private set; }

    public IChannel? ProducerChannel { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var factory = this.CreateConnectionFactory();

        this.Connection = await factory.CreateConnectionAsync(cancellationToken);
        this.Exchange = $"{exchangeName}.x";

        if (createConsumerChannel)
        {
            this.ConsumerChannel = await this.Connection.CreateChannelAsync(null, cancellationToken);
        }

        if (createProducerChannel)
        {
            this.ProducerChannel = await this.Connection.CreateChannelAsync(
                this.ProducerChannelOptions(),
                cancellationToken);
        }

        var setupChannel = this.ConsumerChannel ?? this.ProducerChannel;
            
        if (setupChannel == null)
        {
            throw new InvalidOperationException("At least one channel (consumer or producer) must be created");
        }

        await setupChannel.ExchangeDeclareAsync(
            this.Exchange,
            exchangeType,
            durableExchange,
            cancellationToken: cancellationToken);

        if (createConsumerChannel)
        {
            await setupChannel.ExchangeDeclareAsync(
                $"{this.dlxName}.dlx",
                ExchangeType.Fanout,
                durableExchange,
                cancellationToken: cancellationToken);

            await setupChannel.QueueDeclareAsync(
                $"{this.dlqName}.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);
                
            await setupChannel.QueueBindAsync(
                queue: $"{this.dlqName}.dlq",
                exchange: $"{this.dlxName}.dlx",
                routingKey: string.Empty,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);

            var args = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", $"{this.dlxName}.dlx" }
            };

            await setupChannel.QueueDeclareAsync(
                $"{queueName}.q",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args,
                cancellationToken: cancellationToken);

            foreach (var routingKey in routingKeys)
            {
                await setupChannel.QueueBindAsync(
                    queue: $"{queueName}.q",
                    exchange: this.Exchange,
                    routingKey: routingKey,
                    arguments: null,
                    noWait: false,
                    cancellationToken: cancellationToken);
            }

            if (additionalBindings != null)
            {
                foreach (var binding in additionalBindings)
                {
                    foreach (var routingKey in binding.RoutingKeys)
                    {
                        await setupChannel.QueueBindAsync(
                            queue: $"{queueName}.q",
                            exchange: $"{binding.ExchangeName}.x",
                            routingKey: routingKey,
                            arguments: null,
                            noWait: false,
                            cancellationToken: cancellationToken);
                    }
                }
            }
        }
    }

    internal ConnectionFactory CreateConnectionFactory()
    {
        var factory = new ConnectionFactory
        {
            HostName = ConfigurationManager.Configuration["RABBIT_SERVER"]!,
            UserName = ConfigurationManager.Configuration["RABBIT_USERNAME"]!,
            Password = ConfigurationManager.Configuration["RABBIT_PASSWORD"]!,
            Port = int.Parse(ConfigurationManager.Configuration["RABBIT_PORT"]!)
        };

        if (this.RequestedHeartbeat is { } heartbeat)
        {
            factory.RequestedHeartbeat = heartbeat;
        }

        return factory;
    }

    // Tracking makes BasicPublishAsync wait for the broker's ack and throw on a nack.
    internal CreateChannelOptions? ProducerChannelOptions() =>
        this.PublisherConfirms
            ? new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true)
            : null;

    public async ValueTask DisposeAsync()
    {
        if (this.ConsumerChannel != null)
        {
            await this.ConsumerChannel.DisposeAsync();
        }

        if (this.ProducerChannel != null)
        {
            await this.ProducerChannel.DisposeAsync();
        }

        if (this.Connection != null)
        {
            await this.Connection.DisposeAsync();
        }
    }
}
