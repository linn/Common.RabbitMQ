# Linn.Common.Messaging.RabbitMQ

[![NuGet](https://img.shields.io/nuget/v/Linn.Common.Messaging.RabbitMQ)](https://www.nuget.org/packages/Linn.Common.Messaging.RabbitMQ)

Opinionated RabbitMQ integration for .NET 10 services. Provides async channel setup, typed JSON publishing, routing-key-based message dispatch, and automatic dead-letter queue wiring — all built on top of `RabbitMQ.Client` v7.

---

## Installation

```
dotnet add package Linn.Common.Messaging.RabbitMQ
```

---

## Requirements

- .NET 10
- A RabbitMQ broker reachable via the following environment variables:

| Variable | Description |
|---|---|
| `RABBIT_SERVER` | Hostname of the RabbitMQ broker |
| `RABBIT_PORT` | Port (typically `5672`) |
| `RABBIT_USERNAME` | Broker username |
| `RABBIT_PASSWORD` | Broker password |

---

## Concepts

### Naming conventions

`RabbitChannelConfiguration` enforces a consistent naming scheme for every RabbitMQ object it declares:

| Object | Resolved name |
|---|---|
| Exchange | `{exchangeName}.x` |
| Queue | `{queueName}.q` |
| Dead-letter exchange | `{exchangeName}.dlx` |
| Dead-letter queue | `{queueName}.dlq` |

### Dead-letter topology

Every consumer queue is automatically wired to a dead-letter exchange and queue. Messages that are rejected (or that fail to be handled) are routed to the DLQ rather than lost.

```
Producer → {exchange}.x ──(routing key)──► {queue}.q
                                                │ (reject / unhandled)
                                                ▼
                                          {exchange}.dlx (fanout)
                                                │
                                                ▼
                                          {queue}.dlq
```

---

## Publishing messages

### 1. Configure the channel

```csharp
await using var config = new RabbitChannelConfiguration(
    queueName: "orders",
    routingKeys: ["order.created"],
    exchangeName: "orders",
    createConsumerChannel: false);   // producer-only

await config.InitializeAsync();
```

### 2. Publish a typed message

```csharp
var publisher = new JsonMessagePublisher<OrderDto>(
    rabbitPublisher: new RabbitPublisher(config.ProducerChannel!, config.Exchange!),
    routingKey: "order.created");

await publisher.PublishAsync(new OrderDto { Id = 42, Total = 99.99m });
```

`JsonMessagePublisher<T>` serialises `T` to UTF-8 JSON and sends it to the exchange with the configured routing key. Optional `headers` and `JsonSerializerOptions` can be supplied to the constructor.

---

## Consuming messages

### 1. Implement a handler

Extend `JsonMessageHandler<T>` for automatic JSON deserialisation:

```csharp
public class OrderCreatedHandler : JsonMessageHandler<OrderDto>
{
    public override string RoutingKey => "order.created";

    public override async Task HandleAsync(
        OrderDto order,
        IReadOnlyDictionary<string, object> headers,
        CancellationToken cancellationToken)
    {
        // process the order
    }
}
```

Or implement `IMessageHandler` directly for full control over the raw `Message`:

```csharp
public class RawHandler : IMessageHandler
{
    public string RoutingKey => "some.event";

    public Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        // message.Body is ReadOnlyMemory<byte>
        return Task.CompletedTask;
    }
}
```

### 2. Register services and wire up the router

```csharp
// In Program.cs / Startup
services.AddScoped<IMessageHandler, OrderCreatedHandler>();

// In your IHostedService or background worker
await using var config = new RabbitChannelConfiguration(
    queueName: "orders",
    routingKeys: ["order.created"],
    exchangeName: "orders",
    createProducerChannel: false);  // consumer-only

await config.InitializeAsync(stoppingToken);

var router = new RabbitMessageRouter(config.ConsumerChannel!, serviceProvider);
var consumer = router.CreateConsumer(stoppingToken);

await config.ConsumerChannel!.BasicConsumeAsync(
    queue: config.QueueName,
    autoAck: false,
    consumer: consumer,
    cancellationToken: stoppingToken);
```

> **Important (v6+):** `RabbitMessageRouter` requires an `IServiceProvider`. It creates a new DI scope for each received message, so handlers should be registered as **Scoped** services. This ensures scoped dependencies (e.g. `DbContext`) are fresh per message.

---

## Additional exchange bindings

A consumer queue can be bound to multiple external exchanges using `additionalBindings`:

```csharp
var config = new RabbitChannelConfiguration(
    queueName: "notifications",
    routingKeys: ["notification.email"],
    exchangeName: "notifications",
    additionalBindings:
    [
        new ExchangeBinding("payments", ["payment.completed"]),
        new ExchangeBinding("shipping", ["shipment.dispatched"])
    ]);
```

---

## API reference

### `RabbitChannelConfiguration`

| Parameter | Type | Default | Description |
|---|---|---|---|
| `queueName` | `string` | — | Base name for the queue (`.q` suffix added automatically) |
| `routingKeys` | `string[]` | — | Routing keys to bind the queue to |
| `exchangeName` | `string` | — | Base name for the exchange (`.x` suffix added automatically) |
| `durableExchange` | `bool` | `true` | Whether the exchange survives broker restarts |
| `dlqName` | `string?` | same as `queueName` | Override for the DLQ base name |
| `dlxName` | `string?` | same as `exchangeName` | Override for the DLX base name |
| `createProducerChannel` | `bool` | `true` | Create a channel for publishing |
| `createConsumerChannel` | `bool` | `true` | Create a channel for consuming and declare queue topology |
| `exchangeType` | `string` | `direct` | RabbitMQ exchange type (`direct`, `topic`, `fanout`, `headers`) |
| `additionalBindings` | `ExchangeBinding[]?` | `null` | Extra exchange→queue bindings |

### `JsonMessagePublisher<T>`

| Parameter | Type | Default | Description |
|---|---|---|---|
| `rabbitPublisher` | `RabbitPublisher` | — | Underlying publisher |
| `routingKey` | `string` | — | Routing key for published messages |
| `headers` | `IReadOnlyDictionary<string, object>?` | `null` | Optional message headers |
| `serializerOptions` | `JsonSerializerOptions?` | `null` | Custom JSON serialisation options |

### `ExchangeBinding`

```csharp
public record ExchangeBinding(string ExchangeName, string[] RoutingKeys);
```

Binds the consumer queue to `{ExchangeName}.x` for each routing key in `RoutingKeys`.

---

## Changelog

See [changelog.md](changelog.md).

---

## Publishing the package

The package is published to NuGet automatically by GitHub Actions on every push to `main`. Pull request builds run restore and build only — the pack and publish steps are skipped. See [.github/workflows/build-and-publish.yml](.github/workflows/build-and-publish.yml).