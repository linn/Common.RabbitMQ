using Linn.Common.Configuration;
using RabbitMQ.Client;

namespace Linn.Common.Messaging.RabbitMQ;

public class RabbitChannelConfiguration(
    string queueName,
    string[] routingKeys,
    string exchangeName,
    bool durableExchange = true,
    string? dlqName = null,
    string? dlxName = null,
    bool createProducerChannel = true,
    bool createConsumerChannel = true)
    : IAsyncDisposable
{
    private readonly string dlqName = string.IsNullOrEmpty(dlqName) ? queueName : dlqName;

    private readonly string dlxName = string.IsNullOrEmpty(dlxName) ? exchangeName : dlxName;

    public string? Exchange { get; private set; }

    public string QueueName => $"{queueName}.q";

    public IConnection? Connection { get; private set; }

    public IChannel? ConsumerChannel { get; private set; }

    public IChannel? ProducerChannel { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = ConfigurationManager.Configuration["RABBIT_SERVER"], 
            UserName = ConfigurationManager.Configuration["RABBIT_USERNAME"],
            Password = ConfigurationManager.Configuration["RABBIT_PASSWORD"],
            Port = int.Parse(ConfigurationManager.Configuration["RABBIT_PORT"])
        };
                      
        this.Connection = await factory.CreateConnectionAsync(cancellationToken);
        this.Exchange = $"{exchangeName}.x";

        if (createConsumerChannel)
        {
            this.ConsumerChannel = await this.Connection.CreateChannelAsync(null, cancellationToken);
        }

        if (createProducerChannel)
        {
            this.ProducerChannel = await this.Connection.CreateChannelAsync(null, cancellationToken);
        }

        var setupChannel = this.ConsumerChannel ?? this.ProducerChannel;
            
        if (setupChannel == null)
        {
            throw new InvalidOperationException("At least one channel (consumer or producer) must be created");
        }

        await setupChannel.ExchangeDeclareAsync(
            this.Exchange,
            ExchangeType.Direct,
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

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", $"{this.dlxName}.dlx" },
                { "x-dead-letter-routing-key", string.Empty }
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
        }
    }

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
